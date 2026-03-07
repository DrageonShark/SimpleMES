using MaterialDesignThemes.Wpf;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace SimpleMES.Views
{
    /// <summary>
    /// ToastWindow.xaml 的交互逻辑
    /// </summary>
    public partial class ToastWindow : Window
    {
        public enum ToastType { Success, Error, Info }
        private ToastWindow(string message, ToastType type, double durationSeconds)
        {
            InitializeComponent();
            MessageText.Text = message;
            // 设置不同类型的样式
            SetToastStyle(type);
            // 定位到主窗口居中偏上位置
            PositionToast();
            // 加载后执行动画
            Loaded += (_, _) => StartToastAnimation(durationSeconds);
            //switch (type)
            //{
            //    case ToastType.Success:
            //        RootBorder.Background = new SolidColorBrush(Color.FromRgb(46, 125, 50));
            //        ProgressBar.Foreground = new SolidColorBrush(Color.FromRgb(165, 214, 167));
            //        IconText.Text = "✅";
            //        break;
            //    case ToastType.Error:
            //        RootBorder.Background = new SolidColorBrush(Color.FromRgb(183, 28, 28));
            //        ProgressBar.Foreground = new SolidColorBrush(Color.FromRgb(239, 154, 154));
            //        IconText.Text = "❌";
            //        break;
            //    case ToastType.Info:
            //    default:
            //        RootBorder.Background = new SolidColorBrush(Color.FromRgb(13, 71, 161));
            //        ProgressBar.Foreground = new SolidColorBrush(Color.FromRgb(144, 202, 209));
            //        IconText.Text = "ℹ️";
            //        break;
            //}

            //var owner = Application.Current.MainWindow;
            //if (owner != null)
            //{
            //    Left = owner.Left + (owner.Width - Width) / 2;
            //    Top = owner.Top + 50;
            //}

            //Loaded += (_, _) =>
            //{
            //    // // 创建动画：进度条从100→0，耗时durationSeconds秒
            //    var anim = new DoubleAnimation(100, 0, TimeSpan.FromSeconds(durationSeconds));
            //    // 动画结束后，关闭提示框
            //    anim.Completed += (sender, args) => Close();
            //    // 把动画绑定到进度条的Value属性，开始播放
            //    ProgressBar.BeginAnimation(System.Windows.Controls.Primitives.RangeBase.ValueProperty, anim);
            //};
        }
        /// <summary>
        /// 设置不同类型Toast的样式（颜色+图标）
        /// </summary>
        private void SetToastStyle(ToastType type)
        {
            switch (type)
            {
                case ToastType.Success:
                    RootBorder.Background = new SolidColorBrush(Color.FromRgb(40, 167, 69)); // 更现代的成功绿
                    StatusIcon.Kind = PackIconKind.CheckCircle;
                    break;
                case ToastType.Error:
                    RootBorder.Background = new SolidColorBrush(Color.FromRgb(220, 53, 69)); // 标准错误红
                    StatusIcon.Kind = PackIconKind.AlertCircle;  // 修复：ErrorCircle -> AlertCircle
                    break;
                case ToastType.Info:
                    RootBorder.Background = new SolidColorBrush(Color.FromRgb(0, 123, 255)); // 信息蓝
                    StatusIcon.Kind = PackIconKind.InfoCircle;
                    break;
            }
        }

        /// <summary>
        /// 定位Toast到主窗口居中偏上位置
        /// </summary>
        private void PositionToast()
        {
            var owner = Application.Current.MainWindow;
            if (owner != null)
            {
                Left = owner.Left + (owner.ActualWidth - Width) / 2;
                Top = owner.Top + 60; // 距离顶部60px，更符合视觉习惯
            }
        }

        /// <summary>
        /// 执行Toast动画（淡入→停留→淡出）
        /// </summary>
        private void StartToastAnimation(double durationSeconds)
        {
            // 初始透明度0
            Opacity = 0;

            // 淡入动画：0→1，耗时0.3秒
            var fadeIn = new DoubleAnimation(0, 1, TimeSpan.FromSeconds(0.3));

            // 淡出动画：1→0，耗时0.5秒，延迟durationSeconds秒执行
            var fadeOut = new DoubleAnimation(1, 0, TimeSpan.FromSeconds(0.5))
            {
                BeginTime = TimeSpan.FromSeconds(durationSeconds)
            };

            // 动画完成后关闭窗口
            fadeOut.Completed += (_, _) => Close();

            // 组合动画并播放
            var storyboard = new Storyboard();
            storyboard.Children.Add(fadeIn);
            storyboard.Children.Add(fadeOut);
            Storyboard.SetTarget(storyboard, this);
            Storyboard.SetTargetProperty(storyboard, new PropertyPath(OpacityProperty));
            storyboard.Begin();
        }
        public static void Success(string message, double second = 3) =>
            new ToastWindow(message, ToastType.Success, second).Show();
        public static void Error(string message, double second = 3) =>
            new ToastWindow(message, ToastType.Error, second).Show();
        public static void Info(string message, double second = 3) =>
            new ToastWindow(message, ToastType.Info, second).Show();
    }
}
