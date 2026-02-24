using SimpleMES.Core;
using SimpleMES.Models;

namespace SimpleMES.Services.Strategy
{
    public interface IDevicePollingStrategy
    {
        string Key { get; }
        Task<PollingResult> PollAsync(IDeviceClient client, DeviceModel device, CancellationToken token = default);
    }
}
