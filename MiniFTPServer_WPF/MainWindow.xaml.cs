using MiniFtpServer_WPF.Services;
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
            _server.Start();
            btnStart.IsEnabled = false;
            btnStop.IsEnabled = true;
            lblStatus.Text = "Trạng thái: Đang chạy";

            _startTime = DateTime.Now; // Mốc thời gian bắt đầu
            _timer.Start();            // BẮT ĐẦU CHẠY ĐỒNG HỒ
        }

        private void btnStop_Click(object sender, RoutedEventArgs e)
        {
            _server.Stop();
            _timer.Stop();
            txtUptime.Text = "00:00:00";
            btnStart.IsEnabled = true;
            btnStop.IsEnabled = false;
            lblStatus.Text = "Trạng thái: Đã dừng";
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

    }
}