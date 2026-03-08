using System.Windows.Controls;

namespace SimpleMES.Views
{
    /// <summary>
    /// MonitorView.xaml 的交互逻辑
    /// </summary>
    public partial class MonitorView : UserControl
    {
        public MonitorView()
        {
            InitializeComponent();
        }

        //private void OnToastSuccess(object sender, RoutedEventArgs e) =>
        //    ToastWindow.Success("设备配置更新成功！");

        //private void OnToastError(object sender, RoutedEventArgs e) =>
        //    ToastWindow.Error("连接设备失败，请检查网络配置。");

        //private void OnToastInfo(object sender, RoutedEventArgs e) =>
        //    ToastWindow.Info("系统正在同步数据，请稍候。");

        //private void OnToastWarning(object sender, RoutedEventArgs e) =>
        //    ToastWindow.Warning("设备温度超出正常范围，请注意！");

        //private void OnToastQuestion(object sender, RoutedEventArgs e) =>
        //    ToastWindow.Question("确认要删除该设备配置吗？",
        //        onConfirm: () => ToastWindow.Info("已确认删除。"));
    }
}

