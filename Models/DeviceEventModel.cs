namespace SimpleMES.Models
{
    public class DeviceEventModel
    {
        public long EventId { get; set; }
        public int DeviceId { get; set; }
        public string EventType { get; set; } = string.Empty;
        public string EventLevel { get; set; } = "Info";
        public string EventMessage { get; set; } = string.Empty;
        public string? SnapshotState { get; set; }
        public DateTime OccurredAt { get; set; }
        public int? RelatedAlarmId { get; set; }
        public bool IsResolved { get; set; }
        public DateTime? ResolvedAt { get; set; }
        public int? ConfirmedByUserId { get; set; }
        public DateTime? ConfirmedAt { get; set; }
        public string? ResolutionNote { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
