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
        public int FileId { get; set; }        // ← THÊM MỚI
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
                //TxtFileCount.Text = $"{_items.Count} file";
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
            
        }

        // Xóa vĩnh viễn
        private async void BtnPermanentDelete_Click(object sender, RoutedEventArgs e)
        {
           
        }

        // Dọn dẹp thùng rác
        private async void BtnEmptyTrash_Click(object sender, RoutedEventArgs e)
        {
            
        }

    }
}
