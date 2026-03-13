using SimpleMES.ViewModels;
using System.Windows;

namespace SimpleMES.Views
{
    /// <summary>
    /// code-behind 仅负责初始化和注入 DataContext，不写业务/动画/事件逻辑
    /// </summary>
    public partial class ToastWindow : Window
    {
        public ToastWindow()
        {
            InitializeComponent();
        }

        public ToastWindow(ToastWindowViewModel vm) : this()
        {
            DataContext = vm;
        }
    }
}
