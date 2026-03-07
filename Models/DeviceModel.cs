namespace SimpleMES.Models
{
    public class DeviceModel
    {
        public int DeviceId { get; set; }
        public string DeviceName { get; set; }
        public string IpAddress { get; set; }
        public int? Port { get; set; }
        public string SerialPort { get; set; }
        public byte? SlaveId { get; set; }
        public string DeviceState { get; set; }

        public DateTime LastUpdateTime { get; set; }
    }
}
