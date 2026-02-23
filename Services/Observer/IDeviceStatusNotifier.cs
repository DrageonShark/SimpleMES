namespace SimpleMES.Services.Observer
{
    public interface IDeviceStatusNotifier
    {
        event EventHandler<DeviceStatusChangedEventArgs>? DeviceStatusChanged;
    }
}
