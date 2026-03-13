namespace SimpleMES.Services.Toast
{
    /// <summary>
    /// Toast 消息类型（和 View 解耦，不再放在 Window 里）
    /// </summary>
    public enum ToastType
    {
        Success,
        Error,
        Info,
        Warning,
        Question
    }
}
