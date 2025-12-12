using MiniFTPClient_WPF.Services;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace MiniFTPClient_WPF.thungrac
{

    public class TrashItem
    {
        public bool IsSelected { get; set; }
        public int FileId { get; set; }  
        public string FileName { get; set; } = string.Empty;
        public string OriginalPath { get; set; } = string.Empty;
        public DateTime DeletedDate { get; set; } = DateTime.Now;
        public string Size { get; set; } = string.Empty;
    }

    public partial class Thungrac : Page
    {
        private ObservableCollection<TrashItem> _items =
            new ObservableCollection<TrashItem>();

        public Thungrac()
        {
            InitializeComponent();
            _ = LoadTrashData();
        }

        private async Task LoadTrashData()
        {
            try
            {
                if (!FtpClientService.Instance.IsConnected)
                {
                    MessageBox.Show("Chưa kết nối tới server", "Cảnh báo");
                    return;
                }

                var trashItems = await FtpClientService.Instance.GetTrashAsync();

                _items.Clear();
                foreach (var item in trashItems)
                {
                    _items.Add(item);
                }

                filedatagrid.ItemsSource = _items;

                // Cập nhật số lượng
                TxtFileCount.Text = $"{_items.Count} file";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi tải thùng rác: {ex.Message}", "Lỗi");
            }
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            SearchPlaceholder.Visibility =
                string.IsNullOrWhiteSpace(SearchBox.Text)
                    ? Visibility.Visible
                    : Visibility.Collapsed;
        }

        // Khôi phục file
        private async void BtnRestore_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            if (btn?.DataContext is not TrashItem item) return;

            var result = MessageBox.Show(
                $"Khôi phục file '{item.FileName}'?",
                "Xác nhận",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                bool ok = await FtpClientService.Instance.RestoreFileAsync(item.FileId);

                if (ok)
                {
                    MessageBox.Show("Khôi phục thành công!", "Thành công");
                    await LoadTrashData();
                }
                else
                {
                    MessageBox.Show("Khôi phục thất bại!", "Lỗi");
                }
            }
        }

        // Xóa vĩnh viễn
        private async void BtnPermanentDelete_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            if (btn?.DataContext is not TrashItem item) return;

            var result = MessageBox.Show(
                $"XÓA VĨNH VIỄN file '{item.FileName}'?\nHành động này KHÔNG THỂ HOÀN TÁC!",
                "Cảnh báo nghiêm trọng",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                bool ok = await FtpClientService.Instance.PermanentDeleteAsync(item.FileId);

                if (ok)
                {
                    MessageBox.Show("Đã xóa vĩnh viễn!", "Thành công");
                    await LoadTrashData();
                }
                else
                {
                    MessageBox.Show("Xóa thất bại!", "Lỗi");
                }
            }
        }

        // Dọn dẹp thùng rác
        private async void BtnEmptyTrash_Click(object sender, RoutedEventArgs e)
        {
            if (_items.Count == 0)
            {
                MessageBox.Show("Thùng rác đã trống!", "Thông báo");
                return;
            }

            var result = MessageBox.Show(
                $"XÓA VĨNH VIỄN TẤT CẢ {_items.Count} file?\nHành động này KHÔNG THỂ HOÀN TÁC!",
                "Cảnh báo nghiêm trọng",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                bool ok = await FtpClientService.Instance.EmptyTrashAsync();

                if (ok)
                {
                    MessageBox.Show("Đã dọn dẹp thùng rác!", "Thành công");
                    await LoadTrashData();
                }
                else
                {
                    MessageBox.Show("Dọn dẹp thất bại!", "Lỗi");
                }
            }
        }

    }
}
