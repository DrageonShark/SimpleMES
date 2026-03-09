namespace SimpleMES.Services.Toast
{
    public interface IToastService
    {
        void Success(string message, Action? onConfirm = null, double second = 4);
        void Error(string message, Action? onConfirm = null, double second = 4);
        void Info(string message, Action? onConfirm = null, double second = 4);
        void Warning(string message, Action? onConfirm = null, double second = 4);
        void Question(string message, Action? onConfirm = null, double second = 5);
    }
}
