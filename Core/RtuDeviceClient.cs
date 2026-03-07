using NModbus;
using SimpleMES.Models;
using System.IO.Ports;

namespace SimpleMES.Core
{
    public class RtuDeviceClient : IDeviceClient
    {
        private DeviceModel _device;
        private ModbusFactory _factory;

        public RtuDeviceClient(DeviceModel device, ModbusFactory factory)
        {
            _device = device;
            _factory = factory;
        }

        public async ValueTask DisposeAsync() => await ValueTask.CompletedTask;

        public async Task<ushort[]> ReadHoldingRegistersAsync(ushort startAddress, ushort numberOfPoints, CancellationToken token = default)
        {
            using SerialPort serialPort = new SerialPort(_device.SerialPort!)
            {
                BaudRate = 9600,
                DataBits = 8,
                Parity = Parity.None,
                StopBits = StopBits.One
            };
            serialPort.Open();
            var adapter = new SerialPortAdapter(serialPort);
            using var master = _factory.CreateRtuMaster(adapter);
            master.Transport.ReadTimeout = 2000;
            master.Transport.WriteTimeout = 2000;
            return await master.ReadHoldingRegistersAsync(_device.SlaveId ?? 0, startAddress, numberOfPoints);
        }
    }
}
