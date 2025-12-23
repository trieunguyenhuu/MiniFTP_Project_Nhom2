using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;

// Thêm thư viện Service để gọi Login
using MiniFTPClient_WPF.Services;

namespace MiniFTPClient_WPF
{
    public partial class LoginForm : Window
    {
        private bool _isPasswordVisible = false;
        private bool _isNewPasswordVisible = false;
        private bool _isConfirmPasswordVisible = false;

        public LoginForm()
        {
            InitializeComponent();
        }

        // --- SỰ KIỆN CLICK NÚT ĐĂNG NHẬP (QUAN TRỌNG NHẤT) ---
        private async void BtnLogin_Click(object sender, RoutedEventArgs e)
        {
            string u = AccountBox.Text;

            // Lấy mật khẩu: Nếu đang hiện password box thì lấy Password, nếu đang hiện text box thì lấy Text
            string p = PassPasswordBox.Visibility == Visibility.Visible
                       ? PassPasswordBox.Password
                       : PassVisibleBox.Text;

            if (string.IsNullOrWhiteSpace(u) || string.IsNullOrWhiteSpace(p))
            {
                MessageBox.Show("Vui lòng nhập tài khoản và mật khẩu!");
                return;
            }

            // Gọi Service để đăng nhập
            string result = await FtpClientService.Instance.LoginAsync(u, p);

            if (result == "OK")
            {
                // Thành công -> Mở MainWindow
                MainWindow main = new MainWindow();
                main.Show();
                this.Close();
            }
            else
            {
                // Thất bại
                MessageBox.Show(result, "Đăng nhập thất bại", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // --- CÁC HÀM XỬ LÝ VIDEO & GIAO DIỆN CŨ CỦA BẠN (GIỮ NGUYÊN) ---

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            myVideo.Source = new Uri("video/login.mp4", UriKind.Relative);
            myVideo.Position = TimeSpan.Zero;
            myVideo.Play();
        }

        private void myVideo_MediaEnded(object sender, RoutedEventArgs e)
        {
            try
            {
                myVideo.Position = TimeSpan.Zero;
                myVideo.Play();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "Lỗi trong MediaEnded");
            }
        }

        private void VideoHost_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (videoClip != null)
            {
                videoClip.Rect = new Rect(0, 0, VideoHost.ActualWidth, VideoHost.ActualHeight);
            }
        }

        private void AccountBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            AccountPlaceholder.Visibility = string.IsNullOrWhiteSpace(AccountBox.Text) ? Visibility.Visible : Visibility.Collapsed;
        }

        private void PassPasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (!_isPasswordVisible)
            {
                PassVisibleBox.Text = PassPasswordBox.Password;
            }
            UpdatePasswordPlaceholder();
        }

        private void PassVisibleBox_TextChanged(object sender, TextChangedEventArgs e)
        {
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
                PassVisibleBox.Text = PassPasswordBox.Password;
                PassVisibleBox.Visibility = Visibility.Visible;
                PassPasswordBox.Visibility = Visibility.Collapsed;
                PassEyeIcon.Source = new BitmapImage(new Uri("pack://application:,,,/anh/eyeopen.png"));
            }
            else
            {
                PassPasswordBox.Password = PassVisibleBox.Text;
                PassVisibleBox.Visibility = Visibility.Collapsed;
                PassPasswordBox.Visibility = Visibility.Visible;
                PassEyeIcon.Source = new BitmapImage(new Uri("pack://application:,,,/anh/eyeclose.png"));
            }
            UpdatePasswordPlaceholder();
        }

        private void UpdatePasswordPlaceholder()
        {
            bool empty = string.IsNullOrEmpty(PassPasswordBox.Password) && string.IsNullOrEmpty(PassVisibleBox.Text);
            PassPlaceholder.Visibility = empty ? Visibility.Visible : Visibility.Collapsed;
        }

        private void ForgotPassword_Click(object sender, MouseButtonEventArgs e)
        {
            LoginFormBorder.Visibility = Visibility.Collapsed;
            ForgotFormBorder.Visibility = Visibility.Visible;
        }

        private void BackToLogin_Click(object sender, MouseButtonEventArgs e)
        {
            ForgotFormBorder.Visibility = Visibility.Collapsed;
            ChangePasswordBorder.Visibility = Visibility.Collapsed;
            LoginFormBorder.Visibility = Visibility.Visible;
        }

        private void SendCode_Click(object sender, RoutedEventArgs e)
        {
            ForgotFormBorder.Visibility = Visibility.Collapsed;
            ChangePasswordBorder.Visibility = Visibility.Visible;
        }

        private void EmailBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            EmailPlaceholder.Visibility = string.IsNullOrWhiteSpace(EmailBox.Text) ? Visibility.Visible : Visibility.Collapsed;
        }

        private void CodeBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            CodePlaceholder.Visibility = string.IsNullOrWhiteSpace(CodeBox.Text) ? Visibility.Visible : Visibility.Collapsed;
        }

        private void NewPassPasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (!_isNewPasswordVisible) NewPassVisibleBox.Text = NewPassPasswordBox.Password;
            UpdateNewPasswordPlaceholder();
        }

        private void NewPassVisibleBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isNewPasswordVisible) NewPassPasswordBox.Password = NewPassVisibleBox.Text;
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
                NewPassEyeIcon.Source = new BitmapImage(new Uri("pack://application:,,,/anh/eyeopen.png"));
            }
            else
            {
                NewPassPasswordBox.Password = NewPassVisibleBox.Text;
                NewPassVisibleBox.Visibility = Visibility.Collapsed;
                NewPassPasswordBox.Visibility = Visibility.Visible;
                NewPassEyeIcon.Source = new BitmapImage(new Uri("pack://application:,,,/anh/eyeclose.png"));
            }
            UpdateNewPasswordPlaceholder();
        }

        private void UpdateNewPasswordPlaceholder()
        {
            bool empty = string.IsNullOrEmpty(NewPassPasswordBox.Password) && string.IsNullOrEmpty(NewPassVisibleBox.Text);
            NewPassPlaceholder.Visibility = empty ? Visibility.Visible : Visibility.Collapsed;
        }

        private void ConfirmPassPasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (!_isConfirmPasswordVisible) ConfirmPassVisibleBox.Text = ConfirmPassPasswordBox.Password;
            UpdateConfirmPasswordPlaceholder();
        }

        private void ConfirmPassVisibleBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isConfirmPasswordVisible) ConfirmPassPasswordBox.Password = ConfirmPassVisibleBox.Text;
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
                ConfirmPassEyeIcon.Source = new BitmapImage(new Uri("pack://application:,,,/anh/eyeopen.png"));
            }
            else
            {
                ConfirmPassPasswordBox.Password = ConfirmPassVisibleBox.Text;
                ConfirmPassVisibleBox.Visibility = Visibility.Collapsed;
                ConfirmPassPasswordBox.Visibility = Visibility.Visible;
                ConfirmPassEyeIcon.Source = new BitmapImage(new Uri("pack://application:,,,/anh/eyeclose.png"));
            }
            UpdateConfirmPasswordPlaceholder();
        }

        private void UpdateConfirmPasswordPlaceholder()
        {
            bool empty = string.IsNullOrEmpty(ConfirmPassPasswordBox.Password) && string.IsNullOrEmpty(ConfirmPassVisibleBox.Text);
            ConfirmPassPlaceholder.Visibility = empty ? Visibility.Visible : Visibility.Collapsed;
        }

    }
}