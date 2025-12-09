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
        public Setting()
        {
            InitializeComponent();
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {

        }

        //hàm xử lý ẩn hiện mật khẩu ô xác nhận mật khẩu
        private bool _isConfirmPwdVisible = false;

        private void ConfirmPwd_ToggleEye(object sender, MouseButtonEventArgs e)
        {
            _isConfirmPwdVisible = !_isConfirmPwdVisible;

            if (_isConfirmPwdVisible)
            {
                ConfirmPwdVisibleBox.Text = ConfirmPwdPasswordBox.Password;
                ConfirmPwdVisibleBox.Visibility = Visibility.Visible;
                ConfirmPwdPasswordBox.Visibility = Visibility.Collapsed;

                ConfirmPwdEyeIcon.Source = new BitmapImage(
                    new Uri("pack://application:,,,/MiniFTPClient_WPF;component/anh/eyeopen.png"));
            }
            else
            {
                ConfirmPwdPasswordBox.Password = ConfirmPwdVisibleBox.Text;
                ConfirmPwdVisibleBox.Visibility = Visibility.Collapsed;
                ConfirmPwdPasswordBox.Visibility = Visibility.Visible;

                ConfirmPwdEyeIcon.Source = new BitmapImage(
                    new Uri("pack://application:,,,/MiniFTPClient_WPF;component/anh/eyeclose.png"));
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

        //hàm xử lý ẩn hiệu mật khẩu hiện tại
        private bool _isCurPwdVisible = false;
        private void CurPwd_ToggleEye(object sender, MouseButtonEventArgs e)
        {
            _isCurPwdVisible = !_isCurPwdVisible;

            if (_isCurPwdVisible)
            {
                CurPwdVisibleBox.Text = CurPwdPasswordBox.Password;
                CurPwdVisibleBox.Visibility = Visibility.Visible;
                CurPwdPasswordBox.Visibility = Visibility.Collapsed;

                CurPwdEyeIcon.Source = new BitmapImage(
                    new Uri("pack://application:,,,/anh/eyeopen.png"));
            }
            else
            {
                CurPwdPasswordBox.Password = CurPwdVisibleBox.Text;
                CurPwdVisibleBox.Visibility = Visibility.Collapsed;
                CurPwdPasswordBox.Visibility = Visibility.Visible;

                CurPwdEyeIcon.Source = new BitmapImage(
                    new Uri("pack://application:,,,/anh/eyeclose.png"));
            }

            UpdateCurPwdPlaceholder();
        }

        private void CurPwdPasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (!_isCurPwdVisible)
                CurPwdVisibleBox.Text = CurPwdPasswordBox.Password;

            UpdateCurPwdPlaceholder();
        }

        private void CurPwdVisibleBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isCurPwdVisible)
                CurPwdPasswordBox.Password = CurPwdVisibleBox.Text;

            UpdateCurPwdPlaceholder();
        }

        private void UpdateCurPwdPlaceholder()
        {
            bool isEmpty =
                string.IsNullOrEmpty(CurPwdPasswordBox.Password) &&
                string.IsNullOrEmpty(CurPwdVisibleBox.Text);

            CurPwdPlaceholder.Visibility =
                isEmpty ? Visibility.Visible : Visibility.Collapsed;
        }

        // biến trạng thái: đang hiển thị rõ hay không
        private bool _isNewPwdVisible = false;

        private void NewPwd_ToggleEye(object sender, MouseButtonEventArgs e)
        {
            _isNewPwdVisible = !_isNewPwdVisible;

            if (_isNewPwdVisible)
            {
                // Hiện mật khẩu: dùng TextBox
                NewPwdVisibleBox.Text = NewPwdPasswordBox.Password;
                NewPwdVisibleBox.Visibility = Visibility.Visible;
                NewPwdPasswordBox.Visibility = Visibility.Collapsed;

                NewPwdEyeIcon.Source = new BitmapImage(
                    new Uri("pack://application:,,,/MiniFTPClient_WPF;component/anh/eyeopen.png"));
            }
            else
            {
                // Ẩn mật khẩu: dùng PasswordBox
                NewPwdPasswordBox.Password = NewPwdVisibleBox.Text;
                NewPwdVisibleBox.Visibility = Visibility.Collapsed;
                NewPwdPasswordBox.Visibility = Visibility.Visible;

                NewPwdEyeIcon.Source = new BitmapImage(
                    new Uri("pack://application:,,,/MiniFTPClient_WPF;component/anh/eyeclose.png"));
            }

            UpdateNewPwdPlaceholder();
        }

        private void UpdateNewPwdPlaceholder()
        {
            bool isEmpty =
                string.IsNullOrEmpty(NewPwdPasswordBox.Password) &&
                string.IsNullOrEmpty(NewPwdVisibleBox.Text);

            NewPwdPlaceholder.Visibility =
                isEmpty ? Visibility.Visible : Visibility.Collapsed;
        }

        private void NewPwdPasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (!_isNewPwdVisible)
                NewPwdVisibleBox.Text = NewPwdPasswordBox.Password;

            UpdateNewPwdPlaceholder();
        }

        private void NewPwdVisibleBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isNewPwdVisible)
                NewPwdPasswordBox.Password = NewPwdVisibleBox.Text;

            UpdateNewPwdPlaceholder();
        }

    }
}
