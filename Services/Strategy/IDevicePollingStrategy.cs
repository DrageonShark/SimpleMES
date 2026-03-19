using SimpleMES.Core;
using SimpleMES.Models;

namespace SimpleMES.Services.Strategy
{
    public interface IDevicePollingStrategy
    {
        string Key { get; }
        Task<PollingResult> PollAsync(IDeviceClient client, MonitoredDeviceModel device, CancellationToken token = default);
    }
}
