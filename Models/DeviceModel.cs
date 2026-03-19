namespace SimpleMES.Models
{
    public class DeviceModel
    {
        public int DeviceId { get; set; }
        public string DeviceName { get; set; } = string.Empty;
        public string? DeviceCode { get; set; }
        public string? DeviceType { get; set; }
        public string? WorkshopName { get; set; }
        public string? LineName { get; set; }
        public string? StationName { get; set; }
        public string IpAddress { get; set; } = string.Empty;
        public int? Port { get; set; }
        public string SerialPort { get; set; } = string.Empty;
        public byte? SlaveId { get; set; }
        public bool IsEnabled { get; set; }
        public byte Criticality { get; set; }
        public int SortOrder { get; set; }
        public string? Remark { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
