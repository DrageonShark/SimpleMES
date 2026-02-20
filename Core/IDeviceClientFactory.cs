using SimpleMES.Models;

namespace SimpleMES.Core
{
    public interface IDeviceClientFactory
    {
        IDeviceClient Create(DeviceModel device);
    }
}
