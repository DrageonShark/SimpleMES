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

        // Sidebar active states: used only for view highlighting.
        [ObservableProperty] private bool _isDeviceBoardActive;
        [ObservableProperty] private bool _isDeviceManagementActive;
        [ObservableProperty] private bool _isDeviceAlarmActive;
        [ObservableProperty] private bool _isOrderBoardActive;
        [ObservableProperty] private bool _isOrderManagementActive;
        [ObservableProperty] private bool _isOrderDispatchActive;
        [ObservableProperty] private bool _isReportActive;

        [ObservableProperty] private UserSession _session = UserSession.Current;

        private DeviceShellViewModel DeviceView { get; }
        private OrderShellViewModel OrderView { get; }
        private ReportViewModel ReportView { get; }

        /// <summary>
        /// Identifies the current highlighted navigation item in the sidebar.
        /// </summary>
        private enum NavigationItem
        {
            DeviceBoard,
            DeviceManagement,
            DeviceAlarm,
            OrderBoard,
            OrderManagement,
            OrderDispatch,
            Report
        }

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
            SetActiveNavigation(NavigationItem.DeviceBoard);
        }

        [RelayCommand]
        private void ShowDeviceBoard()
        {
            CurrentView = DeviceView;
            DeviceView.NavigateTo(DeviceModulePage.Board);
            PageTitle = DeviceView.PageTitle;
            IsDeviceWindowMenuOpen = true;
            IsOrderWindowMenuOpen = false;
            SetActiveNavigation(NavigationItem.DeviceBoard);
        }

        [RelayCommand]
        private void ShowDeviceManagement()
        {
            CurrentView = DeviceView;
            DeviceView.NavigateTo(DeviceModulePage.Management);
            PageTitle = DeviceView.PageTitle;
            IsDeviceWindowMenuOpen = true;
            IsOrderWindowMenuOpen = false;
            SetActiveNavigation(NavigationItem.DeviceManagement);
        }

        [RelayCommand]
        private void ShowDeviceAlarm()
        {
            CurrentView = DeviceView;
            DeviceView.NavigateTo(DeviceModulePage.Alarm);
            PageTitle = DeviceView.PageTitle;
            IsDeviceWindowMenuOpen = true;
            IsOrderWindowMenuOpen = false;
            SetActiveNavigation(NavigationItem.DeviceAlarm);
        }

        [RelayCommand]
        private void ShowOrder()
        {
            CurrentView = OrderView;
            OrderView.NavigateTo(OrderModulePage.Board);
            PageTitle = OrderView.PageTitle;
            IsDeviceWindowMenuOpen = false;
            IsOrderWindowMenuOpen = true;
            SetActiveNavigation(NavigationItem.OrderBoard);
        }

        [RelayCommand]
        private void ShowOrderBoard()
        {
            CurrentView = OrderView;
            OrderView.NavigateTo(OrderModulePage.Board);
            PageTitle = OrderView.PageTitle;
            IsDeviceWindowMenuOpen = false;
            IsOrderWindowMenuOpen = true;
            SetActiveNavigation(NavigationItem.OrderBoard);
        }

        [RelayCommand]
        private void ShowManagement()
        {
            CurrentView = OrderView;
            OrderView.NavigateTo(OrderModulePage.Management);
            PageTitle = OrderView.PageTitle;
            IsDeviceWindowMenuOpen = false;
            IsOrderWindowMenuOpen = true;
            SetActiveNavigation(NavigationItem.OrderManagement);
        }

        [RelayCommand]
        private void ShowOrderDispatch()
        {
            CurrentView = OrderView;
            OrderView.NavigateTo(OrderModulePage.Dispatch);
            PageTitle = OrderView.PageTitle;
            IsDeviceWindowMenuOpen = false;
            IsOrderWindowMenuOpen = true;
            SetActiveNavigation(NavigationItem.OrderDispatch);
        }

        [RelayCommand]
        private void ShowReport()
        {
            IsDeviceWindowMenuOpen = false;
            IsOrderWindowMenuOpen = false;
            PageTitle = "数据报表";
            CurrentView = ReportView;
            SetActiveNavigation(NavigationItem.Report);
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
            if (Session.CurrentUser is null)
            {
                return;
            }

            Session.SignOut();
        }

        private void OnSessionPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName != nameof(UserSession.CurrentUser))
            {
                return;
            }

            OnPropertyChanged(nameof(CurrentUserName));
            OnPropertyChanged(nameof(CurrentRoleGreeting));
        }

        /// <summary>
        /// Switches the sidebar highlight in one place.
        /// </summary>
        private void SetActiveNavigation(NavigationItem navigationItem)
        {
            ResetNavigationState();

            switch (navigationItem)
            {
                case NavigationItem.DeviceBoard:
                    IsDeviceBoardActive = true;
                    break;
                case NavigationItem.DeviceManagement:
                    IsDeviceManagementActive = true;
                    break;
                case NavigationItem.DeviceAlarm:
                    IsDeviceAlarmActive = true;
                    break;
                case NavigationItem.OrderBoard:
                    IsOrderBoardActive = true;
                    break;
                case NavigationItem.OrderManagement:
                    IsOrderManagementActive = true;
                    break;
                case NavigationItem.OrderDispatch:
                    IsOrderDispatchActive = true;
                    break;
                case NavigationItem.Report:
                    IsReportActive = true;
                    break;
            }
        }

        /// <summary>
        /// Clears all sidebar highlight states before applying a new one.
        /// </summary>
        private void ResetNavigationState()
        {
            IsDeviceBoardActive = false;
            IsDeviceManagementActive = false;
            IsDeviceAlarmActive = false;
            IsOrderBoardActive = false;
            IsOrderManagementActive = false;
            IsOrderDispatchActive = false;
            IsReportActive = false;
        }
    }
}
