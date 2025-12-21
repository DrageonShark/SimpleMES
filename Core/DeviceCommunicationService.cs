using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using NModbus;
using SimpleMES.Models;
using SimpleMES.Services.DAL;

namespace SimpleMES.Core
{
    public class DeviceCommunicationService
    {
        private readonly IDataRepository _repository;
        private bool _isRunning = false;
        private CancellationTokenSource _cts;

        // 模拟配置：假设我们有两个 Modbus TCP 设备
        private List<DeviceModel> _monitoredDevices;

        public DeviceCommunicationService(IDataRepository repository)
        {
            _repository = repository;
            _monitoredDevices = new List<DeviceModel>();
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
            while (!token.IsCancellationRequested)
            {
                foreach (var device in _monitoredDevices)
                {
                    try
                    {
                        // 创建 Modbus 工厂
                        var factory = new ModbusFactory();
                        IModbusMaster master;
                        //1.判断设备通讯类型
                        if (!string.IsNullOrWhiteSpace(device.IpAddress))
                        {
                            using TcpClient client = new TcpClient();
                            //2.连接设备 (设置超时 2秒)
                            var connectTask = client.ConnectAsync(device.IpAddress, device.Port ?? 502, token).AsTask();
                            if (await Task.WhenAny(connectTask, Task.Delay(2000, token)) != connectTask)
                            {
                                throw new TimeoutException("连接超时");
                            }
                            //3.创建 Master
                            master = factory.CreateMaster(client);
                            master.Transport.ReadTimeout = 1000;
                            //4.读取数据 (假设：寄存器0=温度*10，寄存器1=压力*10，寄存器2=转速)
                            //ReadHoldingRegisters(从站ID, 起始地址, 长度)
                            ushort[] data = await master.ReadHoldingRegistersAsync(1, 0, 3);
                            //5.解析数据
                            decimal temp = data[0] / 10.0m;
                            decimal press = data[1] / 10.0m;
                            int speed = data[2];
                            //6.更新内存状态(用于界面显示)
                            device.Status = "Running";
                            device.LastUpdateTime = DateTime.Now;
                            //7.存入数据库 (调用刚才写的 Repository）
                            var record = new ProductionRecordModel
                            {
                                DeviceId = device.DeviceId,
                                Temperature = temp,
                                Pressure = press,
                                Speed = speed,
                                RecordTime = DateTime.Now
                            };
                            // 异步写入，不等待结果，防止阻塞下一个设备的读取
                            _ = _repository.InsertProductionRecordAsync(record);
                            _ = _repository.UpdateDeviceStatusAsync(device.DeviceId, "Running", DateTime.Now);
                            Console.WriteLine($"[{device.DeviceName}] 温度:{temp} 压力:{press}");
                        }
                        else if(!string.IsNullOrWhiteSpace(device.SerialPort))
                        {
                            SerialPort serialPort = new SerialPort(device.SerialPort)
                            {
                                BaudRate = 9600,
                                DataBits = 8,
                                Parity = Parity.None,
                                StopBits = StopBits.One
                            };
                            serialPort.Open();
                            var adapter = new SerialPortAdapter(serialPort);
                            master = factory.CreateRtuMaster(adapter);
                            master.Transport.ReadTimeout = 2000;
                            master.Transport.WriteTimeout = 2000;
                        }
                        else
                        {
                            
                        }
                    }
                    catch (Exception ex)
                    {
                        // 记录异常，更新设备状态为 Fault
                        device.Status = "Fault";
                        await _repository.UpdateDeviceStatusAsync(device.DeviceId, "Fault", DateTime.Now);
                        Console.WriteLine($"[{device.DeviceName}] 通信失败: {ex.Message}");
                    }
                }
                // 休息 5 秒再进行下一轮轮询
                await Task.Delay(5000, token);
            }
        }

       
    }
}
