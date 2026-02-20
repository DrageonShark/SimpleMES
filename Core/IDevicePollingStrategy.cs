using SimpleMES.Models;

namespace SimpleMES.Core
{
    public interface IDevicePollingStrategy
    {
        string Key { get; }
        Task<PollingResult> PollAsync(IDeviceClient client, DeviceModel device, CancellationToken token = default);
    }
}
