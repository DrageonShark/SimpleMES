using SimpleMES.Models;
using SimpleMES.Services.Strategy;

namespace SimpleMES.Core
{
    public interface IDevicePollingStrategy
    {
        string Key { get; }
        Task<PollingResult> PollAsync(IDeviceClient client, MonitoredDeviceModel device, CancellationToken token = default);
    }
}
