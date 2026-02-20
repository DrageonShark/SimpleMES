using SimpleMES.Models.Dto;

namespace SimpleMES.Core
{
    public sealed class DeviceStatusChangedEventArgs : EventArgs
    {
        public IReadOnlyList<DeviceDto> LatestDevices { get; }
        public DeviceStatusChangedEventArgs(IReadOnlyList<DeviceDto> latestDevices)
        {
            LatestDevices = latestDevices ?? Array.Empty<DeviceDto>();
        }
    }
}
