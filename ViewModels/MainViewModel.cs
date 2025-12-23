using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace SimpleMES.ViewModels
{
    public partial class MainViewModel:ViewModelBase
    {
        [ObservableProperty] private object _currentView;

        // 定义页面对象（缓存起来，不需要每次点击都 new）
        // 暂时先用 object 占位，等创建了真正的 View 再替换
        private object MonitorView { get; }
        private object OrderView { get; }
        private object ReportView { get; } = new object();

        public MainViewModel(MonitorViewModel monitor, OrderViewModel orderView)
        {
            MonitorView = monitor;
            OrderView = orderView;
            CurrentView = MonitorView;
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
    }
}
