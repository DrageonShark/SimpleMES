namespace SimpleMES.Models.Dto
{
    public sealed class DeviceEventQueryResult
    {
        public int TotalCount { get; set; }
        public IReadOnlyList<DeviceEventDto> Items { get; set; } = Array.Empty<DeviceEventDto>();
    }
}
