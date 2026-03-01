using NModbus;
using Serilog;
using SimpleMES.Models;
using System.Net.Sockets;

namespace SimpleMES.Core
{
    public class TcpDeviceClient : IDeviceClient
    {
        private DeviceModel _device;
        private ModbusFactory _factory;

        public TcpDeviceClient(DeviceModel device, ModbusFactory factory)
        {
            _device = device;
            _factory = factory;
        }

        public async ValueTask DisposeAsync() => await ValueTask.CompletedTask;

        public async Task<ushort[]> ReadHoldingRegistersAsync(ushort startAddress, ushort numberOfPoints, CancellationToken token = default)
        {
            using TcpClient client = new TcpClient();
            var connectTask = client.ConnectAsync(_device.IpAddress!, _device.Port ?? 502);
            if (await Task.WhenAny(connectTask, Task.Delay(2000, token)) != connectTask)
            {
                Log.Error("设备连接超时：DeviceId={DeviceId}，DeviceName={DeviceName}", _device.DeviceId, _device.DeviceName);
                throw new TimeoutException("连接超时");
            }
            Log.Information("设备连接成功：DeviceId={DeviceId}，DeviceName={DeviceName}", _device.DeviceId, _device.DeviceName);
            var master = _factory.CreateMaster(client);
            master.Transport.ReadTimeout = 2000;
            master.Transport.WriteTimeout = 2000;
            return await master.ReadHoldingRegistersAsync(_device.SlaveId, startAddress, numberOfPoints);
        }
    }
}
