using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SimpleMES.Models;
using SimpleMES.Services.Security;

namespace SimpleMES.ViewModels
{
    public partial class MainViewModel : ViewModelBase
    {
        [ObservableProperty] private ViewModelBase _currentView;
        private readonly UserSession _session = UserSession.Current;

        public UserModel? User;
        // 定义页面对象（缓存起来，不需要每次点击都 new）
        private MonitorViewModel MonitorView { get; }
        private OrderViewModel OrderView { get; }
        private ReportViewModel ReportView { get; }

        public MainViewModel(MonitorViewModel monitor, OrderViewModel orderView, ReportViewModel reportView)
        {
            MonitorView = monitor;
            OrderView = orderView;
            ReportView = reportView;

            User = _session.CurrentUser;
            ShowMonitor();
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
            PageTitle = "订单管理";
            CurrentView = OrderView;
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
    }
}
