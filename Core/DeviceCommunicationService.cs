using Serilog;
using SimpleMES.Models;
using SimpleMES.Models.Dto;
using SimpleMES.Services.DAL;
using SimpleMES.Services.Observer;
using SimpleMES.Services.State;
using SimpleMES.Services.Strategy;
using System.Collections.Concurrent;

namespace SimpleMES.Core
{
    /// <summary>
    /// 设备通信服务。
    /// 负责设备轮询任务的生命周期管理、设备状态跟踪、快照发布以及配置热更新处理。
    /// </summary>
    public class DeviceCommunicationService : Services.Observer.IDeviceStatusNotifier
    {
        /// <summary>
        /// 数据仓储服务，用于读取设备配置、写入轮询结果、读取最近事件。
        /// </summary>
        private readonly IDataRepository _repository;

        /// <summary>
        /// 设备客户端工厂，用于按设备配置创建通信客户端。
        /// </summary>
        private readonly IDeviceClientFactory _deviceClientFactory;

        /// <summary>
        /// 轮询策略解析器，根据设备类型解析具体采集策略。
        /// </summary>
        private readonly IDevicePollingStrategyResolver _strategyResolver;

        //设备变更通知器
        /// <summary>
        /// 设备配置变更通知器，用于处理新增/修改/启用/停用/删除等热更新事件。
        /// </summary>
        private readonly IDeviceConfigNotifier _configNotifier;

        // 缓存长连接的 Client
        /// <summary>
        /// 活跃设备客户端缓存（按 DeviceId），用于复用长连接。
        /// </summary>
        private readonly ConcurrentDictionary<int, IDeviceClient> _activeClients = new();

        /// <summary>
        /// 当前受监控设备集合（按 DeviceId）。
        /// </summary>
        private readonly ConcurrentDictionary<int, MonitoredDeviceModel> _monitoredDevices = new();
        private readonly ConcurrentDictionary<int, IDeviceState> _deviceStates = new();
        // 每个设备独立的取消令牌，方便单独停止某个设备
        private readonly ConcurrentDictionary<int, CancellationTokenSource> _deviceTasksCts = new();
        private readonly ConcurrentDictionary<int, Task> _devicePollingTasks = new();
        private readonly ConcurrentDictionary<int, DeviceDto> _latestDeviceSnapshots = new();
        private IReadOnlyList<DeviceEventDto> _latestDeviceEvents = Array.Empty<DeviceEventDto>();
        public event EventHandler<DeviceStatusChangedEventArgs>? DeviceStatusChanged;
        public DeviceCommunicationService(IDataRepository repository, IDeviceClientFactory deviceClientFactory, IDevicePollingStrategyResolver strategyResolver, IDeviceConfigNotifier configNotifier)
        {
            _repository = repository;
            _deviceClientFactory = deviceClientFactory;
            _strategyResolver = strategyResolver;
            _configNotifier = configNotifier;
            // 订阅配置变更事件
            _configNotifier.ConfigChanged += OnDeviceConfigChanged;
        }

        // 初始化加载设备列表
        /// <summary>
        /// 启动服务并初始化设备轮询。
        /// 读取全部设备，构建初始快照，对已启用设备启动独立采集线程。
        /// </summary>
        public async Task StartAsync()
        {
            Log.Information("初始化并启动设备通信服务...");
            var devices = await _repository.GetAllDevicesAsync();
            await RefreshRecentEventsAsync();
            foreach (var d in devices)
            {
                _monitoredDevices[d.DeviceId] = d;
                _latestDeviceSnapshots[d.DeviceId] = BuildSnapshot(d);
                if (d.IsEnabled)
                {
                    StartDeviceTask(d);// 为每个设备启动独立的采集线程
                }
            }
            PublishLatestSnapshots();
        }

