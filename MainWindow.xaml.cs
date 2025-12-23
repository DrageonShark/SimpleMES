using System.Windows;

namespace SimpleMES
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private bool _isMenuCollapsed = false;
        public MainWindow()
        {
            InitializeComponent();
           
        }

        private void ToggleMenu(object sender, RoutedEventArgs e)
        {
            _isMenuCollapsed = !_isMenuCollapsed;
            LeftColumn.Width = _isMenuCollapsed ? new GridLength(30) : new GridLength(200);
            if (ToggleMenuButton != null)
            {
                ToggleMenuButton.Content = _isMenuCollapsed ? "⏵" : "⏴";
            }
        }
    }
}