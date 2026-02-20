using NModbus;
using SimpleMES.Models;

namespace SimpleMES.Core
{
    public class DeviceClientFactory : IDeviceClientFactory
    {
        private readonly ModbusFactory _modbusFactory;

        public DeviceClientFactory() : this(new ModbusFactory()) { }

        public DeviceClientFactory(ModbusFactory modbusFactory)
        {
            _modbusFactory = modbusFactory;
        }
        public IDeviceClient Create(DeviceModel device)
        {
            if (!string.IsNullOrWhiteSpace(device.IpAddress))
                return new TcpDeviceClient(device, _modbusFactory);
            if (!string.IsNullOrWhiteSpace(device.SerialPort))
                return new RtuDeviceClient(device, _modbusFactory);
            return new MockDeviceClient(device);
        }
    }
}
