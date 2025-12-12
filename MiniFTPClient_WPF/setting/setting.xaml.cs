using MiniFTPClient_WPF.Services;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace MiniFTPClient_WPF.setting
{
    public partial class Setting : Page
    {
        private bool _isConfirmPwdVisible = false;
        private bool _isCurPwdVisible = false;
        private bool _isNewPwdVisible = false;

        public Setting()
        {
            InitializeComponent();
            LoadUserInfo();
        }

        // ==================== TẢI THÔNG TIN USER ====================
        private void LoadUserInfo()
        {
            try
            {
                // Lấy thông tin từ FtpClientService
                if (FtpClientService.Instance.IsConnected)
                {
                    // Hiển thị tên đầy đủ
                    string fullName = FtpClientService.Instance.CurrentFullName;
                    if (!string.IsNullOrWhiteSpace(fullName))
                    {
                        TxtDisplayName.Text = fullName;
                        TxtFullName.Text = fullName;
                    }

                    // Hiển thị username
                    string username = FtpClientService.Instance.CurrentUsername;
                    if (!string.IsNullOrWhiteSpace(username))
                    {
                        TxtUsername.Text = username;
                    }

                    // Email có thể để trống hoặc lấy từ DB nếu có
                    // TxtEmail.Text = "..."; // Nếu có trong Service
                    string email = FtpClientService.Instance.CurrentEmail;
                    TxtEmailCT.Text = email ;
                    string description = FtpClientService.Instance.CurrentDescription;
                    TxtDescription.Text = description ;
                }
                else
                {
                    MessageBox.Show("Chưa đăng nhập!", "Cảnh báo",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi tải thông tin: {ex.Message}", "Lỗi",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ==================== LƯU THÔNG TIN CÁ NHÂN ====================
        private void BtnSaveProfile_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string username = TxtUsername.Text.Trim();
                string email = TxtEmail.Text.Trim();
                string fullName = TxtFullName.Text.Trim();

                // Validation
                if (string.IsNullOrWhiteSpace(username))
                {
                    MessageBox.Show("Tên tài khoản không được để trống!", "Lỗi",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    TxtUsername.Focus();
                    return;
                }

                if (string.IsNullOrWhiteSpace(fullName))
                {
                    MessageBox.Show("Họ và tên không được để trống!", "Lỗi",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    TxtFullName.Focus();
                    return;
                }

                // Kiểm tra email hợp lệ (nếu nhập)
                if (!string.IsNullOrWhiteSpace(email) && !IsValidEmail(email))
                {
                    MessageBox.Show("Email không hợp lệ!", "Lỗi",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    TxtEmail.Focus();
                    return;
                }

                // TODO: Gửi lệnh UPDATE_PROFILE lên server
                //await FtpClientService.Instance.UpdateProfileAsync(username, email, fullName);

                MessageBox.Show("Chức năng cập nhật thông tin đang được phát triển!",
                    "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi lưu thông tin: {ex.Message}", "Lỗi",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ==================== ĐỔI MẬT KHẨU ====================
        private async void BtnChangePassword_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string currentPwd = _isCurPwdVisible
                    ? CurPwdVisibleBox.Text
                    : CurPwdPasswordBox.Password;

                string newPwd = _isNewPwdVisible
                    ? NewPwdVisibleBox.Text
                    : NewPwdPasswordBox.Password;

                string confirmPwd = _isConfirmPwdVisible
                    ? ConfirmPwdVisibleBox.Text
                    : ConfirmPwdPasswordBox.Password;

                // Validation
                if (string.IsNullOrWhiteSpace(currentPwd))
                {
                    MessageBox.Show("Vui lòng nhập mật khẩu hiện tại!", "Lỗi",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    CurPwdPasswordBox.Focus();
                    return;
                }

                if (string.IsNullOrWhiteSpace(newPwd))
                {
                    MessageBox.Show("Vui lòng nhập mật khẩu mới!", "Lỗi",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    NewPwdPasswordBox.Focus();
                    return;
                }

                if (newPwd.Length < 6)
                {
                    MessageBox.Show("Mật khẩu mới phải có ít nhất 6 ký tự!", "Lỗi",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    NewPwdPasswordBox.Focus();
                    return;
                }

                if (newPwd != confirmPwd)
                {
                    MessageBox.Show("Mật khẩu xác nhận không khớp!", "Lỗi",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    ConfirmPwdPasswordBox.Focus();
                    return;
                }

                if (currentPwd == newPwd)
                {
                    MessageBox.Show("Mật khẩu mới không được trùng với mật khẩu cũ!", "Lỗi",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    NewPwdPasswordBox.Focus();
                    return;
                }

                // Disable nút để tránh spam
                var btn = sender as Button;
                if (btn != null)
                {
                    btn.IsEnabled = false;
                    btn.Content = "Đang xử lý...";
                }

                try
                {
                    // TODO: Gửi lệnh CHANGE_PASSWORD lên server
                    string result = await FtpClientService.Instance.ChangePasswordAsync(currentPwd, newPwd);

                    // Giả lập thành công (xóa dòng này khi có API thật)
                    await System.Threading.Tasks.Task.Delay(500);

                    //MessageBox.Show("Chức năng đổi mật khẩu đang được phát triển!\n\n" +
                    //    "Khi hoàn thiện, bạn sẽ cần đăng nhập lại sau khi đổi mật khẩu.",
                    //    "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);

                    // Xóa các ô nhập sau khi thành công
                    ClearPasswordFields();
                }
                finally
                {
                    if (btn != null)
                    {
                        btn.IsEnabled = true;
                        btn.Content = "Cập nhật";
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi đổi mật khẩu: {ex.Message}", "Lỗi",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ==================== HELPER FUNCTIONS ====================
        private void ClearPasswordFields()
        {
            CurPwdPasswordBox.Clear();
            CurPwdVisibleBox.Clear();
            NewPwdPasswordBox.Clear();
            NewPwdVisibleBox.Clear();
            ConfirmPwdPasswordBox.Clear();
            ConfirmPwdVisibleBox.Clear();
        }

        private bool IsValidEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }

        // ==================== ẨN/HIỆN MẬT KHẨU HIỆN TẠI ====================
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
            bool isEmpty = string.IsNullOrEmpty(CurPwdPasswordBox.Password) &&
                          string.IsNullOrEmpty(CurPwdVisibleBox.Text);

            CurPwdPlaceholder.Visibility = isEmpty ? Visibility.Visible : Visibility.Collapsed;
        }

        // ==================== ẨN/HIỆN MẬT KHẨU MỚI ====================
        private void NewPwd_ToggleEye(object sender, MouseButtonEventArgs e)
        {
            _isNewPwdVisible = !_isNewPwdVisible;

            if (_isNewPwdVisible)
            {
                NewPwdVisibleBox.Text = NewPwdPasswordBox.Password;
                NewPwdVisibleBox.Visibility = Visibility.Visible;
                NewPwdPasswordBox.Visibility = Visibility.Collapsed;
                NewPwdEyeIcon.Source = new BitmapImage(
                    new Uri("pack://application:,,,/MiniFTPClient_WPF;component/anh/eyeopen.png"));
            }
            else
            {
                NewPwdPasswordBox.Password = NewPwdVisibleBox.Text;
                NewPwdVisibleBox.Visibility = Visibility.Collapsed;
                NewPwdPasswordBox.Visibility = Visibility.Visible;
                NewPwdEyeIcon.Source = new BitmapImage(
                    new Uri("pack://application:,,,/MiniFTPClient_WPF;component/anh/eyeclose.png"));
            }

            UpdateNewPwdPlaceholder();
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

        private void UpdateNewPwdPlaceholder()
        {
            bool isEmpty = string.IsNullOrEmpty(NewPwdPasswordBox.Password) &&
                          string.IsNullOrEmpty(NewPwdVisibleBox.Text);

            NewPwdPlaceholder.Visibility = isEmpty ? Visibility.Visible : Visibility.Collapsed;
        }

        // ==================== ẨN/HIỆN XÁC NHẬN MẬT KHẨU ====================
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

        private void UpdateConfirmPwdPlaceholder()
        {
            bool isEmpty = string.IsNullOrEmpty(ConfirmPwdPasswordBox.Password) &&
                          string.IsNullOrEmpty(ConfirmPwdVisibleBox.Text);

            ConfirmPwdPlaceholder.Visibility = isEmpty ? Visibility.Visible : Visibility.Collapsed;
        }

        private void BtnSaveProfile_Click_1(object sender, RoutedEventArgs e)
        {

        }

    }
}