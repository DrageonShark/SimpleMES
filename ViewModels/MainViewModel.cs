using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SimpleMES.Models;
using SimpleMES.Services.Security;
using System.ComponentModel;

namespace SimpleMES.ViewModels
{
    public partial class MainViewModel : DialogViewModelBase
    {
        public event Action? SignOutRequested;
        [ObservableProperty] private DialogViewModelBase _currentView;
        //侧边栏辅助属性，通知UI刷新内容显示 
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(MenuToggleContent))]
        [NotifyPropertyChangedFor(nameof(MenuToggleToolTip))]
        private bool _isMenuCollapsed = false;
        public string MenuToggleContent => IsMenuCollapsed ? "☰" : "❮";
        public string MenuToggleToolTip => IsMenuCollapsed ? "显示侧边栏" : "隐藏侧边栏";
        [ObservableProperty] private bool _isSettingsMenuOpen;
        [ObservableProperty] private bool _isOrderWindowMenuOpen;
        [ObservableProperty] private UserSession _session = UserSession.Current;

        public UserModel? User;
        // 定义页面对象（缓存起来，不需要每次点击都 new）
        private MonitorViewModel MonitorView { get; }
        private OrderShellViewModel OrderView { get; }
        private ReportViewModel ReportView { get; }


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

        public MainViewModel(MonitorViewModel monitor, OrderShellViewModel orderView, ReportViewModel reportView)
        {
            MonitorView = monitor;
            OrderView = orderView;
            ReportView = reportView;

            User = _session.CurrentUser;
            ShowMonitor();
            Session.PropertyChanged += OnSessionPropertyChanged;
        }
        // 定义按钮命令：切换到监控页
        [RelayCommand]
        private void ShowMonitor()
        {
            PageTitle = "设备监控";
            CurrentView = MonitorView;
        }

        // 定义按钮命令：切换到订单页
        [RelayCommand]
        private void ShowOrder()
        {
            IsOrderWindowMenuOpen = false;
            PageTitle = "订单管理";
            CurrentView = OrderView;
        }

        [RelayCommand]
        private void ShowOrderBoard()
        {
            IsOrderWindowMenuOpen = false;
            PageTitle = "订单看板";
            CurrentView = null;
        }
        [RelayCommand]
        private void ShowManagement()
        {
            IsOrderWindowMenuOpen = false;
            PageTitle = "订单管理";
            CurrentView = null;
        }
        [RelayCommand]
        private void ShowOrderDispatch()
        {
            PageTitle = "订单调度";
            CurrentView = null;
        }

        // 定义按钮命令：切换到报表页
        [RelayCommand]
        private void ShowReport()
        {
            PageTitle = "数据报表";
            CurrentView = ReportView;
        }

        [RelayCommand]
        private void SingOut()
        {
            _session.SignOut();
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
