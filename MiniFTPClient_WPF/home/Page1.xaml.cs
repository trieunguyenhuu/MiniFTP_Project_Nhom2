using Microsoft.Win32;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;

using MiniFTPClient_WPF.Models;
using MiniFTPClient_WPF.Services;

namespace MiniFTPClient_WPF.home
{
    public partial class Page1 : Page
    {
        public ObservableCollection<string> Breadcrumbs { get; } = new ObservableCollection<string>();
        public ObservableCollection<FileItem> Files { get; } = new ObservableCollection<FileItem>();

        private readonly ObservableCollection<UserItem> _users = new ObservableCollection<UserItem>();
        private string _selectedFilePath = null;
        private bool _isLoading = false;

        public Page1()
        {
            InitializeComponent();
            this.DataContext = this;

            Breadcrumbs.Add("Home");
            _ = LoadFilesFromServer();
            InitSampleUsers();
            RecipientList.ItemsSource = _users;
            UpdateShareButtonState();
        }

        // ==================== TẢI DỮ LIỆU TỪ SERVER ====================
        private async Task LoadFilesFromServer()
        {
            if (_isLoading) return; // Tránh gọi nhiều lần
            _isLoading = true;

            try
            {
                // Disable các nút trong lúc loading
                SetButtonsEnabled(false);

                Files.Clear();

                if (!FtpClientService.Instance.IsConnected)
                {
                    MessageBox.Show("Chưa kết nối tới server", "Cảnh báo",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var svFiles = await FtpClientService.Instance.GetListingAsync();

                foreach (var f in svFiles)
                {
                    Files.Add(f);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Không thể tải danh sách file:\n{ex.Message}",
                    "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                _isLoading = false;
                SetButtonsEnabled(true);
            }
        }

        // ==================== HELPER: BẬT/TẮT CÁC NÚT ====================
        private void SetButtonsEnabled(bool enabled)
        {
            if (btnUpload != null) btnUpload.IsEnabled = enabled;
            if (BtnRefresh != null) BtnRefresh.IsEnabled = enabled;
        }

        // ==================== BREADCRUMB & NAVIGATION ====================
        private void FileListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (FileListBox.SelectedItem is FileItem item && item.IsFolder)
            {
                var folderName = item.Name.TrimEnd('/');
                NavigateInto(folderName);
                FileListBox.SelectedItem = null;
            }
        }

        private void Breadcrumb_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is TextBlock tb)
            {
                var crumb = tb.Text;
                var idx = Breadcrumbs.IndexOf(crumb);
                if (idx >= 0)
                {
                    NavigateToBreadcrumb(idx);
                }
            }
        }

        private void NavigateInto(string folderName)
        {
            Breadcrumbs.Add(folderName);
            _ = LoadFilesFromServer();
        }

        private void NavigateToBreadcrumb(int index)
        {
            while (Breadcrumbs.Count - 1 > index)
                Breadcrumbs.RemoveAt(Breadcrumbs.Count - 1);

            _ = LoadFilesFromServer();
        }

        // ==================== USER SAMPLE DATA ====================
        public class UserItem
        {
            public string Name { get; set; } = "";
            public string AvatarPath { get; set; } = "";
        }

        private void InitSampleUsers()
        {
            _users.Add(new UserItem { Name = "Kiều Dung", AvatarPath = "pack://application:,,,/MiniFTPClient_WPF;component/anh/karina.jpg" });
            _users.Add(new UserItem { Name = "Sly 🐰", AvatarPath = "pack://application:,,,/MiniFTPClient_WPF;component/anh/karina.jpg" });
            _users.Add(new UserItem { Name = "Mai Kiều Trang", AvatarPath = "pack://application:,,,/MiniFTPClient_WPF;component/anh/karina.jpg" });
            _users.Add(new UserItem { Name = "Admin", AvatarPath = "pack://application:,,,/MiniFTPClient_WPF;component/anh/karina.jpg" });
        }

        // ==================== SHARE PANEL LOGIC ====================
        private async void BtnShare_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var realUsers = await FtpClientService.Instance.GetUsersAsync();
                _users.Clear();

                foreach (var name in realUsers)
                {
                    _users.Add(new UserItem
                    {
                        Name = name,
                        AvatarPath = "pack://application:,,,/MiniFTPClient_WPF;component/anh/karina.jpg"
                    });
                }

                Overlay.Visibility = Visibility.Visible;
                Panel.SetZIndex(Overlay, 999);
                Panel.SetZIndex(SharePanel, 1000);
                SharePanel.Visibility = Visibility.Visible;
                RecipientList.Focus();
                UpdateShareButtonState();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Không thể tải danh sách người dùng:\n{ex.Message}",
                    "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CloseSharePanel_Click(object sender, RoutedEventArgs e)
        {
            SharePanel.Visibility = Visibility.Collapsed;
            Overlay.Visibility = Visibility.Collapsed;
            Panel.SetZIndex(SharePanel, 0);
            Panel.SetZIndex(Overlay, 0);

            _selectedFilePath = null;
            TxtSelectedFile.Text = "(Chưa chọn file)";
            BtnDoShare.IsEnabled = false;
            RecipientList.SelectedItem = null;
        }

        private void Overlay_MouseDown(object sender, MouseButtonEventArgs e)
        {
            CloseSharePanel_Click(sender, null);
        }

        private void RecipientList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateShareButtonState();
        }

