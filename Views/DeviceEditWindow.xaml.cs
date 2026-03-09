using SimpleMES.Models.Dto;
using System.Windows;

namespace SimpleMES.Views
{
    /// <summary>
    /// DeviceEditWindow.xaml 的交互逻辑
    /// </summary>
    public partial class DeviceEditWindow : Window
    {
        private readonly Func<DeviceDto, Task<bool>> _saveAsync;
        //连接测试回调
        private readonly Func<DeviceDto, Task<(bool IsSuccess, string Message)>>? _testConnectionAsync;
        public DeviceEditWindow(Func<DeviceDto, Task<bool>> saveAsync, Func<DeviceDto, Task<(bool IsSuccess, string Message)>>? testConnectionAsync = null)
        {
            _saveAsync = saveAsync;
            _testConnectionAsync = testConnectionAsync;
            InitializeComponent();
        }
        private async void OnSave(object sender, RoutedEventArgs e)
        {
            if (DataContext is not DeviceDto dto) return;
            var ok = await _saveAsync(dto);
            if (ok) DialogResult = true;// 成功则关闭
            // 失败：_saveAsync 内部会弹 MessageBox，这里不关窗
        }
        private async void OnTestConnection(object sender, RoutedEventArgs e)
        {
            if (_testConnectionAsync is null || DataContext is not DeviceDto d) return;

            var result = await _testConnectionAsync(d);
            MessageBox.Show(
                result.Message,
                result.IsSuccess ? "连接测试成功" : "连接测试失败",
                MessageBoxButton.OK,
                result.IsSuccess ? MessageBoxImage.Information : MessageBoxImage.Warning);
        }
    }
}
