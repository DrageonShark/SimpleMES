using SimpleMES.Views;
using System.Windows;

namespace SimpleMES.Services.Toast
{
    /// <summary>
    /// IToastService 的 WPF 实现，内部委托给 ToastWindow。
    /// View 层代码保留在此，ViewModel 只感知接
    /// </summary>
    public class ToastService : IToastService
    {
        public void Success(string message, Action? onConfirm = null, double second = 4) =>
            Show(message, ToastWindow.ToastType.Success, onConfirm, second);

        public void Error(string message, Action? onConfirm = null, double second = 4) =>
            Show(message, ToastWindow.ToastType.Error, onConfirm, second);

        public void Info(string message, Action? onConfirm = null, double second = 4) =>
            Show(message, ToastWindow.ToastType.Info, onConfirm, second);

        public void Warning(string message, Action? onConfirm = null, double second = 4) =>
            Show(message, ToastWindow.ToastType.Warning, onConfirm, second);

        public void Question(string message, Action? onConfirm = null, double second = 5) =>
            Show(message, ToastWindow.ToastType.Question, onConfirm, second);

        private static void Show(string message, ToastWindow.ToastType type, Action? onConfirm, double second)
        {
            if (Application.Current?.Dispatcher is null)
            {
                var w = new ToastWindow(message, type, second, onConfirm);
                w.Show();
                return;
            }

            Application.Current.Dispatcher.Invoke(() =>
            {
                var w = new ToastWindow(message, type, second, onConfirm);
                w.Show();
            });
        }
    }
}

