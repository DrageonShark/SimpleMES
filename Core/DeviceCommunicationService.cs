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
    public class DeviceCommunicationService : Services.Observer.IDeviceStatusNotifier
    {
        private readonly IDataRepository _repository;
        private readonly IDeviceClientFactory _deviceClientFactory;
        private readonly IDevicePollingStrategyResolver _strategyResolver;
        //设备变更通知器
        private readonly IDeviceConfigNotifier _configNotifier;
        // 缓存长连接的 Client
        private readonly ConcurrentDictionary<int, IDeviceClient> _activeClients = new();
        private readonly ConcurrentDictionary<int, DeviceModel> _monitoredDevices = new();
        private readonly ConcurrentDictionary<int, IDeviceState> _deviceStates = new();
        // 每个设备独立的取消令牌，方便单独停止某个设备
        private readonly ConcurrentDictionary<int, CancellationTokenSource> _deviceTasksCts = new();
        private readonly ConcurrentDictionary<int, DeviceDto> _latestDeviceSnapshots = new();
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
        public async Task StartAsync()
        {
            Log.Information("初始化并启动设备通信服务...");
            var devices = await _repository.GetAllDevicesAsync();
            foreach (var d in devices)
            {
                StartDeviceTask(d);// 为每个设备启动独立的采集线程
            }
        }
        ///处理热更新
        private async void OnDeviceConfigChanged(object? sender, DeviceConfigChangeEventArgs e)
        {
            Log.Information("收到设备配置变更通知，设备Id：{DeviceId}，设备名： {DeviceName}, 操作: {ChangeType}", e.Device.DeviceId, e.Device.DeviceName, e.ChangeType);
            if (e.ChangeType == ConfigChangeType.Updated || e.ChangeType == ConfigChangeType.Deleted)
            {
                // 先停止旧的采集任务和销毁旧的 Client
                StopDeviceTask(e.Device.DeviceId);
            }
            if (e.ChangeType == ConfigChangeType.Added || e.ChangeType == ConfigChangeType.Updated)
            {
                StartDeviceTask(e.Device);
            }
        }

        // 启动单个设备的采集循环
        private void StartDeviceTask(DeviceModel device)
        {
            _monitoredDevices[device.DeviceId] = device;
            _deviceStates[device.DeviceId] = new DisconnectedState();
            var cts = new CancellationTokenSource();
            _deviceTasksCts[device.DeviceId] = cts;
            Task.Run(() =>
                _ = SingleDevicePollingLoop(device, cts.Token), cts.Token
            );
        }
        // 停止单个设备的采集循环
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

            _monitoredDevices.TryRemove(deviceId, out _);
            _deviceStates.TryRemove(deviceId, out _);
        }

        // 针对单个设备的长循环
        private async Task SingleDevicePollingLoop(DeviceModel device, CancellationToken token)
        {
            Log.Information("启动设备采集线程，设备Id：{DeviceId}，设备名：{DeviceName}", device.DeviceId, device.DeviceName);
            while (!token.IsCancellationRequested)
            {
                var state = _deviceStates[device.DeviceId];
                try
                {
                    // 1. 获取或创建长连接 Client
                    if (!_activeClients.TryGetValue(device.DeviceId, out var client))
                    {
                        Log.Debug("与设备建立新连接：{DeviceId}，{DeviceName}", device.DeviceId, device.DeviceName);
                        client = _deviceClientFactory.Create(device);
                        _activeClients[device.DeviceId] = client;
                    }
                    // 2. 采集数据
                    var strategy = _strategyResolver.Resolve(device);
                    var outcome = await strategy.PollAsync(client, device, token);//复用长连接的client
                    // 3. 处理持久化和状态机
                    if (outcome.PersistAsync != null)
                        await outcome.PersistAsync(_repository, token);
                    state = await state.HandleAsync(device, outcome.PollResult, _repository, token);
                    _deviceStates[device.DeviceId] = state;
                    NotifySingleDeviceUpdate(device, outcome, state);
                }
                catch (Exception ex)
                {
                    Log.Error("设备异常，设备Id：{DeviceId}，设备名：{DeviceName}，错误信息：{Message}", device.DeviceId, device.DeviceName, ex.Message);
                    // 如果发生异常（如断线），销毁当前的 Client，强制下一次循环重连
                    if (_activeClients.TryGetValue(device.DeviceId, out var badClient))
                        await badClient.DisposeAsync();
                    state = await state.HandleAsync(device, new DevicePollResult(false, null, ex, DateTime.Now), _repository, token);
                    _deviceStates[device.DeviceId] = state;
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
        private void NotifySingleDeviceUpdate(DeviceModel device, PollingResult? outcome, IDeviceState state, Exception err = null)
        {
            var newSnapshot = outcome?.Snapshot ?? new DeviceDto
            {
                DeviceId = device.DeviceId,
                DeviceName = device.DeviceName,
                IpAddress = device.IpAddress,
                SerialPort = device.SerialPort,
                SlaveId = device.SlaveId,
                Temperature = 0,
                Pressure = 0,
                Speed = 0,
                DeviceState = Enum.TryParse<DeviceState>(device.DeviceState, true, out var ds) ? ds : DeviceState.Disconnected,
                LastUpdateTime = device.LastUpdateTime
            };
            _latestDeviceSnapshots[device.DeviceId] = newSnapshot;
            var latestList = _latestDeviceSnapshots.Values.ToList().AsReadOnly();
            DeviceStatusChanged?.Invoke(this, new DeviceStatusChangedEventArgs(latestList));
        }

        public void Stop()
        {
            foreach (var kvp in _deviceTasksCts)
            {
                StopDeviceTask(kvp.Key);
            }
        }
    }
}