        private void UpdateShareButtonState()
        {
            bool hasRecipient = RecipientList.SelectedItem != null;
            bool hasFile = !string.IsNullOrWhiteSpace(TxtSelectedFile.Text) &&
                           TxtSelectedFile.Text != "(Chưa chọn file)";
            BtnDoShare.IsEnabled = hasRecipient && hasFile;
        }

        private void BtnDoShare_Click(object sender, RoutedEventArgs e)
        {
            if (RecipientList.SelectedItem is not UserItem user)
            {
                MessageBox.Show("Vui lòng chọn người nhận.", "Thông báo",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var fileName = TxtSelectedFile.Text;
            if (string.IsNullOrWhiteSpace(fileName) || fileName == "(Chưa chọn file)")
            {
                MessageBox.Show("Vui lòng chọn file để chia sẻ.", "Thông báo",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            MessageBox.Show($"Đã gửi yêu cầu chia sẻ file: {fileName}\nĐến: {user.Name}",
                "Chia sẻ thành công", MessageBoxButton.OK, MessageBoxImage.Information);
            CloseSharePanel_Click(sender, e);
        }

        private void ShowShareFor(FileItem file)
        {
            if (file == null) return;

            _selectedFilePath = null;
            TxtSelectedFile.Text = file.Name;

            Overlay.Visibility = Visibility.Visible;
            Panel.SetZIndex(SharePanel, 1000);
            SharePanel.Visibility = Visibility.Visible;

            RecipientList.Focus();
            UpdateShareButtonState();
        }

        // ==================== CONTEXT MENU ====================
        private static T VisualUpwardSearch<T>(DependencyObject source) where T : DependencyObject
        {
            while (source != null && !(source is T))
            {
                source = VisualTreeHelper.GetParent(source);
            }
            return source as T;
        }

        private void FileListBox_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            var dep = (DependencyObject)e.OriginalSource;
            var item = VisualUpwardSearch<ListBoxItem>(dep);

            if (item != null)
            {
                item.IsSelected = true;
                e.Handled = true;

                if (item.DataContext is FileItem file)
                {
                    var cm = new ContextMenu();

                    var miShare = new MenuItem { Header = "Chia sẻ" };
                    miShare.Click += (s, args) => { ShowShareFor(file); };
                    cm.Items.Add(miShare);

                    var miDownload = new MenuItem { Header = "Tải xuống" };
                    miDownload.Click += async (s, args) =>
                    {
                        await DownloadFile(file);
                    };
                    cm.Items.Add(miDownload);

                    cm.Placement = PlacementMode.MousePoint;
                    cm.IsOpen = true;
                }
            }
        }

        private void SearchBox1_TextChanged(object sender, TextChangedEventArgs e)
        {
            SearchPlaceholder1.Visibility = string.IsNullOrWhiteSpace(SearchBox1.Text)
                ? Visibility.Visible
                : Visibility.Collapsed;
        }

        // ==================== UPLOAD ====================
        private async void BtnUpload_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Multiselect = false,
                Title = "Chọn file để tải lên"
            };

            if (openFileDialog.ShowDialog() != true) return;

            string filePath = openFileDialog.FileName;

            // Disable nút và đổi text
            btnUpload.IsEnabled = false;
            btnUpload.Content = "Đang tải lên...";

            try
            {
                string result = await FtpClientService.Instance.UploadFileAsync(filePath);

                if (result == "OK")
                {
                    MessageBox.Show("Tải lên thành công!", "Thành công",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    await LoadFilesFromServer();
                }
                else
                {
                    MessageBox.Show($"Lỗi: {result}", "Thất bại",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi upload:\n{ex.Message}", "Lỗi",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                btnUpload.IsEnabled = true;
                btnUpload.Content = "Tải lên";
            }
        }

        // ==================== DOWNLOAD ====================
        private async void BtnDownload_Click(object sender, RoutedEventArgs e)
        {
            if (FileListBox.SelectedItem is not FileItem item || item.IsFolder)
            {
                MessageBox.Show("Vui lòng chọn một file (không phải thư mục) để tải xuống.",
                    "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            await DownloadFile(item);
        }

        private async Task DownloadFile(FileItem item)
        {
            SaveFileDialog dlg = new SaveFileDialog
            {
                FileName = item.Name
            };

            if (dlg.ShowDialog() != true) return;

            try
            {
                bool ok = await FtpClientService.Instance.DownloadFileAsync(
                    item.Id, dlg.FileName, item.SizeBytes);

                if (ok)
                {
                    MessageBox.Show("Tải xuống thành công!", "Thành công",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show("Tải xuống thất bại.", "Lỗi",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi download:\n{ex.Message}", "Lỗi",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ==================== DELETE ====================
        private async void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (FileListBox.SelectedItem is not FileItem item)
            {
                MessageBox.Show("Vui lòng chọn một mục để xóa.", "Thông báo",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var result = MessageBox.Show(
                $"Bạn có chắc muốn chuyển '{item.Name}' vào thùng rác?",
                "Xác nhận xóa",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes) return;

            try
            {
                bool ok = await FtpClientService.Instance.DeleteFileAsync(item.Id);

                if (ok)
                {
                    MessageBox.Show("Đã chuyển vào thùng rác.", "Thành công",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    await LoadFilesFromServer();
                }
                else
                {
                    MessageBox.Show("Xóa thất bại.", "Lỗi",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi xóa file:\n{ex.Message}", "Lỗi",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ==================== REFRESH ====================
        private async void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            await LoadFilesFromServer();
        }
    }
}