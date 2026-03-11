using CommunityToolkit.Mvvm.ComponentModel;

namespace SimpleMES.ViewModels
{
    public partial class DialogViewModelBase : ObservableObject
    {
        [ObservableProperty] private string _pageTitle = "未知页面";
        public event Action<bool?>? RequestClose;
        public event Action<string, string, bool>? RequestMessage;

        protected void Close(bool? result) => RequestClose?.Invoke(result);

        protected void ShowMessage(string title, string message, bool isSuccess) =>
            RequestMessage?.Invoke(title, message, isSuccess);
    }
}
