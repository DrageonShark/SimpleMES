using SimpleMES.Models;

namespace SimpleMES.Services.Strategy
{
    public interface IDevicePollingStrategyResolver
    {
        IDevicePollingStrategy Resolve(DeviceModel device);
    }
}
