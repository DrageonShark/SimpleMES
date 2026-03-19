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
            PageTitle = "\u8bbe\u5907\u770b\u677f";
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