        ///处理热更新
        /// <summary>
        /// 处理设备配置热更新事件。
        /// 根据变更类型停止旧任务、更新监控缓存、刷新快照和事件，并按需重启轮询任务。
        /// </summary>
        /// <param name="sender">事件发送方。</param>
        /// <param name="e">配置变更参数。</param>
        private async void OnDeviceConfigChanged(object? sender, DeviceConfigChangeEventArgs e)
        {
            Log.Information("收到设备配置变更通知，设备Id：{DeviceId}，设备名： {DeviceName}, 操作: {ChangeType}", e.Device.DeviceId, e.Device.DeviceName, e.ChangeType);
            if (e.ChangeType == ConfigChangeType.Updated
                || e.ChangeType == ConfigChangeType.Deleted
                || e.ChangeType == ConfigChangeType.Disabled
                || e.ChangeType == ConfigChangeType.Enabled)
            {
                // 先停止旧的采集任务和销毁旧的 Client
                StopDeviceTask(e.Device.DeviceId);
            }

            if (e.ChangeType == ConfigChangeType.Deleted)
            {
                _monitoredDevices.TryRemove(e.Device.DeviceId, out _);
                _latestDeviceSnapshots.TryRemove(e.Device.DeviceId, out _);
                PublishLatestSnapshots();
                return;
            }

            var runtime = ResolveRuntimeSnapshot(e.Device.DeviceId, e.ChangeType);
            var monitored = new MonitoredDeviceModel
            {
                Device = e.Device,
                Runtime = runtime
            };

            _monitoredDevices[e.Device.DeviceId] = monitored;
            _latestDeviceSnapshots[e.Device.DeviceId] = BuildSnapshot(monitored);
            await RefreshRecentEventsAsync();
            PublishLatestSnapshots();

            if ((e.ChangeType == ConfigChangeType.Added || e.ChangeType == ConfigChangeType.Updated || e.ChangeType == ConfigChangeType.Enabled)
                && monitored.Device.IsEnabled)
            {
                StartDeviceTask(monitored);
            }
        }

        // 启动单个设备的采集循环
        /// <summary>
        /// 启动单个设备采集任务，并初始化设备状态与取消令牌。
        /// </summary>
        /// <param name="device">目标设备。</param>
        private void StartDeviceTask(MonitoredDeviceModel device)
        {
            Log.Information("准备启动设备采集线程，设备Id：{DeviceId}，设备名：{DeviceName}", device.DeviceId, device.DeviceName);
            _monitoredDevices[device.DeviceId] = device;
            _deviceStates[device.DeviceId] = new DisconnectedState();
            var cts = new CancellationTokenSource();
            _deviceTasksCts[device.DeviceId] = cts;

            var pollingTask = Task.Factory.StartNew(
                async () => await SingleDevicePollingLoop(device, cts.Token),
                cts.Token,
                TaskCreationOptions.LongRunning,
                TaskScheduler.Default).Unwrap();

            _devicePollingTasks[device.DeviceId] = pollingTask;
            _ = pollingTask.ContinueWith(task =>
            {
                _devicePollingTasks.TryRemove(device.DeviceId, out _);
                if (task.IsFaulted)
                {
                    Log.Error(task.Exception, "设备采集线程异常退出，设备Id：{DeviceId}，设备名：{DeviceName}", device.DeviceId, device.DeviceName);
                }
                else
                {
                    Log.Information("设备采集线程已结束，设备Id：{DeviceId}，设备名：{DeviceName}", device.DeviceId, device.DeviceName);
                }
            }, TaskScheduler.Default);
        }

        // 停止单个设备的采集循环
        /// <summary>
        /// 停止单个设备的采集任务并释放关联资源（取消令牌、客户端连接、状态缓存）。
        /// </summary>
        /// <param name="deviceId">设备ID。</param>
        private void StopDeviceTask(int deviceId)
        {
            if (_deviceTasksCts.TryRemove(deviceId, out var cts))
            {
                cts.Cancel();
                cts.Dispose();
            }

            if (_activeClients.TryRemove(deviceId, out var client))
            {
                // 释放旧连接
                client.DisposeAsync().AsTask().Wait();
            }

            _deviceStates.TryRemove(deviceId, out _);
            _devicePollingTasks.TryRemove(deviceId, out _);
        }

