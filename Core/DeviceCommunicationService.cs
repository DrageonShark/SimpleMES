using NModbus;
using SimpleMES.Models;
using SimpleMES.Models.Dto;
using SimpleMES.Services.DAL;
using SimpleMES.Services.Observer;
using SimpleMES.Services.State;
using System.Diagnostics;

namespace SimpleMES.Core
{
    public class DeviceCommunicationService : Services.Observer.IDeviceStatusNotifier
    {
        private readonly IDataRepository _repository;
        private readonly IDeviceClientFactory _deviceClientFactory;
        private bool _isRunning = false;
        private CancellationTokenSource _cts;
        private List<DeviceModel> _monitoredDevices;
        //使用字典更快，避免重复赋值影响性能
        private readonly Dictionary<int, IDeviceState> _deviceStates = new();
        public event EventHandler<DeviceStatusChangedEventArgs>? DeviceStatusChanged;
        public DeviceCommunicationService(IDataRepository repository, IDeviceClientFactory deviceClientFactory)
        {
            _repository = repository;
            _deviceClientFactory = deviceClientFactory;
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
                        // 模拟数据容器
                        ushort[] data = null;
                        await using var client = _deviceClientFactory.Create(device);
                        data = await client.ReadHoldingRegistersAsync(0, 3, token);
                        var pollResult = new DevicePollResult(
                            IsSuccess: data != null,
                            RawData: data,
                            Exception: null,
                            Timestamp: DateTime.Now);

                        state = await state.HandleAsync(device, pollResult, _repository, token);
                        _deviceStates[device.DeviceId] = state;

                        // === 数据处理与入库 (通用逻辑) ===
                        if (data != null)
                        {
                            decimal temp = Math.Round(data[0] / 10.0m, 3);
                            decimal press = Math.Round(data[1] / 12.0m, 3);
                            int speed = data[2] / 15;

                            // 内存更新
                            device.LastUpdateTime = DateTime.Now;
                            devices.Add(new DeviceDto
                            {
                                DeviceId = device.DeviceId,
                                DeviceName = device.DeviceName,
                                IpAddress = device.IpAddress,
                                LastUpdateTime = device.LastUpdateTime,
                                Pressure = press,
                                SerialPort = device.SerialPort,
                                DeviceState = Enum.Parse<DeviceState>(device.DeviceState, true),
                                Temperature = temp,
                                Speed = speed,
                            });
                            Debug.WriteLine($"SUCCESS >>> [{device.DeviceName}] 温度:{temp} 压力:{press}");
                        }
                    }
                    catch (Exception ex)
                    {
                        var pollResult = new DevicePollResult(false, null, ex, DateTime.Now);
                        state = await state.HandleAsync(device, pollResult, _repository, token);
                        _deviceStates[device.DeviceId] = state;
                        // 打印详细错误方便调试
                        Debug.WriteLine($"[{device.DeviceName}] 错误: {ex.Message}");
                    }
                }
                DeviceStatusChanged?.Invoke(this, new DeviceStatusChangedEventArgs(devices.AsReadOnly()));
                // 暂停 5 秒
                try { await Task.Delay(5000, token); } catch { break; }
            }
        }


    }
}
