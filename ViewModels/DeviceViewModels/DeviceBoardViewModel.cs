using CommunityToolkit.Mvvm.Input;

namespace SimpleMES.ViewModels.DeviceViewModels
{
    public partial class DeviceBoardViewModel : DialogViewModelBase
    {
        private readonly IDeviceWorkspaceActions _actions;

        public DeviceWorkspaceState State { get; }
        public IDeviceWorkspaceActions Actions => _actions;

        public DeviceBoardViewModel(DeviceWorkspaceState state, IDeviceWorkspaceActions actions)
        {
            State = state;
            _actions = actions;
            PageTitle = "设备看板";
        }

        [RelayCommand]
        private Task AddDevice()
        {
            return _actions.AddDeviceAsync();
        }

        [RelayCommand]
        private Task RefreshAlarms()
        {
            return _actions.RefreshAlarmsAsync();
        }
    }
}