        // 针对单个设备的长循环
        /// <summary>
        /// 单设备轮询主循环。
        /// 包括客户端复用/重连、策略采集、持久化、状态机迁移、快照发布与异常处理。
        /// </summary>
        /// <param name="device">目标设备。</param>
        /// <param name="token">取消令牌。</param>
        private async Task SingleDevicePollingLoop(MonitoredDeviceModel device, CancellationToken token)
        {
            Log.Information("启动设备采集线程，设备Id：{DeviceId}，设备名：{DeviceName}", device.DeviceId, device.DeviceName);
            while (!token.IsCancellationRequested)
            {
                var state = _deviceStates[device.DeviceId];
                var previousStateName = state.Name;
                try
                {
                    // 1. 获取或创建长连接 Client
                    if (!_activeClients.TryGetValue(device.DeviceId, out var client))
                    {
                        Log.Debug("与设备建立新连接：{DeviceId}，{DeviceName}", device.DeviceId, device.DeviceName);
                        client = _deviceClientFactory.Create(device.Device);
                        _activeClients[device.DeviceId] = client;
                    }
                    // 2. 采集数据
                    var strategy = _strategyResolver.Resolve(device.Device);
                    var outcome = await strategy.PollAsync(client, device, token);//复用长连接的client
                    // 3. 处理持久化和状态机
                    if (outcome.PersistAsync != null)
                        await outcome.PersistAsync(_repository, token);
                    state = await state.HandleAsync(device, outcome.PollResult, _repository, token);
                    _deviceStates[device.DeviceId] = state;
                    if (!string.Equals(previousStateName, state.Name, StringComparison.OrdinalIgnoreCase))
                    {
                        await RefreshRecentEventsAsync();
                    }
                    NotifySingleDeviceUpdate(device, outcome, state);
                }
                catch (Exception ex)
                {
                    Log.Error("设备异常，设备Id：{DeviceId}，设备名：{DeviceName}，错误信息：{Message}", device.DeviceId, device.DeviceName, ex.Message);
                    // 如果发生异常（如断线），销毁当前的 Client，强制下一次循环重连
                    if (_activeClients.TryRemove(device.DeviceId, out var badClient))
                    {
                        await badClient.DisposeAsync();
                    }
                    state = await state.HandleAsync(device, new DevicePollResult(false, null, ex, DateTime.Now), _repository, token);
                    _deviceStates[device.DeviceId] = state;
                    if (!string.Equals(previousStateName, state.Name, StringComparison.OrdinalIgnoreCase))
                    {
                        await RefreshRecentEventsAsync();
                    }
                    NotifySingleDeviceUpdate(device, null, state, ex);
                }
                try
                {
                    await Task.Delay(2000, token);
                }
                catch
                {
                    break;
                }
            }
            Log.Information("设备采集线程已停止: 设备Id：{DeviceId}，设备名：{DeviceName}", device.DeviceName, device.DeviceId);
        }

        /// <summary>
        /// 更新单设备快照并发布最新快照列表。
        /// </summary>
        /// <param name="device">设备信息。</param>
        /// <param name="outcome">轮询结果（可为 null）。</param>
        /// <param name="state">当前状态机状态。</param>
        /// <param name="err">异常对象（可为 null，仅用于调用链语义）。</param>
        private void NotifySingleDeviceUpdate(MonitoredDeviceModel device, PollingResult? outcome, IDeviceState state, Exception err = null)
        {
            var newSnapshot = BuildSnapshot(device, outcome?.Snapshot);
            _latestDeviceSnapshots[device.DeviceId] = newSnapshot;
            PublishLatestSnapshots();
        }

        /// <summary>
        /// 停止所有设备采集任务。
        /// </summary>
        public void Stop()
        {
            foreach (var kvp in _deviceTasksCts)
            {
                StopDeviceTask(kvp.Key);
            }
        }

