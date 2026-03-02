using SimpleMES.Models;

namespace SimpleMES.Services.Observer
{
    public class DeviceConfigNotifier : IDeviceConfigNotifier
    {
        public event EventHandler<DeviceConfigChangeEventArgs>? ConfigChanged;
        public void NotifyConfigChanged(DeviceModel updateDevice, ConfigChangeType changeType)
        {
            ConfigChanged?.Invoke(this, new DeviceConfigChangeEventArgs(updateDevice, changeType));
        }
    }
}
