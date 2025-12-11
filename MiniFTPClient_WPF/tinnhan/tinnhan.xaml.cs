using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace MiniFTPClient_WPF.tinnhan
{
    public class MessageItem
    {
        public string Sender { get; set; }
        public string Time { get; set; }
        public string FileName { get; set; }
        public string Size { get; set; }
        public bool IsReceived { get; set; }
        public DateTime Date { get; set; }
        public string AvatarPath { get; set; }

        public string Initial
        {
            get
            {
                if (string.IsNullOrWhiteSpace(Sender))
                    return "?";
                return Sender.Substring(0, 1).ToUpper();
            }
        }
    }

    public partial class Tinnhan : Page
    {
        private readonly ObservableCollection<MessageItem> _receivedMessages =
            new ObservableCollection<MessageItem>();

        private readonly ObservableCollection<MessageItem> _sentMessages =
            new ObservableCollection<MessageItem>();

        public Tinnhan()
        {
            InitializeComponent();
            LoadDummyData();
            BindData();
        }

        private void LoadDummyData()
        {
            // Tin đã nhận
            _receivedMessages.Add(new MessageItem
            {
                Sender = "admin",
                FileName = "Project_Report.pdf",
                Size = "2.5 MB",
                Time = "20:46",
                Date = new DateTime(2025, 11, 27),
                IsReceived = true,
                AvatarPath = "pack://application:,,,/MiniFTPClient_WPF;component/anh/karina.jpg"
            });

            _receivedMessages.Add(new MessageItem
            {
                Sender = "admin",
                FileName = "Avatar.png",
                Size = "320 KB",
                Time = "10:15",
                Date = new DateTime(2025, 11, 24),
                IsReceived = true,
                AvatarPath = "pack://application:,,,/MiniFTPClient_WPF;component/anh/karina.jpg"
            });

            _receivedMessages.Add(new MessageItem
            {
                Sender = "admin",
                FileName = "ảnh.png",
                Size = "320 KB",
                Time = "10:17",
                Date = new DateTime(2025, 11, 24),
                IsReceived = true,
                AvatarPath = "pack://application:,,,/MiniFTPClient_WPF;component/anh/karina.jpg"
            });

            _receivedMessages.Add(new MessageItem
            {
                Sender = "admin",
                FileName = "Thống kê.sxln",
                Size = "320 KB",
                Time = "10:25",
                Date = new DateTime(2025, 11, 24),
                IsReceived = true,
                AvatarPath = "pack://application:,,,/MiniFTPClient_WPF;component/anh/karina.jpg"
            });

            // Tin đã gửi
            _sentMessages.Add(new MessageItem
            {
                Sender = "admin",
                FileName = "Marketing_Plan_Q1.pdf",
                Size = "4.8 MB",
                Time = "22:46",
                Date = new DateTime(2025, 11, 24),
                IsReceived = false,
                AvatarPath = "pack://application:,,,/MiniFTPClient_WPF;component/anh/karina.jpg"
            });

            _sentMessages.Add(new MessageItem
            {
                Sender = "admin",
                FileName = "Invoice_2025_11.xlsx",
                Size = "1.2 MB",
                Time = "09:02",
                Date = new DateTime(2025, 11, 27),
                IsReceived = false,
                AvatarPath = "pack://application:,,,/MiniFTPClient_WPF;component/anh/karina.jpg"
            });

            _sentMessages.Add(new MessageItem
            {
                Sender = "admin",
                FileName = "Invoice_2025_11.xlsx",
                Size = "1.2 MB",
                Time = "09:02",
                Date = new DateTime(2025, 11, 27),
                IsReceived = false,
                AvatarPath = "pack://application:,,,/MiniFTPClient_WPF;component/anh/karina.jpg"
            });

            _sentMessages.Add(new MessageItem
            {
                Sender = "admin",
                FileName = "Invoice_2025_11.xlsx",
                Size = "1.2 MB",
                Time = "09:02",
                Date = new DateTime(2025, 11, 27),
                IsReceived = false,
                AvatarPath = "pack://application:,,,/MiniFTPClient_WPF;component/anh/karina.jpg"
            });
        }

        private void BindData()
        {
            var viewReceive = new ListCollectionView(_receivedMessages);
            viewReceive.GroupDescriptions.Add(new PropertyGroupDescription(nameof(MessageItem.Date)));
            ReceivedList.ItemsSource = viewReceive;

            var viewSent = new ListCollectionView(_sentMessages);
            viewSent.GroupDescriptions.Add(new PropertyGroupDescription(nameof(MessageItem.Date)));
            SentList.ItemsSource = viewSent;
        }

        // Search Received
        private void SearchBoxReceived_TextChanged(object sender, TextChangedEventArgs e)
        {
            SearchPlaceholderReceived.Visibility =
                string.IsNullOrWhiteSpace(SearchBoxReceived.Text)
                    ? Visibility.Visible
                    : Visibility.Collapsed;

            // TODO: thêm filter theo _receivedMessages nếu bạn muốn
        }

        // Search Sent
        private void SearchBoxSent_TextChanged(object sender, TextChangedEventArgs e)
        {
            SearchPlaceholderSent.Visibility =
                string.IsNullOrWhiteSpace(SearchBoxSent.Text)
                    ? Visibility.Visible
                    : Visibility.Collapsed;

            // TODO: thêm filter theo _sentMessages nếu bạn muốn
        }

        private void TabReceived_Click(object sender, RoutedEventArgs e)
        {
            ReceivedPanel.Visibility = Visibility.Visible;
            SentPanel.Visibility = Visibility.Collapsed;

            SearchReceivedBorder.Visibility = Visibility.Visible;
            FilterReceivedPanel.Visibility = Visibility.Visible;

            SearchSentBorder.Visibility = Visibility.Collapsed;
            FilterSentPanel.Visibility = Visibility.Collapsed;
        }

        private void TabSent_Click(object sender, RoutedEventArgs e)
        {
            SentPanel.Visibility = Visibility.Visible;
            ReceivedPanel.Visibility = Visibility.Collapsed;

            SearchSentBorder.Visibility = Visibility.Visible;
            FilterSentPanel.Visibility = Visibility.Visible;

            SearchReceivedBorder.Visibility = Visibility.Collapsed;
            FilterReceivedPanel.Visibility = Visibility.Collapsed;
        }

    }
}
