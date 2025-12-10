using MiniFTPClient_WPF.home;
using MiniFTPClient_WPF.Services;
using MiniFTPClient_WPF.setting;
using MiniFTPClient_WPF.thungrac;
using MiniFTPClient_WPF.tinnhan;
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
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            MainFrame.Navigate(new MiniFTPClient_WPF.home.Page1());
            txtUserName.Text = FtpClientService.Instance.CurrentFullName;
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