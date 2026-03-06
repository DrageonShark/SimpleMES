using SimpleMES.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace SimpleMES.Views
{
    /// <summary>
    /// OrderView.xaml 的交互逻辑
    /// </summary>
    public partial class OrderView : UserControl
    {
        public OrderView()
        {
            InitializeComponent();
            DataContextChanged += OrderView_DataContextChanged;
            Unloaded += OrderView_Unloaded;
        }

        private void OrderView_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.OldValue is OrderViewModel oldVm)
                oldVm.Notification -= OnNotification;
            if (e.NewValue is OrderViewModel newVm)
                newVm.Notification += OnNotification;
        }

        private void OrderView_Unloaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is OrderViewModel vm)
                vm.Notification -= OnNotification;
        }

        private void OnNotification(string message)
        {
            MessageBox.Show(message);
        }
    }
}
