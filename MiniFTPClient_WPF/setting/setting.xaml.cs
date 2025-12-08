using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace MiniFTPClient_WPF.setting
{
    /// <summary>
    /// Interaction logic for setting.xaml
    /// </summary>
    public partial class Setting : Page
    {

        private bool _isConfirmPwdVisible = false;

        public Setting()
        {
            InitializeComponent();
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {

        }

        private void ConfirmPwd_ToggleEye(object sender, MouseButtonEventArgs e)
        {
            _isConfirmPwdVisible = !_isConfirmPwdVisible;

            if (_isConfirmPwdVisible)
            {
                ConfirmPwdVisibleBox.Text = ConfirmPwdPasswordBox.Password;
                ConfirmPwdVisibleBox.Visibility = Visibility.Visible;
                ConfirmPwdPasswordBox.Visibility = Visibility.Collapsed;

                ConfirmPwdEyeIcon.Source = new BitmapImage(
                    new Uri("/anh/eyeopen.png"));
            }
            else
            {
                ConfirmPwdPasswordBox.Password = ConfirmPwdVisibleBox.Text;
                ConfirmPwdVisibleBox.Visibility = Visibility.Collapsed;
                ConfirmPwdPasswordBox.Visibility = Visibility.Visible;

                ConfirmPwdEyeIcon.Source = new BitmapImage(
                    new Uri("/anh/eyeclose.png"));
            }

            UpdateConfirmPwdPlaceholder();
        }

        private void UpdateConfirmPwdPlaceholder()
        {
            bool isEmpty =
                string.IsNullOrEmpty(ConfirmPwdPasswordBox.Password) &&
                string.IsNullOrEmpty(ConfirmPwdVisibleBox.Text);

            ConfirmPwdPlaceholder.Visibility =
                isEmpty ? Visibility.Visible : Visibility.Collapsed;
        }

        private void ConfirmPwdPasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (!_isConfirmPwdVisible)
                ConfirmPwdVisibleBox.Text = ConfirmPwdPasswordBox.Password;

            UpdateConfirmPwdPlaceholder();
        }

        private void ConfirmPwdVisibleBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isConfirmPwdVisible)
                ConfirmPwdPasswordBox.Password = ConfirmPwdVisibleBox.Text;

            UpdateConfirmPwdPlaceholder();
        }


    }
}
