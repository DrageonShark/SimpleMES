using CommunityToolkit.Mvvm.ComponentModel;
using System.Windows;

namespace SimpleMES.ViewModels
{
    public partial class DialogViewModelBase : ObservableObject
    {
        [ObservableProperty] private string _pageTitle = "未知页面";
        public event Action<bool?>? RequestClose;
        public event Action<string, string, bool>? RequestMessage;
        public event Func<string, string, MessageBoxImage, bool>? RequestConfirm;

        protected void Close(bool? result) => RequestClose?.Invoke(result);

        protected void ShowMessage(string title, string message, bool isSuccess) =>
            RequestMessage?.Invoke(title, message, isSuccess);
        protected bool Confirm(string title, string message, MessageBoxImage icon = MessageBoxImage.Question) =>
            RequestConfirm?.Invoke(title, message, icon) ?? false;
    }
}
