using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace MiniFTPClient_WPF
{
    /// <summary>
    /// Interaction logic for LoginForm.xaml
    /// </summary>
    public partial class LoginForm : Window
    {
        private bool _isPasswordVisible = false;
        private bool _isNewPasswordVisible = false;
        private bool _isConfirmPasswordVisible = false;

        public LoginForm()
        {
            InitializeComponent();
        }

        // Chạy 1 lần khi cửa sổ load
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // đường dẫn tương đối tới file video
            myVideo.Source = new Uri("video/login.mp4", UriKind.Relative);

            myVideo.Position = TimeSpan.Zero;
            myVideo.Play();
        }

        // Lặp lại video
        private void myVideo_MediaEnded(object sender, RoutedEventArgs e)
        {
            try
            {
                myVideo.Position = TimeSpan.Zero;  // tua về đầu
                myVideo.Play();                    // phát lại
            }
            catch (Exception ex)
            {
                // Nếu có lỗi gì đó, show ra để biết, không để app tự tắt
                MessageBox.Show(ex.ToString(), "Lỗi trong MediaEnded");
            }
        }

        // 👇 Cập nhật Rect của clip mỗi khi MediaElement đổi size
        private void VideoHost_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (videoClip != null)
            {
                videoClip.Rect = new Rect(0, 0,
                                          VideoHost.ActualWidth,
                                          VideoHost.ActualHeight);
            }
        }

        private void AccountBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            AccountPlaceholder.Visibility = string.IsNullOrWhiteSpace(AccountBox.Text) ? Visibility.Visible : Visibility.Collapsed;
        }

        //Xử lý ẩn hiện mật khẩu
        private void PassPasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            // Khi đang ở chế độ ẩn thì đồng bộ sang ô hiển thị
            if (!_isPasswordVisible)
            {
                PassVisibleBox.Text = PassPasswordBox.Password;
            }

            UpdatePasswordPlaceholder();
        }

        private void PassVisibleBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            // Khi đang ở chế độ HIỆN thì đồng bộ ngược lại sang PasswordBox
            if (_isPasswordVisible)
            {
                PassPasswordBox.Password = PassVisibleBox.Text;
            }

            UpdatePasswordPlaceholder();
        }

        private void PassEyeIcon_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            _isPasswordVisible = !_isPasswordVisible;

            if (_isPasswordVisible)
            {
                // Bật chế độ HIỆN
                PassVisibleBox.Text = PassPasswordBox.Password;
                PassVisibleBox.Visibility = Visibility.Visible;
                PassPasswordBox.Visibility = Visibility.Collapsed;

                PassEyeIcon.Source = new BitmapImage(
                    new Uri("pack://application:,,,/ảnh/eyeopen.png"));
            }
            else
            {
                // Bật chế độ ẨN
                PassPasswordBox.Password = PassVisibleBox.Text;
                PassVisibleBox.Visibility = Visibility.Collapsed;
                PassPasswordBox.Visibility = Visibility.Visible;

                PassEyeIcon.Source = new BitmapImage(
                    new Uri("pack://application:,,,/ảnh/eyeclose.png"));
            }

            UpdatePasswordPlaceholder();
        }

        private void UpdatePasswordPlaceholder()
        {
            bool empty = string.IsNullOrEmpty(PassPasswordBox.Password)
                         && string.IsNullOrEmpty(PassVisibleBox.Text);

            PassPlaceholder.Visibility = empty ? Visibility.Visible : Visibility.Collapsed;
        }

        private void ForgotPassword_Click(object sender, MouseButtonEventArgs e)
        {
            LoginFormBorder.Visibility = Visibility.Collapsed;
            ForgotFormBorder.Visibility = Visibility.Visible;
        }

        private void BackToLogin_Click(object sender, MouseButtonEventArgs e)
        {
            // Ẩn cả 2 form phụ
            ForgotFormBorder.Visibility = Visibility.Collapsed;
            ChangePasswordBorder.Visibility = Visibility.Collapsed;

            // Hiện lại form đăng nhập
            LoginFormBorder.Visibility = Visibility.Visible;
        }

        private void SendCode_Click(object sender, RoutedEventArgs e)
        {
            // TODO: ở đây sau này bạn có thể gửi mã thật qua email

            // Ẩn form quên mật khẩu, hiện form đổi mật khẩu
            ForgotFormBorder.Visibility = Visibility.Collapsed;
            ChangePasswordBorder.Visibility = Visibility.Visible;
        }

        //Hàm xử lí Placeholder
        private void EmailBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            EmailPlaceholder.Visibility = string.IsNullOrWhiteSpace(EmailBox.Text) ? Visibility.Visible : Visibility.Collapsed;
        }

        //Hàm xử lí Placeholder
        private void CodeBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            CodePlaceholder.Visibility = string.IsNullOrWhiteSpace(CodeBox.Text) ? Visibility.Visible : Visibility.Collapsed;
        }

        //Xử lý ân hiện mật khẩu ô mật khẩu mới

        private void NewPassPasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (!_isNewPasswordVisible)
            {
                NewPassVisibleBox.Text = NewPassPasswordBox.Password;
            }

            UpdateNewPasswordPlaceholder();
        }

        private void NewPassVisibleBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isNewPasswordVisible)
            {
                NewPassPasswordBox.Password = NewPassVisibleBox.Text;
            }

            UpdateNewPasswordPlaceholder();
        }

        private void NewPassEyeIcon_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            _isNewPasswordVisible = !_isNewPasswordVisible;

            if (_isNewPasswordVisible)
            {
                NewPassVisibleBox.Text = NewPassPasswordBox.Password;
                NewPassVisibleBox.Visibility = Visibility.Visible;
                NewPassPasswordBox.Visibility = Visibility.Collapsed;

                NewPassEyeIcon.Source = new BitmapImage(
                    new Uri("pack://application:,,,/ảnh/eyeopen.png"));
            }
            else
            {
                NewPassPasswordBox.Password = NewPassVisibleBox.Text;
                NewPassVisibleBox.Visibility = Visibility.Collapsed;
                NewPassPasswordBox.Visibility = Visibility.Visible;

                NewPassEyeIcon.Source = new BitmapImage(
                    new Uri("pack://application:,,,/ảnh/eyeclose.png"));
            }

            UpdateNewPasswordPlaceholder();
        }

        private void UpdateNewPasswordPlaceholder()
        {
            bool empty = string.IsNullOrEmpty(NewPassPasswordBox.Password)
                         && string.IsNullOrEmpty(NewPassVisibleBox.Text);

            NewPassPlaceholder.Visibility = empty ? Visibility.Visible : Visibility.Collapsed;
        }

        //xử lý ẩn hiện mật khẩu ô xác nhận mật khẩu
        private void ConfirmPassPasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (!_isConfirmPasswordVisible)
            {
                ConfirmPassVisibleBox.Text = ConfirmPassPasswordBox.Password;
            }

            UpdateConfirmPasswordPlaceholder();
        }

        private void ConfirmPassVisibleBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isConfirmPasswordVisible)
            {
                ConfirmPassPasswordBox.Password = ConfirmPassVisibleBox.Text;
            }

            UpdateConfirmPasswordPlaceholder();
        }

        private void ConfirmPassEyeIcon_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            _isConfirmPasswordVisible = !_isConfirmPasswordVisible;

            if (_isConfirmPasswordVisible)
            {
                ConfirmPassVisibleBox.Text = ConfirmPassPasswordBox.Password;
                ConfirmPassVisibleBox.Visibility = Visibility.Visible;
                ConfirmPassPasswordBox.Visibility = Visibility.Collapsed;

                ConfirmPassEyeIcon.Source = new BitmapImage(
                    new Uri("pack://application:,,,/ảnh/eyeopen.png"));
            }
            else
            {
                ConfirmPassPasswordBox.Password = ConfirmPassVisibleBox.Text;
                ConfirmPassVisibleBox.Visibility = Visibility.Collapsed;
                ConfirmPassPasswordBox.Visibility = Visibility.Visible;

                ConfirmPassEyeIcon.Source = new BitmapImage(
                    new Uri("pack://application:,,,/ảnh/eyeclose.png"));
            }

            UpdateConfirmPasswordPlaceholder();
        }

        private void UpdateConfirmPasswordPlaceholder()
        {
            bool empty = string.IsNullOrEmpty(ConfirmPassPasswordBox.Password)
                         && string.IsNullOrEmpty(ConfirmPassVisibleBox.Text);

            ConfirmPassPlaceholder.Visibility = empty ? Visibility.Visible : Visibility.Collapsed;
        }

    }
}
