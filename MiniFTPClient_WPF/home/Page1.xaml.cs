using Microsoft.Win32;
using MiniFTPClient_WPF.Models;
using MiniFTPClient_WPF.Services;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.IO;

namespace MiniFTPClient_WPF.home
{
    public partial class Page1 : Page
    {
        // Sử dụng FileItem từ Models
        public ObservableCollection<string> Breadcrumbs { get; } = new ObservableCollection<string>();
        public ObservableCollection<FileItem> Files { get; } = new ObservableCollection<FileItem>();

        // 🔹 Danh sách người dùng mẫu (UI giả lập)
        private readonly ObservableCollection<UserItem> _users = new ObservableCollection<UserItem>();

        private string _selectedFilePath = null;
        private FileItem _selectedFileItem;

        public ObservableCollection<ActivityLog> ActivityLogs { get; } = new ObservableCollection<ActivityLog>();
        public Page1()
        {
            InitializeComponent();

            this.DataContext = this;

            // Khởi tạo Breadcrumb
            Breadcrumbs.Add("Home");

            //_ = LoadFilesFromServer();

            RecipientList.ItemsSource = _users;

            UpdateShareButtonState();

            // THÊM MỚI: Log khởi động
            AddLog("Khởi động ứng dụng", "Start");
        }

        // THÊM MỚI: Hàm thêm log
        private void AddLog(string message, string level = "Info")
        {
            var log = new ActivityLog
            {
                Message = message,
                Timestamp = DateTime.Now,
                Level = level
            };

            // Thêm vào đầu danh sách (log mới nhất lên trên)
            ActivityLogs.Insert(0, log);

            // Giới hạn 50 log gần nhất để tránh quá tải
            if (ActivityLogs.Count > 50)
            {
                ActivityLogs.RemoveAt(ActivityLogs.Count - 1);
            }
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            // Mỗi khi vào trang này sẽ tự tải lại danh sách file
            _ = LoadFilesFromServer();
        }

        // --- HÀM TẢI DỮ LIỆU TỪ SERVER ---
        private async Task LoadFilesFromServer()
        {
            try
            {
                AddLog("Đang tải danh sách file...", "Info");
                Files.Clear();

                if (!FtpClientService.Instance.IsConnected)
                {
                    AddLog("Chưa kết nối tới server", "Stop");
                    return;
                }

                var svFiles = await FtpClientService.Instance.GetListingAsync();

                foreach (var f in svFiles)
                {
                    Files.Add(f);
                }

                AddLog($"Đã tải {svFiles.Count} mục", "Info");
            }
            catch (Exception ex)
            {
                AddLog($"Lỗi tải danh sách: {ex.Message}", "Stop");
                MessageBox.Show("Không thể tải danh sách file: " + ex.Message);
            }
        }

        // =========================================================
        // BREADCRUMB + FILE LIST NAVIGATION
        // =========================================================

