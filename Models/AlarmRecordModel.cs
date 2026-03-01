namespace SimpleMES.Models
{
    public class AlarmRecordModel
    {
        public int AlarmId { get; set; }
        public int DeviceId { get; set; }
        public string AlarmMessage { get; set; }
        public DateTime AlarmTime { get; set; }
        public bool IsAck { get; set; } // SQL bit 对应 C# bool
    }
}
