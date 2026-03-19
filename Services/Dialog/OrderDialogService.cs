using SimpleMES.Models;
using SimpleMES.Services.DAL;
using SimpleMES.Services.Toast;
using SimpleMES.ViewModels;
using SimpleMES.ViewModels.OrderViewModels;
using SimpleMES.Views.Orders;
using System.Windows;

namespace SimpleMES.Services.Dialog
{
    public class OrderDialogService : IOrderDialogService
    {
        private readonly IDataRepository _repository;
        private readonly IToastService _toast;

        public OrderDialogService(IDataRepository repository, IToastService toast)
        {
            _repository = repository;
            _toast = toast;
        }

        public Task<bool> ShowOrderDetailDialogAsync(OrderModel order)
        {
            return ShowDialogAsync(
                () => new OrderDetailViewModel(order, _repository, _toast),
                () => new OrderDetailWindow());
        }
        private static Task<bool> ShowDialogAsync(
            Func<DialogViewModelBase> vmFactory,
            Func<Window> windowFactory)
        {
            return Application.Current.Dispatcher.InvokeAsync(() =>
            {
                var vm = vmFactory();
                var window = windowFactory();
                vm.RequestClose += result =>
                {
                    window.DialogResult = result ?? false;
                    window.Close();
                };

                vm.RequestMessage += (title, message, isSuccess) =>
                {
                    MessageBox.Show(
                        window,
                        message,
                        title,
                        MessageBoxButton.OK,
                        isSuccess ? MessageBoxImage.Information : MessageBoxImage.Warning);
                };
                vm.RequestConfirm += (title, message, icon) =>
                {
                    var result = MessageBox.Show(
                        window,
                        message,
                        title,
                        MessageBoxButton.YesNo,
                        icon);
                    return result == MessageBoxResult.Yes;
                };
                window.Owner = Application.Current.MainWindow;
                window.DataContext = vm;
                return window.ShowDialog() == true;
            }).Task;
        }
    }
}
