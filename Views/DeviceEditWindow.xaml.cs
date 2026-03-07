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
        public DeviceEditWindow(Func<DeviceDto, Task<bool>> saveAsync)
        {
            _saveAsync = saveAsync;
            InitializeComponent();
        }
        private async void OnSave(object sender, RoutedEventArgs e)
        {
            if (DataContext is not DeviceDto dto) return;
            var ok = await _saveAsync(dto);
            if (ok) DialogResult = true;// 成功则关闭
            // 失败：_saveAsync 内部会弹 MessageBox，这里不关窗
        }
    }
}
