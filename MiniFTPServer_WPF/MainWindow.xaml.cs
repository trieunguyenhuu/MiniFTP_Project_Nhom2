using MiniFtpServer_WPF.Services;
using MahApps.Metro.IconPacks;
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

namespace MiniFTPServer_WPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private FtpServer _server;

        private System.Windows.Threading.DispatcherTimer _timer;
        private DateTime _startTime;
        private bool _isNewPasswordVisible = false;
        private bool _isConfirmPasswordVisible = false;

        public MainWindow()
        {
            InitializeComponent();
            // Khởi tạo
            var db = new DatabaseService();
            _server = new FtpServer(db, Log, UpdateConnectionCount);

            _timer = new System.Windows.Threading.DispatcherTimer();
            _timer.Interval = TimeSpan.FromSeconds(1); // Nhảy mỗi 1 giây
            _timer.Tick += Timer_Tick; // Gọi hàm Timer_Tick
        }

        private void Log(string msg)
        {
            Dispatcher.Invoke(() =>
            {
                logHoatDong.AppendText($"[{System.DateTime.Now:HH:mm:ss}] {msg}\n");
                logHoatDong.ScrollToEnd();
            });
        }

        private void btnStart_Click(object sender, RoutedEventArgs e)
        {
            iconServerStatus.Kind = PackIconMaterialKind.Wifi;
            borderServerIcon.Background = new SolidColorBrush(Color.FromRgb(0xDD, 0xFB, 0xEC)); // #DDFBEC
            iconServerStatus.Foreground = Brushes.DarkGreen;
            var greenBrush = (Brush)FindResource("AccentGreen");
            ellipseServerStatus.Fill = greenBrush;
            txtServerStatus.Text = "Online";
            txtServerStatus.Foreground = greenBrush;
            _server.Start();
            btnStart.IsEnabled = false;
            btnStop.IsEnabled = true;
            lblStatus.Text = "Online";
            lblStatus.Foreground = greenBrush;

            _startTime = DateTime.Now; // Mốc thời gian bắt đầu
            _timer.Start();            // BẮT ĐẦU CHẠY ĐỒNG HỒ
        }

        private void btnStop_Click(object sender, RoutedEventArgs e)
        {
            iconServerStatus.Kind = PackIconMaterialKind.WifiOff;
            borderServerIcon.Background = new SolidColorBrush(Color.FromRgb(0xFF, 0xEC, 0xEF)); // #FFECEF
            iconServerStatus.Foreground = new SolidColorBrush(Color.FromRgb(0xFF, 0x6B, 0x81)); // #FF6B81
            var redBrush = (Brush)FindResource("DangerRed");
            ellipseServerStatus.Fill = redBrush;
            txtServerStatus.Text = "Offline";
            txtServerStatus.Foreground = redBrush;
            _server.Stop();
            _timer.Stop();
            txtUptime.Text = "00:00:00";
            btnStart.IsEnabled = true;
            btnStop.IsEnabled = false;
            lblStatus.Text = "Offline";
            lblStatus.Foreground = redBrush;
        }

        // Hàm này sẽ được FtpServer gọi mỗi khi có người kết nối/ngắt kết nối
        private void UpdateConnectionCount(int count)
        {
            // Dùng Dispatcher để cập nhật giao diện từ luồng khác an toàn
            Dispatcher.Invoke(() =>
            {
                // Đảm bảo bạn đã đặt tên x:Name="txtConnectionCount" trong file XAML nhé
                if (txtConnectionCount != null)
                {
                    txtConnectionCount.Text = count.ToString();
                }
            });
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            TimeSpan duration = DateTime.Now - _startTime;
            // Đảm bảo bên XAML bạn đã đặt x:Name="txtUptime" cho cái TextBlock thời gian nhé!
            if (txtUptime != null)
            {
                txtUptime.Text = duration.ToString(@"hh\:mm\:ss");
            }
        }

        private void TaikhoanBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            TaikhoanPlaceholder.Visibility = string.IsNullOrWhiteSpace(TaikhoanBox.Text) ? Visibility.Visible : Visibility.Collapsed;
        }

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
                    new Uri("pack://application:,,,/MiniFTPServer_WPF;component/image/Eyeopen.png"));
            }
            else
            {
                NewPassPasswordBox.Password = NewPassVisibleBox.Text;
                NewPassVisibleBox.Visibility = Visibility.Collapsed;
                NewPassPasswordBox.Visibility = Visibility.Visible;

                NewPassEyeIcon.Source = new BitmapImage(
                    new Uri("pack://application:,,,/MiniFTPServer_WPF;component/image/Eyeclose.png"));
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
                    new Uri("pack://application:,,,/MiniFTPServer_WPF;component/image/Eyeopen.png"));
            }
            else
            {
                ConfirmPassPasswordBox.Password = ConfirmPassVisibleBox.Text;
                ConfirmPassVisibleBox.Visibility = Visibility.Collapsed;
                ConfirmPassPasswordBox.Visibility = Visibility.Visible;

                ConfirmPassEyeIcon.Source = new BitmapImage(
                    new Uri("pack://application:,,,/MiniFTPServer_WPF;component/image/Eyeclose.png"));
            }

            UpdateConfirmPasswordPlaceholder();
        }

        private void UpdateConfirmPasswordPlaceholder()
        {
            bool empty = string.IsNullOrEmpty(ConfirmPassPasswordBox.Password)
                         && string.IsNullOrEmpty(ConfirmPassVisibleBox.Text);

            ConfirmPassPlaceholder.Visibility = empty ? Visibility.Visible : Visibility.Collapsed;
        }

        private void CloseAddPanel()
        {
            AddPanel.Visibility = Visibility.Collapsed;
            Overlay.Visibility = Visibility.Collapsed;
        }

        private void CloseAddPanel_Click(object sender, RoutedEventArgs e)
        {
            CloseAddPanel();
        }

        private void Overlay_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            CloseAddPanel();
        }

        private void OpenAddPanel_Click(object sender, RoutedEventArgs e)
        {
            Overlay.Visibility = Visibility.Visible;
            Panel.SetZIndex(Overlay, 999);          // overlay phía dưới panel
            Panel.SetZIndex(AddPanel, 1000);
            AddPanel.Visibility = Visibility.Visible;
        }


    }
}