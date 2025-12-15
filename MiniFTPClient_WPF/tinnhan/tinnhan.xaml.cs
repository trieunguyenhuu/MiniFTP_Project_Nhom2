using MiniFTPClient_WPF.Services;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace MiniFTPClient_WPF.tinnhan
{
    public class MessageItem
    {
        public int FileId { get; set; }
        public long SizeInBytes { get; set; }  // Để sắp xếp theo dung lượng
        public bool IsRead { get; set; }       // Đánh dấu đã đọc
        public string Sender { get; set; }
        public string Time { get; set; }
        public string FileName { get; set; }
        public string Size { get; set; }
        public bool IsReceived { get; set; }
        public DateTime Date { get; set; }
        public string AvatarPath { get; set; }
        private bool _isAccepted;  // THÊM DÒNG NÀY

        public string Initial
        {
            get
            {
                if (string.IsNullOrWhiteSpace(Sender))
                    return "?";
                return Sender.Substring(0, 1).ToUpper();
            }
        }
        public bool IsAccepted
        {
            get => _isAccepted;
            set
            {
                _isAccepted = value;
                OnPropertyChanged(nameof(IsAccepted));
                OnPropertyChanged(nameof(ShowDownloadButton));
                OnPropertyChanged(nameof(ShowAcceptDeclineButtons));
            }
        }

        public Visibility ShowDownloadButton => IsAccepted ? Visibility.Visible : Visibility.Collapsed;
        public Visibility ShowAcceptDeclineButtons => IsAccepted ? Visibility.Collapsed : Visibility.Visible;
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

    }

    public partial class Tinnhan : Page
    {
        private readonly ObservableCollection<MessageItem> _receivedMessages =
            new ObservableCollection<MessageItem>();

        private readonly ObservableCollection<MessageItem> _sentMessages =
            new ObservableCollection<MessageItem>();

        private ListCollectionView _receivedView;
        private ListCollectionView _sentView;
        public Tinnhan()
        {
            InitializeComponent();
            //LoadDummyData();
            _ = LoadRealData();
            BindData();
            SetupFilters();
        }

        private async Task LoadRealData()
        {
            if (!FtpClientService.Instance.IsConnected) return;

            // --- PHẦN 1: TIN ĐÃ NHẬN (RECEIVED) ---
            _receivedMessages.Clear();
            var sharedFiles = await FtpClientService.Instance.GetSharedFilesAsync();

            foreach (var file in sharedFiles)
            {
                _receivedMessages.Add(new MessageItem
                {
                    FileId = file.FileId,
                    Sender = file.OwnerName, // Người gửi
                    FileName = file.FileName,
                    Size = file.FormattedSize,
                    SizeInBytes = file.FileSize,
                    Time = "Mới đây",
                    Date = DateTime.Now,
                    IsReceived = true,
                    IsAccepted = false,
                    AvatarPath = "pack://application:,,,/MiniFTPClient_WPF;component/anh/karina.jpg"
                });
            }

            // --- PHẦN 2: TIN ĐÃ GỬI (SENT) - THÊM MỚI ---
            _sentMessages.Clear();
            var sentFiles = await FtpClientService.Instance.GetSentFilesAsync();

            foreach (var file in sentFiles)
            {
                _sentMessages.Add(new MessageItem
                {
                    // Với tin đã gửi, trường Sender ta sẽ hiển thị Tên người nhận 
                    // để giao diện hiện: "Nguyễn Văn A" -> "bạn đã gửi 1 file"
                    Sender = file.OwnerName, // Ở bước 3 ta đã map ReceiverName vào biến OwnerName
                    FileName = file.FileName,
                    Size = file.FormattedSize,
                    SizeInBytes = file.FileSize,
                    Time = "Mới đây",
                    Date = DateTime.Now,
                    IsReceived = false, // Đánh dấu là tin gửi đi
                    AvatarPath = "pack://application:,,,/MiniFTPClient_WPF;component/anh/karina.jpg"
                });
            }
        }

        private void SetupFilters()
        {
            CbReceiveDateFilter.SelectionChanged += FilterReceived_Changed;
            CbSenderFilter.SelectionChanged += FilterReceived_Changed;
        }

        // Trong Tinnhan.xaml.cs

        private async void BtnDecline_Click(object sender, RoutedEventArgs e)
        {
            // 1. Lấy thông tin dòng đang chọn
            if (sender is Button btn && btn.DataContext is MessageItem msg)
            {
                var result = MessageBox.Show($"Bạn có chắc muốn từ chối nhận file '{msg.FileName}' không?",
                    "Xác nhận từ chối", MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    // 2. Gọi Server hủy share
                    // UnshareFileAsync(FileID, UserID_Của_Mình) -> Tự xóa mình khỏi danh sách được share
                    bool ok = await FtpClientService.Instance.UnshareFileAsync(
                        msg.FileId,
                        FtpClientService.Instance.CurrentUserId
                    );

                    if (ok)
                    {
                        // 3. Xóa khỏi giao diện
                        _receivedMessages.Remove(msg);

                        MessageBox.Show("Đã từ chối file thành công. File đã bị xóa khỏi danh sách của bạn.",
                            "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        MessageBox.Show("Lỗi: Không thể từ chối file này.", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }
        private async void Btn_Download_Shared_Click(object sender, RoutedEventArgs e)
        {
            //await FtpClientService.Instance.DownloadFileAsync(item.Id, dlg.FileName, item.SizeBytes);
        }
        private async void Btn_Accept_Click(object sender, RoutedEventArgs e)
        {
            //var button = sender as Button;
            //var message = button?.DataContext as MessageItem;

            //if (message != null)
            //{
            //    message.IsAccepted = true;

            //    MessageBox.Show($"Đã chấp nhận file: {message.FileName}", "Chấp nhận",
            //        MessageBoxButton.OK, MessageBoxImage.Information);
            //}
            if (sender is Button btn && btn.DataContext is MessageItem msg)
            {
                msg.IsAccepted = true;

                // Ép refresh để UI cập nhật Visibility
                CollectionViewSource.GetDefaultView(ReceivedList.ItemsSource)?.Refresh();
            }

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
            _receivedView = new ListCollectionView(_receivedMessages);
            _receivedView.GroupDescriptions.Add(new PropertyGroupDescription(nameof(MessageItem.Date)));
            _receivedView.SortDescriptions.Add(new SortDescription(nameof(MessageItem.Date), ListSortDirection.Descending)); // THÊM DÒNG NÀY
            ReceivedList.ItemsSource = _receivedView;

            _sentView = new ListCollectionView(_sentMessages);
            _sentView.GroupDescriptions.Add(new PropertyGroupDescription(nameof(MessageItem.Date)));
            _sentView.SortDescriptions.Add(new SortDescription(nameof(MessageItem.Date), ListSortDirection.Descending)); // THÊM DÒNG NÀY
            SentList.ItemsSource = _sentView;
        }

        // Search Received
        private void SearchBoxReceived_TextChanged(object sender, TextChangedEventArgs e)
        {
            SearchPlaceholderReceived.Visibility =
                string.IsNullOrWhiteSpace(SearchBoxReceived.Text)
                    ? Visibility.Visible
                    : Visibility.Collapsed;

            FilterReceived_Changed(null, null);
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
            TabSent.IsChecked = false;

            ReceivedPanel.Visibility = Visibility.Visible;
            SentPanel.Visibility = Visibility.Collapsed;

            SearchReceivedBorder.Visibility = Visibility.Visible;
            FilterReceivedPanel.Visibility = Visibility.Visible;

            SearchSentBorder.Visibility = Visibility.Collapsed;
            FilterSentPanel.Visibility = Visibility.Collapsed;
        }

        private void TabSent_Click(object sender, RoutedEventArgs e)
        {
            TabReceived.IsChecked = false;

            SentPanel.Visibility = Visibility.Visible;
            ReceivedPanel.Visibility = Visibility.Collapsed;

            SearchSentBorder.Visibility = Visibility.Visible;
            FilterSentPanel.Visibility = Visibility.Visible;

            SearchReceivedBorder.Visibility = Visibility.Collapsed;
            FilterReceivedPanel.Visibility = Visibility.Collapsed;
        }

        private void FilterReceived_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (_receivedView == null) return;

            _receivedView.Filter = item =>
            {
                if (item is not MessageItem msg) return false;

                // Lọc theo ngày
                var dateFilter = (CbReceiveDateFilter.SelectedItem as ComboBoxItem)?.Content.ToString();
                if (!FilterByDate(msg, dateFilter))
                    return false;

                // Lọc theo dung lượng
                var sizeFilter = (CbSenderFilter.SelectedItem as ComboBoxItem)?.Content.ToString();
                if (!FilterBySize(msg, sizeFilter))
                    return false;

                // Lọc theo search box
                var searchText = SearchBoxReceived.Text.Trim().ToLower();
                if (!string.IsNullOrEmpty(searchText) && !msg.Sender.ToLower().Contains(searchText))
                    return false;

                return true;
            };
            _receivedView.Refresh();
        }

        private bool FilterByDate(MessageItem msg, string filter)
        {
            if (string.IsNullOrEmpty(filter) || filter == "Tất cả") return true;
            if (filter == "Hôm nay") return msg.Date.Date == DateTime.Now.Date;
            if (filter == "7 ngày trước") return msg.Date >= DateTime.Now.AddDays(-7);
            return true;
        }

        private bool FilterBySize(MessageItem msg, string filter)
        {
            if (string.IsNullOrEmpty(filter) || filter == "Tất cả") return true;

            long sizeInBytes = msg.SizeInBytes;
            if (filter.Contains("Nhỏ")) return sizeInBytes < 1048576; // < 1MB
            if (filter.Contains("Trung bình")) return sizeInBytes >= 1048576 && sizeInBytes <= 10485760; // 1-10MB
            if (filter.Contains("Lớn")) return sizeInBytes > 10485760; // > 10MB

            return true;
        }

        public class BoolToVisibilityConverter : IValueConverter
        {
            public bool Inverse { get; set; } // đảo điều kiện

            public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            {
                bool flag = value is bool b && b;
                if (Inverse) flag = !flag;
                return flag ? Visibility.Visible : Visibility.Collapsed;
            }

            public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
                => throw new NotImplementedException();
        }
    }
}
