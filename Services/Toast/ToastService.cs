using SimpleMES.Views;

namespace SimpleMES.Services.Toast
{
    /// <summary>
    /// IToastService 的 WPF 实现，内部委托给 ToastWindow。
    /// View 层代码保留在此，ViewModel 只感知接
    /// </summary>
    public class ToastService : IToastService
    {
        public void Success(string message, Action? onConfirm = null, double second = 4) =>
            ToastWindow.Success(message, onConfirm, second);

        public void Error(string message, Action? onConfirm = null, double second = 4) =>
            ToastWindow.Error(message, onConfirm, second);

        public void Info(string message, Action? onConfirm = null, double second = 4) =>
            ToastWindow.Info(message, onConfirm, second);

        public void Warning(string message, Action? onConfirm = null, double second = 4) =>
            ToastWindow.Warning(message, onConfirm, second);

        public void Question(string message, Action? onConfirm = null, double second = 5) =>
            ToastWindow.Question(message, onConfirm, second);
    }
}
