using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Collections.ObjectModel;
using System.Windows.Media;

namespace MiniFTPClient_WPF.thungrac
{
    // Model dữ liệu cho 1 dòng trong thùng rác
    public class TrashItem
    {
        public bool IsSelected { get; set; }

        // Gán giá trị mặc định để không còn CS8618
        public string Name { get; set; } = string.Empty;
        public string OriginalPath { get; set; } = string.Empty;
        public DateTime DeletedDate { get; set; } = DateTime.Now;
        public string Size { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public Brush IconBackground { get; set; } = Brushes.Transparent;
    }

    public partial class Thungrac : Page
    {
        // Khởi tạo luôn để không bị CS8618
        private ObservableCollection<TrashItem> _items = new ObservableCollection<TrashItem>();

        public Thungrac()
        {
            InitializeComponent();
            LoadSampleData();
            UpdateButtonsState();
        }

        // --- Tải dữ liệu mẫu ---
        private void LoadSampleData()
        {
            _items = new ObservableCollection<TrashItem>
            {
                new TrashItem { Name="Dự án 2024", OriginalPath="/Documents/Projects", DeletedDate=DateTime.Now.AddDays(-1), Size="245 MB", Icon="📁", IconBackground=new SolidColorBrush(Color.FromRgb(79,70,229)) },
                new TrashItem { Name="Báo cáo tháng 10.docx", OriginalPath="/Documents", DeletedDate=DateTime.Now.AddDays(-2), Size="2.4 MB", Icon="📄", IconBackground=new SolidColorBrush(Color.FromRgb(75,85,99)) },
                new TrashItem { Name="Ảnh du lịch", OriginalPath="/Pictures", DeletedDate=DateTime.Now.AddDays(-3), Size="1.2 GB", Icon="📁", IconBackground=new SolidColorBrush(Color.FromRgb(79,70,229)) },
                new TrashItem { Name="presentation.pdf", OriginalPath="/Documents/Work", DeletedDate=DateTime.Now.AddDays(-4), Size="5.8 MB", Icon="📄", IconBackground=new SolidColorBrush(Color.FromRgb(75,85,99)) },
                new TrashItem { Name="video.mp4", OriginalPath="/Videos", DeletedDate=DateTime.Now.AddDays(-5), Size="850 MB", Icon="🎬", IconBackground=new SolidColorBrush(Color.FromRgb(56,189,248)) },
                new TrashItem { Name="Hóa đơn.xlsx", OriginalPath="/Documents/Finance", DeletedDate=DateTime.Now.AddDays(-6), Size="430 KB", Icon="📄", IconBackground=new SolidColorBrush(Color.FromRgb(75,85,99)) },
                new TrashItem { Name="Thiết kế UI", OriginalPath="/Design", DeletedDate=DateTime.Now.AddDays(-7), Size="320 MB", Icon="🎨", IconBackground=new SolidColorBrush(Color.FromRgb(244,114,182)) },
                new TrashItem { Name="Log hệ thống", OriginalPath="/Logs", DeletedDate=DateTime.Now.AddDays(-8), Size="120 MB", Icon="🧾", IconBackground=new SolidColorBrush(Color.FromRgb(34,197,94)) },
                new TrashItem { Name="Backup.zip", OriginalPath="/Backups", DeletedDate=DateTime.Now.AddDays(-9), Size="3.2 GB", Icon="🗂", IconBackground=new SolidColorBrush(Color.FromRgb(14,165,233)) },
                new TrashItem { Name="note.txt", OriginalPath="/Desktop", DeletedDate=DateTime.Now.AddDays(-10), Size="4 KB", Icon="📝", IconBackground=new SolidColorBrush(Color.FromRgb(107,114,128)) },
            };

            TrashGrid.ItemsSource = _items;
        }

        // --- Cập nhật nút khi tick chọn ---
        private void RowCheckBox_Changed(object sender, RoutedEventArgs e)
        {
            UpdateButtonsState();
        }

        private void UpdateButtonsState()
        {
            int count = _items.Count(i => i.IsSelected);
            bool any = count > 0;

            SelectedInfoButton.IsEnabled = any;
            RestoreButton.IsEnabled = any;
            DeleteForeverButton.IsEnabled = any;

            SelectedInfoButton.Content = any
                ? $"{count} mục được chọn"
                : "0 mục được chọn";
        }

        // --- Làm trống toàn bộ ---
        private void EmptyBin_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Bạn có chắc muốn làm trống toàn bộ thùng rác?",
                                 "Xác nhận",
                                 MessageBoxButton.YesNo,
                                 MessageBoxImage.Warning) == MessageBoxResult.Yes)
            {
                _items.Clear();
                UpdateButtonsState();
            }
        }

        // --- Khôi phục ---
        private void Restore_Click(object sender, RoutedEventArgs e)
        {
            var selectedItems = _items.Where(i => i.IsSelected).ToList();
            if (!selectedItems.Any()) return;

            MessageBox.Show($"Khôi phục {selectedItems.Count} mục (DEMO)",
                            "Khôi phục",
                            MessageBoxButton.OK,
                            MessageBoxImage.Information);

            foreach (var item in selectedItems)
                _items.Remove(item);

            UpdateButtonsState();
        }

        // --- Xóa vĩnh viễn ---
        private void DeleteForever_Click(object sender, RoutedEventArgs e)
        {
            var selectedItems = _items.Where(i => i.IsSelected).ToList();
            if (!selectedItems.Any()) return;

            if (MessageBox.Show($"Bạn có chắc muốn xóa vĩnh viễn {selectedItems.Count} mục?",
                                 "Cảnh báo",
                                 MessageBoxButton.YesNo,
                                 MessageBoxImage.Error) == MessageBoxResult.Yes)
            {
                foreach (var item in selectedItems)
                    _items.Remove(item);
            }

            UpdateButtonsState();
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            SearchPlaceholder.Visibility = string.IsNullOrWhiteSpace(SearchBox.Text) ? Visibility.Visible : Visibility.Collapsed;
        }
    }
}