        private void FileListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (FileListBox.SelectedItem is FileItem item)
            {
                // ✅ Nếu là folder → đi vào folder
                if (item.IsFolder)
                {
                    NavigateInto(item.Name.TrimEnd('/'));
                    FileListBox.SelectedItem = null; // reset để click lại được
                }
                else
                {
                    // ✅ Nếu là file → tự động chọn file
                    _selectedFileItem = item;               // lưu file đang chọn
                    TxtSelectedFile.Text = item.Name;       // hiển thị tên file
                    UpdateShareButtonState();               // cập nhật nút Share
                }
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

        private async void NavigateInto(string folderName)
        {
            AddLog($"Đang mở thư mục: {folderName}", "Info");

            bool ok = await FtpClientService.Instance.ChangeDirectoryAsync(folderName);

            if (ok)
            {
                Breadcrumbs.Add(folderName);
                AddLog($"Đã chuyển vào: {folderName}", "Info");
                _ = LoadFilesFromServer();
            }
            else
            {
                AddLog($"Không thể mở thư mục: {folderName}", "Stop");
                MessageBox.Show("Không thể mở thư mục này.", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void NavigateToBreadcrumb(int index)
        {
            // Logic: Breadcrumbs đang là [Home, A, B, C]. 
            // Click vào A (index 1). Cần back 2 lần (C->B, B->A).
            // Số lần back = (Tổng số phần tử - 1) - index đích

            int stepsBack = (Breadcrumbs.Count - 1) - index;
            if (stepsBack <= 0) return;

            bool allOk = true;
            for (int i = 0; i < stepsBack; i++)
            {
                // Gửi lệnh ".." để server lùi lại 1 cấp
                bool ok = await FtpClientService.Instance.ChangeDirectoryAsync("..");
                if (!ok) allOk = false;
            }

            // Cập nhật UI Breadcrumbs (xóa các phần tử phía sau)
            while (Breadcrumbs.Count - 1 > index)
            {
                Breadcrumbs.RemoveAt(Breadcrumbs.Count - 1);
            }

            // Tải lại danh sách
            _ = LoadFilesFromServer();
        }

        // =========================================================
        // USER MODEL & SAMPLE DATA (Giữ nguyên cho giao diện Share)
        // =========================================================

        public class UserItem : INotifyPropertyChanged
        {
            private bool _isSelected;

            public int UserId { get; set; }
            public string Name { get; set; } = "";
            public string AvatarPath { get; set; } = "";

            public bool IsSelected
            {
                get => _isSelected;
                set
                {
                    if (_isSelected != value)
                    {
                        _isSelected = value;
                        OnPropertyChanged(nameof(IsSelected));
                    }
                }
            }

            public event PropertyChangedEventHandler PropertyChanged;
            protected void OnPropertyChanged(string propertyName) =>
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }


        // =========================================================
        // SHARE PANEL LOGIC (Giữ nguyên)
        // =========================================================

        private async void BtnShare_Click(object sender, RoutedEventArgs e)
        {
            // 1. Kiểm tra đã chọn file chưa
            if (_selectedFileItem == null || _selectedFileItem.IsFolder)
            {
                MessageBox.Show("Vui lòng chọn một file (không phải thư mục) để chia sẻ.",
                                "Chưa chọn file",
                                MessageBoxButton.OK,
                                MessageBoxImage.Warning);
                return;
            }

            try
            {
                // 2. Lấy danh sách user (bao gồm cả user_id)
                var realUsers = await FtpClientService.Instance.GetUsersWithIdAsync();

                _users.Clear();
                foreach (var (userId, fullName) in realUsers)
                {
                    _users.Add(new UserItem
                    {
                        UserId = userId,
                        Name = fullName,
                        AvatarPath = "pack://application:,,,/MiniFTPClient_WPF;component/anh/karina.jpg",
                        IsSelected = false // ✅ Mặc định KHÔNG chọn
                    });
                }

                // 3. Cập nhật file đã chọn
                TxtSelectedFile.Text = _selectedFileItem.Name;

                // 4. Hiện panel
                Overlay.Visibility = Visibility.Visible;
                Panel.SetZIndex(Overlay, 999);
                Panel.SetZIndex(SharePanel, 1000);
                SharePanel.Visibility = Visibility.Visible;

                // 5. Reset trạng thái nút
                UpdateShareButtonState();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi tải danh sách người dùng: {ex.Message}",
                    "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }



        private void CloseSharePanel_Click(object sender, RoutedEventArgs e)
        {
            SharePanel.Visibility = Visibility.Collapsed;
            Overlay.Visibility = Visibility.Collapsed;
            Panel.SetZIndex(SharePanel, 0);
            Panel.SetZIndex(Overlay, 0);

            // Reset state
            _selectedFilePath = null;
            TxtSelectedFile.Text = "(Chưa chọn file)";
            BtnDoShare.IsEnabled = false;
            BtnDoShare.Content = "Chia sẻ";

            // ✅ BỎ CHỌN TẤT CẢ USER
            foreach (var user in _users)
            {
                user.IsSelected = false;
            }
        }


        private void Overlay_MouseDown(object sender, MouseButtonEventArgs e)
        {
            SharePanel.Visibility = Visibility.Collapsed;
            CreateFolderPanel.Visibility = Visibility.Collapsed;
            Overlay.Visibility = Visibility.Collapsed;
        }


        private void RecipientList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateShareButtonState();
        }

        private void RecipientCheckBox_Changed(object sender, RoutedEventArgs e)
        {
            UpdateShareButtonState();
        }

        private void UpdateShareButtonState()
        {
            // Đếm số user được chọn
            int selectedCount = _users.Count(u => u.IsSelected);

            bool hasFile = !string.IsNullOrWhiteSpace(TxtSelectedFile.Text)
                           && TxtSelectedFile.Text != "(Chưa chọn file)";

            BtnDoShare.IsEnabled = selectedCount > 0 && hasFile;

            // Cập nhật text nút
            if (selectedCount > 0)
            {
                BtnDoShare.Content = $"Chia sẻ ({selectedCount})";
            }
            else
            {
                BtnDoShare.Content = "Chia sẻ";
            }
        }

        private async void BtnDoShare_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var selectedUsers = _users.Where(u => u.IsSelected).ToList();

                if (selectedUsers.Count == 0)
                {
                    MessageBox.Show("Vui lòng chọn ít nhất một người nhận.", "Thông báo",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (_selectedFileItem == null)
                {
                    MessageBox.Show("Chưa chọn file để chia sẻ.", "Lỗi",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                BtnDoShare.IsEnabled = false;
                BtnDoShare.Content = "Đang chia sẻ...";

                AddLog($"Đang chia sẻ: {_selectedFileItem.Name}", "Info");

                int successCount = 0;
                int failCount = 0;

                foreach (var user in selectedUsers)
                {
                    string result = await FtpClientService.Instance.ShareFileAsync(
                        _selectedFileItem.Id,
                        user.Name,
                        "READ"
                    );

                    if (result == "OK")
                    {
                        successCount++;
                        AddLog($"Đã chia sẻ với: {user.Name}", "Info");
                    }
                    else
                    {
                        failCount++;
                        AddLog($"Chia sẻ thất bại với: {user.Name}", "Stop");
                    }
                }

                if (failCount == 0)
                {
                    AddLog($"Chia sẻ thành công '{_selectedFileItem.Name}' với {successCount} người", "Info");
                    MessageBox.Show(
                        $"Đã chia sẻ thành công '{_selectedFileItem.Name}' với {successCount} người!",
                        "Chia sẻ thành công",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information
                    );
                }
                else
                {
                    MessageBox.Show(
                        $"Kết quả:\n• Thành công: {successCount}\n• Thất bại: {failCount}",
                        "Chia sẻ hoàn tất",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning
                    );
                }

                if (Window.GetWindow(this) is MainWindow mainWindow)
                {
                    mainWindow.AddNotification("File đã được gửi", $"Bạn đã gửi {_selectedFileItem.Name}.");
                }

                CloseSharePanel_Click(sender, e);
            }
            catch (Exception ex)
            {
                AddLog($"Lỗi chia sẻ: {ex.Message}", "Stop");
                MessageBox.Show($"Lỗi chia sẻ: {ex.Message}", "Lỗi",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                BtnDoShare.IsEnabled = true;
                BtnDoShare.Content = "Chia sẻ";
            }
        }

        private void ShowShareFor(FileItem file)
        {
            if (file == null) return;

            _selectedFilePath = null; // Có thể lưu ID file nếu cần
            TxtSelectedFile.Text = file.Name;

            Overlay.Visibility = Visibility.Visible;
            Panel.SetZIndex(SharePanel, 1000);
            SharePanel.Visibility = Visibility.Visible;

            RecipientList.Focus();
            UpdateShareButtonState();
        }

        // =========================================================
        // CONTEXT MENU & SEARCH LOGIC
        // =========================================================

        // Hàm helper tìm Visual Parent (để chuột phải vào ListBoxItem hoạt động)
        private static T VisualUpwardSearch<T>(DependencyObject source) where T : DependencyObject
        {
            while (source != null && !(source is T))
            {
                source = VisualTreeHelper.GetParent(source);
            }
            return source as T;
        }

        // Hàm Right_Click mở rộng cho client (chia sẻ, tải xuống,...)
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

                    // Menu Chia sẻ - CẬP NHẬT: tải danh sách user trước khi hiển thị
                    var miShare = new MenuItem { Header = "Chia sẻ" };
                    miShare.Click += async (s, args) =>
                    {
                        // Tải danh sách người dùng từ server (đã loại bỏ người hiện tại)
                        await LoadUsersAndShowSharePanel(file);
                    };
                    cm.Items.Add(miShare);

                    // Menu Tải xuống
                    var miDownload = new MenuItem { Header = "Tải xuống" };
                    miDownload.Click += async (s, args) =>
                    {
                        // Mở hộp thoại lưu file
                        SaveFileDialog dlg = new SaveFileDialog();
                        dlg.FileName = file.Name; // Gợi ý tên file gốc

                        if (dlg.ShowDialog() == true)
                        {
                            // Gọi Service để tải
                            bool ok = await FtpClientService.Instance.DownloadFileAsync(file.Id, dlg.FileName, file.SizeBytes);

                            if (ok)
                                MessageBox.Show("Tải xuống thành công!", "Thông báo");
                            else
                                MessageBox.Show("Tải thất bại. Vui lòng kiểm tra kết nối.", "Lỗi");
                        }
                    };
                    cm.Items.Add(miDownload);

                    // Hiển thị Menu
                    cm.Placement = PlacementMode.MousePoint;
                    cm.IsOpen = true;
                }
            }
        }

        // HÀM MỚI: Tải danh sách user và hiển thị SharePanel
        // (Danh sách đã được lọc trong GetUsersAsync, không cần lọc lại ở đây)
        private async Task LoadUsersAndShowSharePanel(FileItem file)
        {
            try
            {
                // Lấy danh sách user từ server (đã loại bỏ người hiện tại trong Service)
                var realUsers = await FtpClientService.Instance.GetUsersAsync();

                _users.Clear();

                // Thêm vào UI (không cần kiểm tra lại vì đã lọc ở Service)
                foreach (var name in realUsers)
                {
                    _users.Add(new UserItem
                    {
                        Name = name,
                        AvatarPath = "pack://application:,,,/MiniFTPClient_WPF;component/anh/karina.jpg"
                    });
                }

                // Hiển thị panel chia sẻ với file đã chọn
                ShowShareFor(file);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Không thể tải danh sách người dùng: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SearchBox1_TextChanged(object sender, TextChangedEventArgs e)
        {
            SearchPlaceholder1.Visibility = string.IsNullOrWhiteSpace(SearchBox1.Text) ? Visibility.Visible : Visibility.Collapsed;
            // TODO: Bạn có thể thêm logic filter ObservableCollection<FileItem> ở đây để lọc danh sách
        }

        private async void BtnUpload_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Multiselect = false;
            openFileDialog.Title = "Chọn file để tải lên";

            if (openFileDialog.ShowDialog() == true)
            {
                string filePath = openFileDialog.FileName;
                string fileName = Path.GetFileName(filePath);

                AddLog($"Đang tải lên: {fileName}", "Info");

                string result = await FtpClientService.Instance.UploadFileAsync(filePath);

                if (result == "OK")
                {
                    AddLog($"Tải lên thành công: {fileName}", "Info");
                    MessageBox.Show("Tải lên thành công!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                    await LoadFilesFromServer();
                }
                else
                {
                    AddLog($"Tải lên thất bại: {fileName}", "Stop");
                    MessageBox.Show(result, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }


        // --- 1. SỰ KIỆN NÚT TẢI XUỐNG ---
        private async void BtnDownload_Click(object sender, RoutedEventArgs e)
        {
            if (FileListBox.SelectedItem is not FileItem item || item.IsFolder)
            {
                MessageBox.Show("Vui lòng chọn một file (không phải thư mục) để tải xuống.");
                return;
            }

            SaveFileDialog dlg = new SaveFileDialog();
            dlg.FileName = item.Name;

            if (dlg.ShowDialog() == true)
            {
                AddLog($"Đang tải xuống: {item.Name}", "Info");

                bool ok = await FtpClientService.Instance.DownloadFileAsync(item.Id, dlg.FileName, item.SizeBytes);

                if (ok)
                {
                    AddLog($"Tải xuống thành công: {item.Name}", "Info");
                    MessageBox.Show("Tải xuống thành công!", "Thông báo");
                }
                else
                {
                    AddLog($"Tải xuống thất bại: {item.Name}", "Stop");
                    MessageBox.Show("Tải thất bại. Vui lòng kiểm tra kết nối.", "Lỗi");
                }
            }
        }

        // --- 2. SỰ KIỆN NÚT XÓA ---
        private async void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (FileListBox.SelectedItem is not FileItem item)
            {
                MessageBox.Show("Vui lòng chọn một mục để xóa.");
                return;
            }

            if (MessageBox.Show($"Bạn có chắc muốn chuyển '{item.Name}' vào thùng rác?", "Xác nhận", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                AddLog($"Đang xóa: {item.Name}", "Info");

                bool ok = await FtpClientService.Instance.DeleteFileAsync(item.Id);

                if (ok)
                {
                    AddLog($"Đã chuyển vào thùng rác: {item.Name}", "Info");
                    MessageBox.Show("Đã chuyển vào thùng rác.", "Thành công");
                    await LoadFilesFromServer();
                }
                else
                {
                    AddLog($"Xóa thất bại: {item.Name}", "Stop");
                    MessageBox.Show("Xóa thất bại.", "Lỗi");
                }
            }
        }

        // --- 3. SỰ KIỆN NÚT LÀM MỚI (REFRESH) ---
        private async void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            await LoadFilesFromServer();
        }

        //Hàm gọi border tạo thư mục
        private void CreateFolder_Click(object sender, RoutedEventArgs e)
        {
            Overlay.Visibility = Visibility.Visible;
            Panel.SetZIndex(Overlay, 999);

            CreateFolderPanel.Visibility = Visibility.Visible;
            Panel.SetZIndex(CreateFolderPanel, 1000);

            TxtNewFolderName.Text = "";
            BtnCreateFolderConfirm.IsEnabled = false;
            TxtNewFolderName.Focus();
        }
        private void CloseCreateFolderPanel_Click(object sender, RoutedEventArgs e)
        {
            CreateFolderPanel.Visibility = Visibility.Collapsed;
            Overlay.Visibility = Visibility.Collapsed;

            Panel.SetZIndex(CreateFolderPanel, 0);
            Panel.SetZIndex(Overlay, 0);
        }

        private void TxtNewFolderName_TextChanged(object sender, TextChangedEventArgs e)
        {
            BtnCreateFolderConfirm.IsEnabled =
                !string.IsNullOrWhiteSpace(TxtNewFolderName.Text);
        }
        private async void ConfirmCreateFolder_Click(object sender, RoutedEventArgs e)
        {
            string folderName = TxtNewFolderName.Text.Trim();
            if (string.IsNullOrWhiteSpace(folderName)) return;

            AddLog($"Đang tạo thư mục: {folderName}", "Info");

            bool ok = await FtpClientService.Instance.CreateDirectoryAsync(folderName);

            if (ok)
            {
                AddLog($"Đã tạo thư mục: {folderName}", "Info");
                await LoadFilesFromServer();
                CloseCreateFolderPanel_Click(sender, e);
            }
            else
            {
                AddLog($"Tạo thư mục thất bại: {folderName}", "Stop");
                MessageBox.Show("Tạo thư mục thất bại.",
                    "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}