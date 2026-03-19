using SimpleMES.Models.Dto;

namespace SimpleMES.Services.Observer
{
    public sealed class DeviceStatusChangedEventArgs : EventArgs
    {
        public IReadOnlyList<DeviceDto> LatestDevices { get; }
        public IReadOnlyList<DeviceEventDto> RecentEvents { get; }

        public DeviceStatusChangedEventArgs(IReadOnlyList<DeviceDto> latestDevices, IReadOnlyList<DeviceEventDto>? recentEvents = null)
        {
            LatestDevices = latestDevices ?? Array.Empty<DeviceDto>();
            RecentEvents = recentEvents ?? Array.Empty<DeviceEventDto>();
        }
    }
}
