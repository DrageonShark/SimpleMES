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
        public DeviceAddWindow(Func<DeviceModel, Task<bool>> onSure)
        {
            _onSure = onSure;
            InitializeComponent();
        }
        private async void OnSure(object sender, RoutedEventArgs e)
        {
            if (DataContext is not DeviceModel d) return;
            var ok = await _onSure(d);
            if (ok) DialogResult = true;
        }
    }
}
