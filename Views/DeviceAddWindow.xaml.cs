using SimpleMES.Models;
using System.Windows;

namespace SimpleMES.Views
{
    /// <summary>
    /// DeviceAddWindow.xaml 的交互逻辑
    /// </summary>
    public partial class DeviceAddWindow : Window
    {
        private readonly Func<DeviceModel, Task<bool>> _onSure;
        private readonly Func<DeviceModel, Task<(bool IsSuccess, string Message)>>? _testConnectionAsync;
        public DeviceAddWindow(Func<DeviceModel, Task<bool>> onSure, Func<DeviceModel, Task<(bool IsSuccess, string Message)>>? testConnectionAsync = null)
        {
            _onSure = onSure;
            _testConnectionAsync = testConnectionAsync;
            InitializeComponent();
        }
        private async void OnSure(object sender, RoutedEventArgs e)
        {
            if (DataContext is not DeviceModel d) return;
            var ok = await _onSure(d);
            if (ok) DialogResult = true;
        }
        private async void OnTestConnection(object sender, RoutedEventArgs e)
        {
            if (_testConnectionAsync is null || DataContext is not DeviceModel d) return;

            var result = await _testConnectionAsync(d);
            MessageBox.Show(
                result.Message,
                result.IsSuccess ? "连接测试成功" : "连接测试失败",
                MessageBoxButton.OK,
                result.IsSuccess ? MessageBoxImage.Information : MessageBoxImage.Warning);
        }
    }
}
