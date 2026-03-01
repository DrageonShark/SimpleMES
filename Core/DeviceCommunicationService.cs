using NModbus;
using Serilog;
using SimpleMES.Models;
using SimpleMES.Models.Dto;
using SimpleMES.Services.DAL;
using SimpleMES.Services.Observer;
using SimpleMES.Services.State;

namespace SimpleMES.Core
{
    public class DeviceCommunicationService : Services.Observer.IDeviceStatusNotifier
    {
        private readonly IDataRepository _repository;
        private readonly IDeviceClientFactory _deviceClientFactory;
        private readonly IDevicePollingStrategyResolver _strategyResolver;
        private bool _isRunning = false;
        private CancellationTokenSource _cts;
        private List<DeviceModel> _monitoredDevices;
        //使用字典更快，避免重复赋值影响性能
        private readonly Dictionary<int, IDeviceState> _deviceStates = new();
        public event EventHandler<DeviceStatusChangedEventArgs>? DeviceStatusChanged;
        public DeviceCommunicationService(IDataRepository repository, IDeviceClientFactory deviceClientFactory, IDevicePollingStrategyResolver strategyResolver)
        {
            _repository = repository;
            _deviceClientFactory = deviceClientFactory;
            _strategyResolver = strategyResolver;
            _monitoredDevices = new List<DeviceModel>();
            _ = LoadDevicesAsync();
        }
        // 初始化加载设备列表
        public async Task LoadDevicesAsync()
        {
            _monitoredDevices = (await _repository.GetAllDevicesAsync()).ToList();
            foreach (var d in _monitoredDevices)
                _deviceStates.TryAdd(d.DeviceId, new DisconnectedState());
        }

        public void Start()
        {
            if (_isRunning) return;
            _isRunning = true;
            _cts = new CancellationTokenSource();
            // 开启一个后台长任务
            Task.Run(() => PollingLoop(_cts.Token));
        }

        public void Stop()
        {
            _isRunning = false;
            _cts?.Cancel();
        }

        private async Task PollingLoop(CancellationToken token)
        {
            Log.Information("连接设备");
            var factory = new ModbusFactory();

            while (!token.IsCancellationRequested)
            {
                List<DeviceDto> devices = new List<DeviceDto>();
                foreach (var device in _monitoredDevices)
                {
                    if (!_deviceStates.TryGetValue(device.DeviceId, out var state))
                        state = _deviceStates[device.DeviceId] = new DisconnectedState();

                    try
                    {
                        Log.Debug("尝试连接设备：DeviceId={DeviceId}，DeviceName={DeviceName}", device.DeviceId, device.DeviceName);
                        await using var client = _deviceClientFactory.Create(device);
                        var strategy = _strategyResolver.Resolve(device);
                        var outcome = await strategy.PollAsync(client, device, token);
                        if (outcome.PersistAsync != null)
                            await outcome.PersistAsync(_repository, token);
                        state = await state.HandleAsync(device, outcome.PollResult, _repository, token);
                        _deviceStates[device.DeviceId] = state;

                        var snapshot = outcome.Snapshot ?? new DeviceDto
                        {
                            DeviceId = device.DeviceId,
                            DeviceName = device.DeviceName,
                            IpAddress = device.IpAddress,
                            SerialPort = device.SerialPort,
                            Temperature = 0,
                            Pressure = 0,
                            Speed = 0,
                            DeviceState = Enum.TryParse<DeviceState>(device.DeviceState, true, out var ds) ? ds : DeviceState.Disconnected,
                            LastUpdateTime = device.LastUpdateTime
                        };

                        snapshot.DeviceState = Enum.TryParse<DeviceState>(device.DeviceState, true, out var parsed) ? parsed : DeviceState.Disconnected;
                        snapshot.LastUpdateTime = device.LastUpdateTime;
                        devices.Add(snapshot);
                        Log.Debug("设备连接成功：DeviceId={DeviceId}，DeviceName={DeviceName}", device.DeviceId, device.DeviceName);
                    }
                    catch (Exception ex)
                    {
                        var pollResult = new DevicePollResult(false, null, ex, DateTime.Now);
                        state = await state.HandleAsync(device, pollResult, _repository, token);
                        _deviceStates[device.DeviceId] = state;
                        devices.Add(new DeviceDto
                        {
                            DeviceId = device.DeviceId,
                            DeviceName = device.DeviceName,
                            IpAddress = device.IpAddress,
                            SerialPort = device.SerialPort,
                            Temperature = 0,
                            Pressure = 0,
                            Speed = 0,
                            DeviceState = DeviceState.Disconnected,
                            LastUpdateTime = device.LastUpdateTime
                        });
                        // 打印详细错误方便调试
                        Log.Error("[{DeviceName}] 错误: {ex}", device.DeviceName, ex.Message);
                    }
                }
                DeviceStatusChanged?.Invoke(this, new DeviceStatusChangedEventArgs(devices.AsReadOnly()));
                // 暂停 5 秒
                try { await Task.Delay(5000, token); } catch { break; }
            }
        }


    }
}
