using SimpleMES.Models;

namespace SimpleMES.Core
{
    public interface IDevicePollingStrategyResolver
    {
        IDevicePollingStrategy Resolve(DeviceModel device);
    }
}
