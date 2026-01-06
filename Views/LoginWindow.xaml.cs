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
            }
            base.OnClosed(e);
        }
    }
}