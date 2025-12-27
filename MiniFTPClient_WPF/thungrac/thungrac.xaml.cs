using MiniFTPClient_WPF.Services;
using MiniFTPClient_WPF.Models;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace MiniFTPClient_WPF.thungrac
{

    using System.ComponentModel;
    using System.Windows.Input;

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
            var selectedItems = _items.Where(i => i.IsSelected).ToList();

            if (selectedItems.Count == 0)
            {
                MessageBox.Show("Vui lòng chọn file cần xóa.", "Thông báo");
                return;
            }

            var result = MessageBox.Show(
                $"XÓA VĨNH VIỄN {selectedItems.Count} file đã chọn?\nHành động này KHÔNG THỂ HOÀN TÁC!",
                "Cảnh báo nghiêm trọng",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes)
                return;

            bool allOk = true;

            foreach (var item in selectedItems)
            {
                bool ok = await FtpClientService.Instance.PermanentDeleteAsync(item.FileId);
                if (!ok)
                    allOk = false;
            }

            if (allOk)
                MessageBox.Show("Đã xóa các file đã chọn!", "Thành công");
            else
                MessageBox.Show("Một số file không thể xóa!", "Lỗi");

            await LoadTrashData();
        }




        // tick chọn tất cả 
        private void SelectAllCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            foreach (var item in _items)
            {
                item.IsSelected = true;
            }

            //filedatagrid.Items.Refresh(); // Đảm bảo DataGrid vẽ lại
        }

        // huỷ tick chọn tất cả
        private void SelectAllCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            foreach (var item in _items)
            {
                item.IsSelected = false;
            }

            //filedatagrid.Items.Refresh();
        }

        // hàm chọn khi tick vào file
        private void filedatagrid_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            // Lấy vị trí chuột
            var point = e.GetPosition(filedatagrid);

            // Hit test để tìm phần tử dưới chuột
            var hit = VisualTreeHelper.HitTest(filedatagrid, point);
            if (hit == null) return;

            // Tìm dòng tương ứng
            var row = FindParent<DataGridRow>(hit.VisualHit);
            if (row == null) return;

            // Lấy item tương ứng
            if (row.Item is TrashItem item)
            {
                item.IsSelected = !item.IsSelected;
            }
        }

        // hàm để tìm dòng chứa file tương ứng khi click chọn
        private T FindParent<T>(DependencyObject child) where T : DependencyObject
        {
            DependencyObject parent = VisualTreeHelper.GetParent(child);

            while (parent != null && parent is not T)
            {
                parent = VisualTreeHelper.GetParent(parent);
            }

            return parent as T;
        }



    }
}
