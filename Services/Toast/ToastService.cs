using SimpleMES.ViewModels;
using SimpleMES.Views;
using System.Windows;

namespace SimpleMES.Services.Toast
{
    /// <summary>
    /// Toast 的调度中心：
    /// 1) 负责创建 Window + VM
    /// 2) 负责多条 Toast 的堆叠位置
    /// 3) 负责关闭时机（含退出动画等待）
    /// </summary>
    public class ToastService : IToastService
    {
        // Toast 窗口固定宽度
        private const double ToastWidth = 340;
        // 窗口离屏幕边缘的距离
        private const double ScreenMargin = 16;
        // 多个Toast之间的间距
        private const double ToastSpacing = 10;
        // 退出动画的时长（毫秒）
        private const int CloseAnimationMilliseconds = 500;

        // 线程锁：保证多线程操作_entries时不冲突
        private readonly object _sync = new();
        // 存储所有正在显示的Toast窗口信息
        private readonly List<ToastEntry> _entries = new();

        /// <summary>
        /// 将窗口、VM和关闭状态打包在一起辅助维护通知列表
        /// </summary>
        private sealed class ToastEntry
        {
            public ToastEntry(ToastWindow window, ToastWindowViewModel viewModel)
            {
                Window = window;
                ViewModel = viewModel;
            }

            public ToastWindow Window { get; }          // Toast窗口
            public ToastWindowViewModel ViewModel { get; } // 对应的VM
            public bool IsClosing { get; set; }         // 是否正在关闭（防止重复关闭）
        }

        public void Success(string message, Action? onConfirm = null, double second = 4) =>
            Show(message, ToastType.Success, onConfirm, second);

        public void Error(string message, Action? onConfirm = null, double second = 4) =>
            Show(message, ToastType.Error, onConfirm, second);

        public void Info(string message, Action? onConfirm = null, double second = 4) =>
            Show(message, ToastType.Info, onConfirm, second);

        public void Warning(string message, Action? onConfirm = null, double second = 4) =>
            Show(message, ToastType.Warning, onConfirm, second);

        public void Question(string message, Action? onConfirm = null, double second = 5) =>
            Show(message, ToastType.Question, onConfirm, second);
        /// <summary>
        /// 创建并显示一个 <see cref="ToastWindow"/> 通知窗口。
        /// <para>
        /// 若 <see cref="Application.Current"/> 的 <see cref="System.Windows.Threading.Dispatcher"/>
        /// 不可用（如单元测试或非 UI 线程初始化阶段），则直接返回；
        /// 否则换到UI线程执行，以确保线程安全。
        /// </para>
        /// </summary>
        /// <param name="message">要显示的通知消息内容。</param>
        /// <param name="type">Toast 通知的类型，决定图标与样式。</param>
        /// <param name="onConfirm">通知确认后执行的回调方法，为 <see langword="null"/> 时不执行任何操作。</param>
        /// <param name="second">通知显示的持续时间（秒）。</param>
        private void Show(string message, ToastType type, Action? onConfirm, double second)
        {
            if (Application.Current?.Dispatcher is null) return;

            // 切换到UI线程执行（WPF的界面操作必须在UI线程）
            Application.Current.Dispatcher.Invoke(() =>
            {
                ShowOnUiThread(message, type, onConfirm, second);
            });
        }

        private void ShowOnUiThread(string message, ToastType type, Action? onConfirm, double second)
        {
            // 1. 创建VM
            var vm = new ToastWindowViewModel(message, type, second, onConfirm);
            // 2. 创建Toast窗口，把VM传给窗口（数据绑定）
            var window = new ToastWindow(vm);
            // 3. 打包成ToastEntry，加入管理列表
            var entry = new ToastEntry(window, vm);

            // 4. 监听VM的CloseRequested事件：VM请求关闭时，由Service处理关闭流程
            vm.CloseRequested += toastVm => { _ = CloseEntryAsync(entry); };

            // 5. 监听窗口Closed事件：窗口真正关闭后回收资源
            window.Closed += (_, _) => OnWindowClosed(entry);

            // 6. 窗口内容渲染完成后重排位置（此时窗口高度更准确）
            window.ContentRendered += (_, _) => Relayout();

            // 7. 加锁把entry加入列表（多线程安全）
            lock (_sync)
            {
                _entries.Add(entry);
            }

            // 8. 显示窗口
            window.Show();
            // 9. 重排所有Toast的位置（新窗口加进来了，要调整位置）
            Relayout();
        }

        /// <summary>
        /// 通知关闭逻辑
        /// </summary>
        private async Task CloseEntryAsync(ToastEntry entry)
        {
            // 如果已经在关闭，直接返回（防止重复关闭）
            if (entry.IsClosing) return;
            entry.IsClosing = true;

            // 通知VM开始关闭,触发XAML里的退出动画
            entry.ViewModel.BeginClosing();

            // 等待动画完成
            await Task.Delay(CloseAnimationMilliseconds);

            // 如果窗口还显示着，就真正关闭它
            if (entry.Window.IsVisible)
            {
                entry.Window.Close();
            }
        }

        /// <summary>
        /// 当前toast弹窗相关联的窗口关闭时，执行弹窗资源的清理和移除。
        /// </summary>
        private void OnWindowClosed(ToastEntry entry)
        {
            // 释放VM的资源（停止定时器等）
            entry.ViewModel.Dispose();

            // 加锁从列表中移除当前Toast（多线程安全）
            lock (_sync)
            {
                _entries.Remove(entry);
            }

            // 重排剩余Toast的位置
            Relayout();
        }
        /// <summary>
        /// 重新计算并更新所有活动弹出通知的位置
        /// </summary>
        private void Relayout()
        {
            // 检查是否在UI线程，不在的话切换到UI线程
            if (Application.Current?.Dispatcher is null) return;
            if (!Application.Current.Dispatcher.CheckAccess())
            {
                Application.Current.Dispatcher.Invoke(Relayout);
                return;
            }

            // 加锁复制一份_entries列表（防止遍历过程中列表被修改）
            List<ToastEntry> snapshot;
            lock (_sync)
            {
                snapshot = _entries.ToList();
            }

            // 获取屏幕工作区（排除任务栏的区域）
            var area = SystemParameters.WorkArea;
            // Toast的X坐标：屏幕右侧 - 窗口宽度 - 边缘间距（靠右显示）
            var left = area.Right - ToastWidth - ScreenMargin;
            // 初始Y坐标：屏幕底部 - 边缘间距（从下往上排）
            var bottom = area.Bottom - ScreenMargin;

            // 从后往前遍历（最新的Toast在最下面）
            for (var i = snapshot.Count - 1; i >= 0; i--)
            {
                var entry = snapshot[i];
                // 获取窗口高度：如果ActualHeight>0就用实际高度，否则默认180
                var height = entry.Window.ActualHeight > 0 ? entry.Window.ActualHeight : 180;
                // 当前Toast的Top坐标：底部坐标 - 窗口高度
                var top = bottom - height;

                // 把位置赋值给VM（VM的Left/Top属性绑定到窗口的位置）
                entry.Window.Left = left;
                entry.Window.Top = top;

                // 保留VM赋值也可以（给你后续做动画/调试用）
                entry.ViewModel.Left = left;
                entry.ViewModel.Top = top;


                // 更新bottom：为下一个（上面的）Toast预留位置（当前Top - 间距）
                bottom = top - ToastSpacing;
            }
        }
    }
}

