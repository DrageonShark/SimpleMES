using SimpleMES.Models;

namespace SimpleMES.Services.Observer
{
    public class DeviceConfigChangeEventArgs : EventArgs
    {
        public DeviceModel Device { get; }
        public ConfigChangeType ChangeType { get; }
        public DeviceConfigChangeEventArgs(DeviceModel device, ConfigChangeType type)
        {
            Device = device;
            ChangeType = type;
        }
    }
}
