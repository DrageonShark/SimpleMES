using System.Windows;
using System.Windows.Input;

namespace SimpleMES
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }
        private void DragArea_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                if (WindowState == WindowState.Maximized)
                {
                    SystemCommands.RestoreWindow(this);
                }
                else
                {
                    SystemCommands.MaximizeWindow(this);
                }

                return;
            }

            DragMove();
        }
        private void MinButton_OnClick(object sender, RoutedEventArgs e)
        {
            SystemCommands.MinimizeWindow(this);
        }

        private void MaxButton_OnClick(object sender, RoutedEventArgs e)
        {
            SystemCommands.MaximizeWindow(this);
        }

        private void RestoreButton_OnClick(object sender, RoutedEventArgs e)
        {
            SystemCommands.RestoreWindow(this);
        }

        private void CloseButton_OnClick(object sender, RoutedEventArgs e)
        {
            SystemCommands.CloseWindow(this);
        }
    }
}