namespace SimpleMES.Models
{
    public class MonitoredDeviceModel
    {
        public DeviceModel Device { get; set; } = new();
        public DeviceRuntimeModel Runtime { get; set; } = new();

        public int DeviceId => Device.DeviceId;
        public string DeviceName => Device.DeviceName;
        public string IpAddress => Device.IpAddress;
        public string SerialPort => Device.SerialPort;
        public int? Port => Device.Port;
        public byte? SlaveId => Device.SlaveId;
        public bool IsEnabled => Device.IsEnabled;

        public string DeviceState
        {
            get => Runtime.DeviceState;
            set => Runtime.DeviceState = value;
        }

        public DateTime LastUpdateTime
        {
            get => Runtime.LastUpdateTime;
            set => Runtime.LastUpdateTime = value;
        }
    }
}
