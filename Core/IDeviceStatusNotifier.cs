namespace SimpleMES.Core
{
    public interface IDeviceStatusNotifier
    {
        event EventHandler<DeviceStatusChangedEventArgs>? DeviceStatusChanged;
    }
}
