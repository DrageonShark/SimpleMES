namespace SimpleMES.Models
{
    public class DeviceRuntimeModel
    {
        public int DeviceId { get; set; }
        public string DeviceState { get; set; } = "Disconnected";
        public DateTime LastUpdateTime { get; set; }
        public DateTime? LastHeartbeatTime { get; set; }
        public DateTime? LastStateChangeTime { get; set; }
        public string? CurrentOrderNo { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
