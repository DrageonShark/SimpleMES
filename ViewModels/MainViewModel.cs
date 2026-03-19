using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SimpleMES.Services.Security;
using SimpleMES.ViewModels.DeviceViewModels;
using SimpleMES.ViewModels.OrderViewModels;
using System.ComponentModel;

namespace SimpleMES.ViewModels
{
    public partial class MainViewModel : DialogViewModelBase
    {
        [ObservableProperty] private DialogViewModelBase _currentView = null!;
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(MenuToggleContent))]
        [NotifyPropertyChangedFor(nameof(MenuToggleToolTip))]
        private bool _isMenuCollapsed;
        [ObservableProperty] private bool _isSettingsMenuOpen;
        [ObservableProperty] private bool _isDeviceWindowMenuOpen;
        [ObservableProperty] private bool _isOrderWindowMenuOpen;
        [ObservableProperty] private UserSession _session = UserSession.Current;

        private DeviceShellViewModel DeviceView { get; }
        private OrderShellViewModel OrderView { get; }
        private ReportViewModel ReportView { get; }

        public string MenuToggleContent => IsMenuCollapsed ? "☰" : "❮";
        public string MenuToggleToolTip => IsMenuCollapsed ? "显示侧边栏" : "隐藏侧边栏";
        public string CurrentUserName => Session.CurrentUser?.UserName ?? "未登录";

        public string CurrentRoleGreeting
        {
            get
            {
                var roleText = Session.CurrentUser?.Role switch
                {
                    1 => "管理员",
                    2 => "组长",
                    3 => "员工",
                    _ => "访客"
                };
                return $"{roleText}，祝你有个美好的一天";
            }
        }

        public MainViewModel(DeviceShellViewModel deviceView, OrderShellViewModel orderView, ReportViewModel reportView)
        {
            DeviceView = deviceView;
            OrderView = orderView;
            ReportView = reportView;

            ShowMonitor();
            Session.PropertyChanged += OnSessionPropertyChanged;
        }

        [RelayCommand]
        private void ShowMonitor()
        {
            CurrentView = DeviceView;
            DeviceView.NavigateTo(DeviceModulePage.Board);
            PageTitle = DeviceView.PageTitle;
            IsDeviceWindowMenuOpen = true;
            IsOrderWindowMenuOpen = false;
        }

        [RelayCommand]
        private void ShowDeviceBoard()
        {
            CurrentView = DeviceView;
            DeviceView.NavigateTo(DeviceModulePage.Board);
            PageTitle = DeviceView.PageTitle;
            IsDeviceWindowMenuOpen = true;
            IsOrderWindowMenuOpen = false;
        }

        [RelayCommand]
        private void ShowDeviceManagement()
        {
            CurrentView = DeviceView;
            DeviceView.NavigateTo(DeviceModulePage.Management);
            PageTitle = DeviceView.PageTitle;
            IsDeviceWindowMenuOpen = true;
            IsOrderWindowMenuOpen = false;
        }

        [RelayCommand]
        private void ShowDeviceAlarm()
        {
            CurrentView = DeviceView;
            DeviceView.NavigateTo(DeviceModulePage.Alarm);
            PageTitle = DeviceView.PageTitle;
            IsDeviceWindowMenuOpen = true;
            IsOrderWindowMenuOpen = false;
        }

        [RelayCommand]
        private void ShowOrder()
        {
            CurrentView = OrderView;
            OrderView.NavigateTo(OrderModulePage.Board);
            PageTitle = OrderView.PageTitle;
            IsDeviceWindowMenuOpen = false;
            IsOrderWindowMenuOpen = true;
        }

        [RelayCommand]
        private void ShowOrderBoard()
        {
            CurrentView = OrderView;
            OrderView.NavigateTo(OrderModulePage.Board);
            PageTitle = OrderView.PageTitle;
            IsDeviceWindowMenuOpen = false;
            IsOrderWindowMenuOpen = true;
        }

        [RelayCommand]
        private void ShowManagement()
        {
            CurrentView = OrderView;
            OrderView.NavigateTo(OrderModulePage.Management);
            PageTitle = OrderView.PageTitle;
            IsDeviceWindowMenuOpen = false;
            IsOrderWindowMenuOpen = true;
        }

        [RelayCommand]
        private void ShowOrderDispatch()
        {
            CurrentView = OrderView;
            OrderView.NavigateTo(OrderModulePage.Dispatch);
            PageTitle = OrderView.PageTitle;
            IsDeviceWindowMenuOpen = false;
            IsOrderWindowMenuOpen = true;
        }

        [RelayCommand]
        private void ShowReport()
        {
            IsDeviceWindowMenuOpen = false;
            IsOrderWindowMenuOpen = false;
            PageTitle = "数据报表";
            CurrentView = ReportView;
        }

        [RelayCommand]
        private void ToggleMenu()
        {
            IsMenuCollapsed = !IsMenuCollapsed;
        }

        [RelayCommand]
        private void OpenSoftwareVersion()
        {
            IsSettingsMenuOpen = false;
            PageTitle = "软件版本";
        }

        [RelayCommand]
        private void OpenLogRecord()
        {
            IsSettingsMenuOpen = false;
            PageTitle = "日志记录";
        }

        [RelayCommand]
        private void OpenSystemConfig()
        {
            IsSettingsMenuOpen = false;
            PageTitle = "系统配置";
        }

        [RelayCommand]
        private void SignOutCurrentUser()
        {
            IsSettingsMenuOpen = false;
            if (Session.CurrentUser is null) return;
            Session.SignOut();
        }

        private void OnSessionPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName != nameof(UserSession.CurrentUser)) return;

            OnPropertyChanged(nameof(CurrentUserName));
            OnPropertyChanged(nameof(CurrentRoleGreeting));
        }
    }
}
