using SimpleMES.Models.Dto;
using System.Windows;

namespace SimpleMES.Views
{
    /// <summary>
    /// DeviceEditWindow.xaml 的交互逻辑
    /// </summary>
    public partial class DeviceEditWindow : Window
    {
        public DeviceEditWindow(DeviceDto dto)
        {
            InitializeComponent();
            DataContext = dto;
        }

        private void OnConfirmClick(object sender, RoutedEventArgs e)
        {
            var dto = DataContext as DeviceDto;
            // 空引用保护（Bug 7 修复）
            if (string.IsNullOrWhiteSpace(dto?.DeviceName))
            {
                MessageBox.Show("设备名称不能为空！", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            DialogResult = true;
            Close();
        }

        private void OnCancelClick(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
