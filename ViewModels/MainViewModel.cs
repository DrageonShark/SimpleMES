using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SimpleMES.Models;
using SimpleMES.Services.Security;

namespace SimpleMES.ViewModels
{
    public partial class MainViewModel:ViewModelBase
    {
        [ObservableProperty] private ViewModelBase _currentView;
        private readonly UserSession _session = UserSession.Current;

        public UserModel? User;
        // 定义页面对象（缓存起来，不需要每次点击都 new）
        private MonitorViewModel MonitorView { get; }
        private OrderViewModel OrderView { get; }
        private ReportViewModel ReportView { get; }
        private LoginViewModel loginView { get; }

        public MainViewModel(MonitorViewModel monitor, OrderViewModel orderView, ReportViewModel reportView, LoginViewModel loginView)
        {
            MonitorView = monitor;
            OrderView = orderView;
            ReportView = reportView;
            this.loginView = loginView;

            User = _session.CurrentUser;
            ShowLogin();
        }
        // 定义按钮命令：切换到监控页
        [RelayCommand]
        private void ShowMonitor()
        {
            PageTitle = "设备监控页面";
            CurrentView = MonitorView;
        }

        // 定义按钮命令：切换到订单页
        [RelayCommand]
        private void ShowOrder()
        {
            PageTitle = "订单页面 ";
            CurrentView = OrderView;
        }

        // 定义按钮命令：切换到报表页
        [RelayCommand]
        private void ShowReport()
        {
            PageTitle = "报表页面";
            CurrentView = ReportView;
        }
        [RelayCommand]
        private void ShowLogin()
        {
            PageTitle = "登录页面";
            CurrentView = loginView;
        }

        

        [RelayCommand]
        private void SingOut()
        {
            _session.SignOut();
        }
    }
}