        /// <summary>
        /// 解析并生成运行时快照。
        /// 如果设备已存在，尽量复用现有运行时信息；否则创建默认运行时对象。
        /// </summary>
        /// <param name="deviceId">设备ID。</param>
        /// <param name="changeType">配置变更类型。</param>
        /// <returns>可用于监控模型的运行时对象。</returns>
        private DeviceRuntimeModel ResolveRuntimeSnapshot(int deviceId, ConfigChangeType changeType)
        {
            if (_monitoredDevices.TryGetValue(deviceId, out var existing))
            {
                var runtime = existing.Runtime;
                if (changeType == ConfigChangeType.Enabled)
                {
                    runtime.DeviceState = nameof(DeviceState.Disconnected);
                    runtime.LastUpdateTime = DateTime.Now;
                }
                else if (changeType == ConfigChangeType.Disabled)
                {
                    runtime.DeviceState = nameof(DeviceState.Disabled);
                    runtime.LastUpdateTime = DateTime.Now;
                    runtime.LastStateChangeTime = DateTime.Now;
                }

                return runtime;
            }

            var now = DateTime.Now;
            return new DeviceRuntimeModel
            {
                DeviceId = deviceId,
                DeviceState = changeType == ConfigChangeType.Disabled ? nameof(DeviceState.Disabled) : nameof(DeviceState.Disconnected),
                LastUpdateTime = now,
                LastStateChangeTime = now,
                UpdatedAt = now
            };
        }

        /// <summary>
        /// 构建设备对外快照。
        /// 若传入实时快照则在其基础上覆盖静态/运行时字段；否则根据监控模型创建新快照。
        /// </summary>
        /// <param name="device">监控设备模型。</param>
        /// <param name="liveSnapshot">实时快照（可选）。</param>
        /// <returns>用于发布的设备快照。</returns>
        private DeviceDto BuildSnapshot(MonitoredDeviceModel device, DeviceDto? liveSnapshot = null)
        {
            var snapshot = liveSnapshot ?? new DeviceDto
            {
                DeviceId = device.DeviceId,
                DeviceName = device.DeviceName,
                DeviceCode = device.Device.DeviceCode,
                DeviceType = device.Device.DeviceType,
                IpAddress = device.IpAddress,
                Port = device.Port,
                SerialPort = device.SerialPort,
                SlaveId = device.SlaveId,
                WorkshopName = device.Device.WorkshopName,
                LineName = device.Device.LineName,
                StationName = device.Device.StationName,
                IsEnabled = device.Device.IsEnabled,
                Criticality = device.Device.Criticality,
                Temperature = 0,
                Pressure = 0,
                Speed = 0
            };

            snapshot.DeviceName = device.DeviceName;
            snapshot.DeviceCode = device.Device.DeviceCode;
            snapshot.DeviceType = device.Device.DeviceType;
            snapshot.IpAddress = device.IpAddress;
            snapshot.Port = device.Port;
            snapshot.SerialPort = device.SerialPort;
            snapshot.SlaveId = device.SlaveId;
            snapshot.WorkshopName = device.Device.WorkshopName;
            snapshot.LineName = device.Device.LineName;
            snapshot.StationName = device.Device.StationName;
            snapshot.IsEnabled = device.Device.IsEnabled;
            snapshot.Criticality = device.Device.Criticality;
            snapshot.SortOrder = device.Device.SortOrder;
            snapshot.Remark = device.Device.Remark;
            snapshot.DeviceState = Enum.TryParse<DeviceState>(device.Runtime.DeviceState, true, out var ds)
                ? ds
                : DeviceState.Disconnected;
            snapshot.LastUpdateTime = device.Runtime.LastUpdateTime;
            snapshot.LastHeartbeatTime = device.Runtime.LastHeartbeatTime;
            snapshot.LastStateChangeTime = device.Runtime.LastStateChangeTime;
            snapshot.CurrentOrderNo = device.Runtime.CurrentOrderNo;
            return snapshot;
        }

        /// <summary>
        /// 发布当前全部设备快照及最近设备事件给订阅方。
        /// </summary>
        private void PublishLatestSnapshots()
        {
            var latestList = _latestDeviceSnapshots.Values.ToList().AsReadOnly();
            DeviceStatusChanged?.Invoke(this, new DeviceStatusChangedEventArgs(latestList, _latestDeviceEvents));
        }

        /// <summary>
        /// 从仓储刷新最近设备事件缓存。
        /// </summary>
        private async Task RefreshRecentEventsAsync()
        {
            _latestDeviceEvents = (await _repository.GetRecentDeviceEventsAsync()).ToList().AsReadOnly();
        }
    }
}
