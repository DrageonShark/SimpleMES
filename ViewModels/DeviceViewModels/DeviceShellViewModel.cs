using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace SimpleMES.ViewModels.DeviceViewModels
{
    public enum DeviceModulePage
    {
        Board,
        Management,
        Alarm
    }

    public partial class DeviceShellViewModel : DialogViewModelBase
    {
        private readonly DeviceBoardViewModel _boardViewModel;
        private readonly DeviceManagementViewModel _managementViewModel;
        private readonly DeviceAlarmViewModel _alarmViewModel;

        [ObservableProperty]
        private DialogViewModelBase _currentChild = null!;

        public DeviceShellViewModel(
            DeviceBoardViewModel boardViewModel,
            DeviceManagementViewModel managementViewModel,
            DeviceAlarmViewModel alarmViewModel)
        {
            _boardViewModel = boardViewModel;
            _managementViewModel = managementViewModel;
            _alarmViewModel = alarmViewModel;

            NavigateTo(DeviceModulePage.Board);
        }

        public void NavigateTo(DeviceModulePage page)
        {
            switch (page)
            {
                case DeviceModulePage.Board:
                    CurrentChild = _boardViewModel;
                    PageTitle = "\u8bbe\u5907\u770b\u677f";
                    break;
                case DeviceModulePage.Management:
                    CurrentChild = _managementViewModel;
                    PageTitle = "\u8bbe\u5907\u7ba1\u7406";
                    break;
                case DeviceModulePage.Alarm:
                    CurrentChild = _alarmViewModel;
                    PageTitle = "\u8bbe\u5907\u544a\u8b66";
                    break;
            }
        }

        [RelayCommand]
        private void ShowBoard() => NavigateTo(DeviceModulePage.Board);

        [RelayCommand]
        private void ShowManagement() => NavigateTo(DeviceModulePage.Management);

        [RelayCommand]
        private void ShowAlarm() => NavigateTo(DeviceModulePage.Alarm);
    }
}
