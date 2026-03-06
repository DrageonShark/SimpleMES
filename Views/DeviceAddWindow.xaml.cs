using SimpleMES.Models.Dto;
using System.Windows;

namespace SimpleMES.Views
{
    /// <summary>
    /// DeviceAddWindow.xaml 的交互逻辑
    /// </summary>
    public partial class DeviceAddWindow : Window
    {
        public DeviceAddWindow(DeviceDto dto)
        {
            InitializeComponent();
            // DataContext 使用 DeviceDto，不是 DeviceModel（Bug 3 修复）
            DataContext = dto;
        }

        private void OnConfirmClick(object sender, RoutedEventArgs e)
        {
            var dto = DataContext as DeviceDto;
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
