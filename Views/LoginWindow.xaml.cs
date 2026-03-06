using System;
using System.Windows;
using SimpleMES.ViewModels;

namespace SimpleMES.Views
{
    public partial class LoginWindow : Window
    {
        public LoginWindow(LoginViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
            viewModel.LoginSucceeded += OnLoginSucceeded;
            viewModel.Notification += OnNotification;
        }

        private void OnNotification(string message)
        {
            MessageBox.Show(message);
        }

        private void OnLoginSucceeded()
        {
            DialogResult = true;
            Close();
        }

        protected override void OnClosed(EventArgs e)
        {
            if (DataContext is LoginViewModel vm)
            {
                vm.LoginSucceeded -= OnLoginSucceeded;
                vm.Notification -= OnNotification;
            }
            base.OnClosed(e);
        }
    }
}