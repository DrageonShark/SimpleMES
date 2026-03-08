using MaterialDesignThemes.Wpf;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace SimpleMES.Views
{
    /// <summary>
    /// 右下角 Toast 通知窗口
    /// 支持 Success / Error / Info / Warning / Question 五种类型
    /// 多条通知自动堆叠，带滑入/滑出动画和圆形倒计时环
    /// </summary>
    public partial class ToastWindow : Window
    {
        // ── 类型枚举 ──────────────────────────────────────────────────────────
        public enum ToastType { Success, Error, Info, Warning, Question }

        // ── 布局常量 ──────────────────────────────────────────────────────────
        private const double ToastWidth          = 340;
        private const double ScreenMargin        = 16;
        private const double ToastSpacing        = 10;
        // 2π × radius / strokeThickness = 2π × 12 / 3 ≈ 25.13（弧长对应的笔画单位数）
        private const double CircumferenceUnits  = 25.13;

        // ── 多实例管理 ────────────────────────────────────────────────────────
        private static readonly List<ToastWindow> _activeToasts = new();
        private static readonly object _lock = new();

        // ── 实例字段 ──────────────────────────────────────────────────────────
        private DispatcherTimer? _timer;
        private readonly double  _durationSeconds;
        private double           _elapsed;
        private readonly Action? _onConfirm;
        private bool             _isClosing;

        // ── 构造 ──────────────────────────────────────────────────────────────
        private ToastWindow(string message, ToastType type, double durationSeconds, Action? onConfirm = null)
        {
            InitializeComponent();
            _durationSeconds = durationSeconds;
            _onConfirm       = onConfirm;

            MessageText.Text  = message;
            DateTimeText.Text = DateTime.Now.ToString("yyyy/M/d HH:mm:ss");

            SetToastStyle(type);

            // 初始化进度环为满圆
            ProgressRing.StrokeDashArray = new DoubleCollection { CircumferenceUnits, 999 };

            HeaderCloseBtn.Click += (_, _) => BeginCloseAnimation();
            CloseBtn.Click       += (_, _) => BeginCloseAnimation();
            ConfirmBtn.Click     += (_, _) => { _onConfirm?.Invoke(); BeginCloseAnimation(); };

            Loaded += OnLoaded;
        }

        // ── 加载后：定位 + 滑入动画 + 启动倒计时 ────────────────────────────
        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            lock (_lock) { _activeToasts.Add(this); }

            double targetLeft = SystemParameters.WorkArea.Right - ToastWidth - ScreenMargin;

            // 计算所有 Toast 的目标 Top
            var positions = GetToastPositions();
            foreach (var (toast, targetTop) in positions)
            {
                if (toast == this)
                {
                    this.Top = targetTop;
                }
                else
                {
                    // 已有 Toast 向上动画腾出空间
                    var anim = new DoubleAnimation(toast.Top, targetTop,
                        new Duration(TimeSpan.FromSeconds(0.3)))
                    { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } };
                    toast.BeginAnimation(TopProperty, anim);
                }
            }

            // 从屏幕右侧外滑入
            this.Left = SystemParameters.WorkArea.Right + 20;
            var slideIn = new DoubleAnimation(this.Left, targetLeft,
                new Duration(TimeSpan.FromSeconds(0.4)))
            { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } };
            BeginAnimation(LeftProperty, slideIn);

            StartProgressRing();
        }

        // ── 样式设置：颜色 + 图标 ─────────────────────────────────────────────
        private void SetToastStyle(ToastType type)
        {
            switch (type)
            {
                case ToastType.Success:
                    TitleText.Text          = "成功";
                    HeaderBorder.Background = new SolidColorBrush(Color.FromRgb(33, 150, 243));
                    StatusIcon.Kind         = PackIconKind.CheckCircle;
                    StatusIcon.Foreground   = new SolidColorBrush(Color.FromRgb(76, 175, 80));
                    break;
                case ToastType.Error:
                    TitleText.Text          = "错误";
                    HeaderBorder.Background = new SolidColorBrush(Color.FromRgb(244, 67, 54));
                    StatusIcon.Kind         = PackIconKind.AlertCircle;
                    StatusIcon.Foreground   = new SolidColorBrush(Color.FromRgb(244, 67, 54));
                    break;
                case ToastType.Info:
                    TitleText.Text          = "提示";
                    HeaderBorder.Background = new SolidColorBrush(Color.FromRgb(33, 150, 243));
                    StatusIcon.Kind         = PackIconKind.InformationCircle;
                    StatusIcon.Foreground   = new SolidColorBrush(Color.FromRgb(33, 150, 243));
                    break;
                case ToastType.Warning:
                    TitleText.Text          = "警告";
                    HeaderBorder.Background = new SolidColorBrush(Color.FromRgb(255, 152, 0));
                    StatusIcon.Kind         = PackIconKind.AlertOutline;
                    StatusIcon.Foreground   = new SolidColorBrush(Color.FromRgb(255, 152, 0));
                    break;
                case ToastType.Question:
                    TitleText.Text          = "询问";
                    HeaderBorder.Background = new SolidColorBrush(Color.FromRgb(103, 58, 183));
                    StatusIcon.Kind         = PackIconKind.HelpCircle;
                    StatusIcon.Foreground   = new SolidColorBrush(Color.FromRgb(103, 58, 183));
                    break;
            }
        }

        // ── 圆形进度环倒计时（DispatcherTimer，每 50ms 更新一次弧线长度）────
        private void StartProgressRing()
        {
            _elapsed = 0;
            _timer   = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(50) };
            _timer.Tick += (_, _) =>
            {
                if (_isClosing) { _timer!.Stop(); return; }

                _elapsed += 0.05;
                double remaining = CircumferenceUnits * (1.0 - _elapsed / _durationSeconds);
                if (remaining < 0) remaining = 0;

                // 动态缩短弧线，实现顺时针消失效果
                ProgressRing.StrokeDashArray = new DoubleCollection { remaining, 999 };

                if (_elapsed >= _durationSeconds)
                {
                    _timer!.Stop();
                    BeginCloseAnimation();
                }
            };
            _timer.Start();
        }

        // ── 关闭动画：向右滑出 + 淡出（约 1 秒）────────────────────────────
        private void BeginCloseAnimation()
        {
            if (_isClosing) return;
            _isClosing = true;
            _timer?.Stop();

            double targetLeft = SystemParameters.WorkArea.Right + 30;

            var slideOut = new DoubleAnimation(Left, targetLeft,
                new Duration(TimeSpan.FromSeconds(0.9)))
            { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn } };

            var fadeOut = new DoubleAnimation(1.0, 0.0,
                new Duration(TimeSpan.FromSeconds(0.9)));

            slideOut.Completed += (_, _) =>
            {
                lock (_lock) { _activeToasts.Remove(this); }

                // 剩余 Toast 向下补位
                foreach (var (toast, targetTop) in GetToastPositions())
                {
                    var anim = new DoubleAnimation(toast.Top, targetTop,
                        new Duration(TimeSpan.FromSeconds(0.3)))
                    { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } };
                    toast.BeginAnimation(TopProperty, anim);
                }
                Close();
            };

            BeginAnimation(LeftProperty,    slideOut);
            BeginAnimation(OpacityProperty, fadeOut);
        }

        // ── 计算所有活跃 Toast 的目标 Top（最新在最下，旧的往上叠）─────────
        private static List<(ToastWindow toast, double targetTop)> GetToastPositions()
        {
            var screenArea = SystemParameters.WorkArea;
            var result     = new List<(ToastWindow, double)>();
            double bottom  = screenArea.Bottom - ScreenMargin;

            lock (_lock)
            {
                for (int i = _activeToasts.Count - 1; i >= 0; i--)
                {
                    var toast     = _activeToasts[i];
                    double height = toast.ActualHeight > 0 ? toast.ActualHeight : 180;
                    double top    = bottom - height;
                    result.Add((toast, top));
                    bottom = top - ToastSpacing;
                }
            }
            return result;
        }

        // ── 静态入口 API ──────────────────────────────────────────────────────
        public static void Show(string message, ToastType type, Action? onConfirm = null, double second = 4) =>
            Application.Current.Dispatcher.Invoke(() =>
                new ToastWindow(message, type, second, onConfirm).Show());

        public static void Success(string message, Action? onConfirm = null, double second = 4) =>
            Show(message, ToastType.Success, onConfirm, second);

        public static void Error(string message, Action? onConfirm = null, double second = 4) =>
            Show(message, ToastType.Error, onConfirm, second);

        public static void Info(string message, Action? onConfirm = null, double second = 4) =>
            Show(message, ToastType.Info, onConfirm, second);

        public static void Warning(string message, Action? onConfirm = null, double second = 4) =>
            Show(message, ToastType.Warning, onConfirm, second);

        public static void Question(string message, Action? onConfirm = null, double second = 5) =>
            Show(message, ToastType.Question, onConfirm, second);
    }
}
