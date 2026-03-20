using SimpleMES.Models;
using SimpleMES.Models.Dto;
using SimpleMES.ViewModels;
using SimpleMES.Views;
using SimpleMES.Views.Devices;
using System.Windows;

namespace SimpleMES.Services.Dialog
{
    public class DeviceDialogService : IDeviceDialogService
    {
        public async Task<bool> ShowAddDeviceDialogAsync(
            DeviceModel draft,
            Func<DeviceModel, Task<bool>> saveAsync,
            Func<DeviceModel, Task<(bool IsSuccess, string Message)>>? testAsync = null)
        {
            return await ShowDialogAsync(
                () => new DeviceAddDialogViewModel(draft, saveAsync, testAsync),
                () => new DeviceAddWindow());
        }

        public async Task<bool> ShowEditDeviceDialogAsync(DeviceDto draft, Func<DeviceDto, Task<bool>> saveAsync, Func<DeviceDto, Task<(bool IsSuccess, string Message)>>? testAsync = null)
        {
            return await ShowDialogAsync(
                () => new DeviceEditDialogViewModel(draft, saveAsync, testAsync),
                () => new DeviceEditWindow());
        }
        private static Task<bool> ShowDialogAsync(
            Func<DialogViewModelBase> vmFactory,
            Func<Window> windowFactory)
        {
            if (Application.Current is null)
            {
                var window = windowFactory();
                var vm = vmFactory();
                WireDialog(window, vm);
                return Task.FromResult(window.ShowDialog() == true);
            }

            return Application.Current.Dispatcher.InvokeAsync(() =>
            {
                var window = windowFactory();
                var vm = vmFactory();
                WireDialog(window, vm);
                window.Owner = Application.Current.MainWindow;
                return window.ShowDialog() == true;
            }).Task;
        }
        private static void WireDialog(Window window, DialogViewModelBase vm)
        {
            vm.RequestClose += result =>
            {
                window.DialogResult = true;
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
            window.DataContext = vm;
        }
    }
}
