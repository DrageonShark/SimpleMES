using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace SimpleMES.Helpers
{
    public static class PasswordBoxHelper
    {
        // 定义附加属性 Password
        public static readonly DependencyProperty PasswordProperty =
            DependencyProperty.RegisterAttached("Password", typeof(string), 
                typeof(PasswordBoxHelper),  new FrameworkPropertyMetadata(string.Empty, OnPasswordPropertyChanged));

        public static string GetPassword(DependencyObject dp)
        {
            return (string)dp.GetValue(PasswordProperty);
        }
        public static void SetPassword(DependencyObject dp, string value)
        {
            dp.SetValue(PasswordProperty, value);
        }

        //防止更新时的无限递归循环
        private static bool _isUpdating;
        private static void OnPasswordPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            if (sender is PasswordBox passwordBox)
            {
                passwordBox.PasswordChanged -= PasswordChanged;
                if (!_isUpdating) passwordBox.Password = (e.NewValue == null ? string.Empty : e.NewValue.ToString())!;
                passwordBox.PasswordChanged += PasswordChanged;
            }
        }

        public static bool GetAttach(DependencyObject dp)
        {
            return (bool)dp.GetValue(AttachProperty);
        }
        public static void SetAttach(DependencyObject dp, bool value)
        {
            dp.SetValue(AttachProperty, value);
        }
        // 添加 PasswordChanged 事件处理方法
        private static void PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (sender is PasswordBox passwordBox)
            {
                _isUpdating = true;
                SetPassword(passwordBox, passwordBox.Password);
                _isUpdating = false;
            }
        }

        // 定义附加属性 Attach，用于启用功能
        private static readonly DependencyProperty AttachProperty =
            DependencyProperty.RegisterAttached("Attach", typeof(bool),
                typeof(PasswordBoxHelper), new FrameworkPropertyMetadata(false, Attach));
        private static void Attach(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            if (sender is PasswordBox passwordBox)
            {
                if ((bool)e.NewValue) passwordBox.PasswordChanged += PasswordChanged;
                else passwordBox.PasswordChanged -= PasswordChanged;
            }
        }
    }
}
