using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MaterialDesignThemes.Wpf;
using SimpleMES.Services.Toast;
using System.Windows.Media;
using System.Windows.Threading;

namespace SimpleMES.ViewModels
{
    /// <summary>
    /// Toast 单条消息的 VM：
    /// 1) 提供所有可绑定属性
    /// 2) 提供按钮命令
    /// 3) 管理倒计时进度
    /// </summary>
    public partial class ToastWindowViewModel : DialogViewModelBase, IDisposable
    {
        // 2π × radius / strokeThickness = 2π × 12 / 3 ≈ 25.13（弧长对应的笔画单位数）
        private const double CircumferenceUnits = 25.13;
        private readonly double _durationSeconds;   // 提示框显示时长（秒）
        private readonly Action? _onConfirm;        // 点击确认按钮要执行的方法
        private DispatcherTimer? _timer;            // WPF定时器（控制倒计时）
        private double _elapsedSeconds;             // 已经过去的秒数
        private bool _closeRequested;               // 是否已经请求关闭（防止重复关闭）

        //界面绑定属性
        [ObservableProperty] private string _title = string.Empty;
        [ObservableProperty] private string _message = string.Empty;
        [ObservableProperty] private string _dateTimeText = string.Empty;
        [ObservableProperty] private Brush _headerBrush = Brushes.DodgerBlue; // 标题栏颜色
        [ObservableProperty] private Brush _iconBrush = Brushes.DodgerBlue;   // 图标颜色
        [ObservableProperty] private PackIconKind _iconKind = PackIconKind.InformationCircle; // 图标样式

        [ObservableProperty] private double _progressUnits = CircumferenceUnits;// 倒计时圆环进度
        [ObservableProperty] private bool _isClosing;// 是否正在关闭（触发退出动画
        [ObservableProperty] private bool _showConfirmButton;// 是否显示确认按钮

        //窗口位置
        [ObservableProperty] private double _left;
        [ObservableProperty] private double _top;

        /// <summary>
        /// 请求关闭事件，由ToastService监听并负责真正关闭窗口
        /// </summary>
        public event Action<ToastWindowViewModel>? CloseRequested;

        public ToastWindowViewModel(
            string message, ToastType type, double durationSeconds, Action? onConfirm = null)
        {
            Message = message;
            DateTimeText = DateTime.Now.ToString("yyyy/M/d HH:mm:ss");
            _durationSeconds = durationSeconds <= 0 ? 4 : durationSeconds;
            _onConfirm = onConfirm;
            ShowConfirmButton = onConfirm is not null;

            ApplyStyle(type); // 根据提示类型设置样式（颜色、图标）
            StartCountdown(); // 启动倒计时
        }

        [RelayCommand]
        private void Close()
        {
            RequestClose(); // 点击关闭按钮，请求关闭
        }

        [RelayCommand]
        private void Confirm()
        {
            _onConfirm?.Invoke(); // 执行确认回调（比如提交表单）
            RequestClose();       // 关闭提示框
        }
        /// <summary>
        /// 由 Service 调用：开始关闭状态（触发 XAML 的退出动画）
        /// </summary>
        public void BeginClosing()
        {
            if (IsClosing) return; // 已经在关闭，直接返回
            IsClosing = true;      // 标记为正在关闭
            _timer?.Stop();        // 停止倒计时
        }

        private void StartCountdown()
        {
            _elapsedSeconds = 0; // 重置已过去的秒数
            ProgressUnits = CircumferenceUnits; // 重置圆环进度（满的）

            // 创建定时器，每50毫秒触发一次（0.05秒）
            _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(50) };
            _timer.Tick += (_, _) => // 定时器每触发一次执行的逻辑
            {
                if (IsClosing) // 正在关闭，停止定时器
                {
                    _timer?.Stop();
                    return;
                }

                _elapsedSeconds += 0.05; // 累计已过去的秒数（50毫秒=0.05秒）
                // 计算剩余进度比例：1 - 已过去时间/总时长
                var ratio = 1d - (_elapsedSeconds / _durationSeconds);
                // 更新圆环进度（最小为0，避免负数）
                ProgressUnits = Math.Max(0, CircumferenceUnits * ratio);

                // 已过去时间 >= 总时长，停止定时器并关闭
                if (_elapsedSeconds >= _durationSeconds)
                {
                    _timer?.Stop();
                    RequestClose();
                }
            };
            _timer.Start(); // 启动定时器
        }

        private new void RequestClose()
        {
            // 防止重复触发关闭
            if (_closeRequested) return;
            _closeRequested = true;
            CloseRequested?.Invoke(this); // 触发关闭事件，通知ToastService关闭窗口
        }

        private void ApplyStyle(ToastType type)
        {
            switch (type)
            {
                case ToastType.Success: // 成功提示
                    Title = "成功";
                    HeaderBrush = new SolidColorBrush(Color.FromRgb(33, 150, 243)); // 标题栏蓝色
                    IconKind = PackIconKind.CheckCircle; // 对勾图标
                    IconBrush = new SolidColorBrush(Color.FromRgb(76, 175, 80)); // 图标绿色
                    break;

                case ToastType.Error: // 错误提示
                    Title = "错误";
                    HeaderBrush = new SolidColorBrush(Color.FromRgb(244, 67, 54)); // 标题栏红色
                    IconKind = PackIconKind.AlertCircle; // 警告圆图标
                    IconBrush = new SolidColorBrush(Color.FromRgb(244, 67, 54)); // 图标红色
                    break;

                case ToastType.Info: // 信息提示
                    Title = "提示";
                    HeaderBrush = new SolidColorBrush(Color.FromRgb(33, 150, 243)); // 标题栏蓝色
                    IconKind = PackIconKind.InformationCircle; // 信息圆图标
                    IconBrush = new SolidColorBrush(Color.FromRgb(33, 150, 243)); // 图标蓝色
                    break;

                case ToastType.Warning: // 警告提示
                    Title = "警告";
                    HeaderBrush = new SolidColorBrush(Color.FromRgb(255, 152, 0)); // 标题栏橙色
                    IconKind = PackIconKind.AlertOutline; // 警告轮廓图标
                    IconBrush = new SolidColorBrush(Color.FromRgb(255, 152, 0)); // 图标橙色
                    break;

                case ToastType.Question: // 询问提示
                    Title = "询问";
                    HeaderBrush = new SolidColorBrush(Color.FromRgb(103, 58, 183)); // 标题栏紫色
                    IconKind = PackIconKind.HelpCircle; // 帮助圆图标
                    IconBrush = new SolidColorBrush(Color.FromRgb(103, 58, 183)); // 图标紫色
                    break;
            }
        }

        public void Dispose()
        {
            if (_timer is null) return;
            _timer.Stop(); // 停止定时器
            _timer = null; // 释放定时器
        }
    }
}

