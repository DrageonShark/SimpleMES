namespace SimpleMES.Models
{
    public class AlarmRecordModel
    {
        public int AlarmId { get; set; }
        public int DeviceId { get; set; }
        public string? AlarmCode { get; set; }
        public string? AlarmLevel { get; set; }
        public string? AlarmSource { get; set; }
        public string AlarmMessage { get; set; }
        public DateTime AlarmTime { get; set; }
        public bool IsAck { get; set; } // SQL bit 对应 C# bool
        public int? AckUserId { get; set; }
        public DateTime? AckTime { get; set; }
        public DateTime? RecoverTime { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
