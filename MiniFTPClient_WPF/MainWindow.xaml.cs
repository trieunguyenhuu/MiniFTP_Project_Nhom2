using MiniFTPClient_WPF.home;
using MiniFTPClient_WPF.Services;
using MiniFTPClient_WPF.setting;
using MiniFTPClient_WPF.thungrac;
using MiniFTPClient_WPF.tinnhan;
using System.Collections.ObjectModel;
using System.ComponentModel;
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

namespace MiniFTPClient_WPF
{
    public class NotificationItem : INotifyPropertyChanged
    {
        private bool _isRead;

        public string Title { get; set; }
        public string Message { get; set; }
        public string Time { get; set; }
        public DateTime Timestamp { get; set; }

        public bool IsRead
        {
            get => _isRead;
            set
            {
                _isRead = value;
                OnPropertyChanged(nameof(IsRead));
                OnPropertyChanged(nameof(BackgroundColor));
            }
        }

        public Brush BackgroundColor => IsRead
            ? Brushes.White
            : new SolidColorBrush(Color.FromRgb(240, 245, 255));

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public partial class MainWindow : Window
    {
        private ObservableCollection<NotificationItem> _notifications = new ObservableCollection<NotificationItem>();
        public MainWindow()
        {
            InitializeComponent();
            MainFrame.Navigate(new MiniFTPClient_WPF.home.Page1());
            // Kiểm tra null để tránh lỗi nếu chưa kịp login mà chạy thẳng MainWindow để test
            if (FtpClientService.Instance.CurrentFullName != null)
            {
                txtUserName.Text = FtpClientService.Instance.CurrentFullName;
            }

            LoadNotifications();
            UpdateNotificationBadge();

            // Đăng ký sự kiện khi bấm nút X
            this.Closing += MainWindow_Closing;
        }

        private void SetHeader(string title)
        {
            HeaderTitle.Text = title;
        }

        private void trangchu_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new Page1());
            SetHeader("Trang chủ");
        }

        private void tinnhan_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new Tinnhan());
            SetHeader("Tin nhắn");
        }

        private void thungrac_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new Thungrac());
            SetHeader("Thùng rác");
        }

        private void setting_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new Setting());
            SetHeader("Cài đặt");
        }

        private void BtnLogout_Click(object sender, RoutedEventArgs e)
        {
            // 1. Tạo và hiển thị lại màn hình Đăng nhập
            LoginForm loginForm = new LoginForm();
            loginForm.Show();

            // 2. Đóng màn hình chính
            // Lưu ý: Lệnh Close() này sẽ kích hoạt sự kiện MainWindow_Closing ở dưới,
            // nên nó sẽ tự động gửi lệnh QUIT và ngắt kết nối FTP luôn.
            this.Close();
        }

        // THÊM 6 HÀM NÀY VÀO CUỐI CLASS MainWindow (trước dấu } cuối)

        private void LoadNotifications()
        {
            _notifications.Add(new NotificationItem
            {
                Title = "File mới từ admin",
                Message = "Project_Report.pdf (2.5 MB)",
                Time = "2 phút trước",
                Timestamp = DateTime.Now.AddMinutes(-2),
                IsRead = false
            });

            _notifications.Add(new NotificationItem
            {
                Title = "File mới từ john_doe",
                Message = "Marketing_Plan.pdf (4.8 MB)",
                Time = "1 giờ trước",
                Timestamp = DateTime.Now.AddHours(-1),
                IsRead = false
            });

            NotificationList.ItemsSource = _notifications;
        }

        private void UpdateNotificationBadge()
        {
            int unreadCount = _notifications.Count(n => !n.IsRead);

            if (unreadCount > 0)
            {
                NotificationBadge.Visibility = Visibility.Visible;
                NotificationCount.Text = unreadCount > 99 ? "99+" : unreadCount.ToString();
            }
            else
            {
                NotificationBadge.Visibility = Visibility.Collapsed;
            }
        }

        private void NotificationButton_Click(object sender, RoutedEventArgs e)
        {
            NotificationPopup.IsOpen = !NotificationPopup.IsOpen;
        }

        private void MarkAllRead_Click(object sender, RoutedEventArgs e)
        {
            foreach (var notification in _notifications)
            {
                notification.IsRead = true;
            }
            UpdateNotificationBadge();
        }

        private void NotificationItem_MouseEnter(object sender, MouseEventArgs e)
        {
            if (sender is Border border)
            {
                border.Background = new SolidColorBrush(Color.FromRgb(249, 250, 251));
            }
        }

        private void NotificationItem_MouseLeave(object sender, MouseEventArgs e)
        {
            if (sender is Border border && border.DataContext is NotificationItem notification)
            {
                border.Background = notification.BackgroundColor;
            }
        }

        // HÀM QUAN TRỌNG - để gửi thông báo từ các page khác
        public void AddNotification(string title, string message)
        {
            var newNotification = new NotificationItem
            {
                Title = title,
                Message = message,
                Time = "Vừa xong",
                Timestamp = DateTime.Now,
                IsRead = false
            };

            _notifications.Insert(0, newNotification);
            UpdateNotificationBadge();
        }

        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // Trước khi tắt, gửi lệnh QUIT cho server biết
            if (FtpClientService.Instance.IsConnected)
            {
                FtpClientService.Instance.Disconnect();
            }
        }
    }
}