namespace SimpleMES.Services.Toast
{
    /// <summary>
    /// 定义 Toast 通知服务的接口，提供多种类型的弹出通知方法。
    /// </summary>
    public interface IToastService
    {
        /// <summary>
        /// 显示一条成功类型的 Toast 通知。
        /// </summary>
        /// <param name="message">要显示的通知消息内容。</param>
        /// <param name="onConfirm">通知确认后执行的回调方法，默认为 <see langword="null"/>。</param>
        /// <param name="second">通知显示的持续时间（秒），默认为 4 秒。</param>
        void Success(string message, Action? onConfirm = null, double second = 4);

        /// <summary>
        /// 显示一条错误类型的 Toast 通知。
        /// </summary>
        /// <param name="message">要显示的通知消息内容。</param>
        /// <param name="onConfirm">通知确认后执行的回调方法，默认为 <see langword="null"/>。</param>
        /// <param name="second">通知显示的持续时间（秒），默认为 4 秒。</param>
        void Error(string message, Action? onConfirm = null, double second = 4);

        /// <summary>
        /// 显示一条信息类型的 Toast 通知。
        /// </summary>
        /// <param name="message">要显示的通知消息内容。</param>
        /// <param name="onConfirm">通知确认后执行的回调方法，默认为 <see langword="null"/>。</param>
        /// <param name="second">通知显示的持续时间（秒），默认为 4 秒。</param>
        void Info(string message, Action? onConfirm = null, double second = 4);

        /// <summary>
        /// 显示一条警告类型的 Toast 通知。
        /// </summary>
        /// <param name="message">要显示的通知消息内容。</param>
        /// <param name="onConfirm">通知确认后执行的回调方法，默认为 <see langword="null"/>。</param>
        /// <param name="second">通知显示的持续时间（秒），默认为 4 秒。</param>
        void Warning(string message, Action? onConfirm = null, double second = 4);

        /// <summary>
        /// 显示一条询问类型的 Toast 通知，用于需要用户确认的场景。
        /// </summary>
        /// <param name="message">要显示的通知消息内容。</param>
        /// <param name="onConfirm">用户确认后执行的回调方法，默认为 <see langword="null"/>。</param>
        /// <param name="second">通知显示的持续时间（秒），默认为 5 秒。</param>
        void Question(string message, Action? onConfirm = null, double second = 5);
    }
}