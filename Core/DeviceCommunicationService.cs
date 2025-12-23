using NModbus;
using SimpleMES.Models;
using SimpleMES.Services.DAL;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Ports;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using SimpleMES.Models.Dto;

namespace SimpleMES.Core
{
    public class DeviceCommunicationService
    {
        private readonly IDataRepository _repository;
        private bool _isRunning = false;
        private CancellationTokenSource _cts;
        public event Action<List<DeviceDto>> OnDeviceStatusChanged;

        // 模拟配置：假设我们有两个 Modbus TCP 设备
        private List<DeviceModel> _monitoredDevices;

        public DeviceCommunicationService(IDataRepository repository)
        {
            _repository = repository;
            _monitoredDevices = new List<DeviceModel>();
            _ = LoadDevicesAsync();
        }
        // 初始化加载设备列表
        public async Task LoadDevicesAsync()
        {
            _monitoredDevices = (await _repository.GetAllDevicesAsync()).ToList();
        }

        public void Start()
        {
            if(_isRunning) return;
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
                    try
                    {
                        // 模拟数据容器
                        ushort[] data = null;

                        // === TCP 设备处理 ===
                        if (!string.IsNullOrWhiteSpace(device.IpAddress))
                        {
                            using (TcpClient client = new TcpClient())
                            {
                                // 1. 连接
                                var connectTask = client.ConnectAsync(device.IpAddress, device.Port ?? 502);
                                // 等待连接，超时时间 2秒
                                if (await Task.WhenAny(connectTask, Task.Delay(2000, token)) != connectTask)
                                {
                                    throw new TimeoutException("连接超时");
                                }

                                // 2. 读取
                                var master = factory.CreateMaster(client);
                                master.Transport.ReadTimeout = 2000;
                                master.Transport.WriteTimeout = 2000;

                                // 注意：这里要把硬编码的 '1' 改成 device.SlaveId
                                data = await master.ReadHoldingRegistersAsync(device.SlaveId, 0, 3);
                            }
                        }
                        // === 串口 (RTU) 设备处理 ===
                        else if (!string.IsNullOrWhiteSpace(device.SerialPort))
                        {
                            // 使用 using 确保每次读取完都关闭串口，释放 COM 口资源
                            // 虽然效率不如长连接，但最稳定，不会报 "Port Already Open"
                            using (SerialPort serialPort = new SerialPort(device.SerialPort))
                            {
                                serialPort.BaudRate = 9600;
                                serialPort.DataBits = 8;
                                serialPort.Parity = Parity.None;
                                serialPort.StopBits = StopBits.One;

                                serialPort.Open(); // 打开串口

                                var adapter = new SerialPortAdapter(serialPort);
                                using (var master = factory.CreateRtuMaster(adapter))
                                {
                                    master.Transport.ReadTimeout = 2000;
                                    master.Transport.WriteTimeout = 2000;

                                    // 注意：改为 device.SlaveId
                                    data = await master.ReadHoldingRegistersAsync(device.SlaveId, 0, 3);
                                }
                            }
                            // 离开 using 块，serialPort 自动 Close()
                        }

                        // === 数据处理与入库 (通用逻辑) ===
                        if (data != null)
                        {
                            decimal temp = data[0] / 10.0m;
                            decimal press = data[1] / 12.0m;
                            int speed = data[2] / 15;

                            // 内存更新
                            device.Status = "Running";
                            device.LastUpdateTime = DateTime.Now;
                            devices.Add(new DeviceDto
                            {
                                DeviceId = device.DeviceId,
                                DeviceName = device.DeviceName,
                                IpAddress = device.IpAddress,
                                LastUpdateTime = device.LastUpdateTime,
                                Pressure = press,
                                SerialPort = device.SerialPort,
                                Status = device.Status,
                                Temperature = temp,
                                Speed = speed
                            });

                            // 数据库写入
                            var record = new ProductionRecordModel
                            {
                                DeviceId = device.DeviceId,
                                Temperature = temp,
                                Pressure = press,
                                Speed = speed,
                                RecordTime = DateTime.Now
                            };

                            _ = _repository.InsertProductionRecordAsync(record);
                            _ = _repository.UpdateDeviceStatusAsync(device.DeviceId, "Running", DateTime.Now);

                            Debug.WriteLine($"SUCCESS >>> [{device.DeviceName}] 温度:{temp} 压力:{press}");
                        }
                    }
                    catch (Exception ex)
                    {
                        device.Status = "Fault";
                        await _repository.UpdateDeviceStatusAsync(device.DeviceId, "Fault", DateTime.Now);
                        // 打印详细错误方便调试
                        Console.WriteLine($"[{device.DeviceName}] 错误: {ex.Message}");
                    }
                }
                OnDeviceStatusChanged?.Invoke(devices);
                // 暂停 5 秒
                try { await Task.Delay(5000, token); } catch { break; }
            }
        }


    }
}
