namespace SimpleMES.Models.Dto
{
    public sealed class DeviceEventQueryCriteria
    {
        public string? Keyword { get; set; }
        public string? EventLevel { get; set; }
        public string? ProcessingStatus { get; set; }
        public int? DeviceId { get; set; }
        public string? EventType { get; set; }
        public DateTime? OccurredFrom { get; set; }
        public int Skip { get; set; }
        public int Take { get; set; } = 40;
    }
}
