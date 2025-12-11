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
using System.Windows.Media.Animation;

using MiniFTPClient_WPF.Models;
using MiniFTPClient_WPF.Services;

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

        public Page1()
        {
            InitializeComponent();

            this.DataContext = this;

            // Khởi tạo Breadcrumb
            Breadcrumbs.Add("Home");

            _ = LoadFilesFromServer();

            RecipientList.ItemsSource = _users;

            UpdateShareButtonState();
        }

        // --- HÀM TẢI DỮ LIỆU TỪ SERVER ---
        private async Task LoadFilesFromServer()
        {
            try
            {
                // Xóa danh sách cũ
                Files.Clear();

                // Kiểm tra kết nối trước
                if (!FtpClientService.Instance.IsConnected)
                {
                    // Nếu chưa kết nối (ví dụ chạy thẳng vào trang Home mà ko qua Login), báo lỗi nhẹ
                    // Hoặc bạn có thể để trống để tránh crash
                    return;
                }

                // Gọi Service lấy list từ Server
                var svFiles = await FtpClientService.Instance.GetListingAsync();

                // Đổ dữ liệu vào giao diện
                foreach (var f in svFiles)
                {
                    Files.Add(f);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Không thể tải danh sách file: " + ex.Message);
            }
        }

        // =========================================================
        // BREADCRUMB + FILE LIST NAVIGATION
        // =========================================================

        private void FileListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Xử lý khi click vào item
            // Nếu là Folder -> Đi vào trong (Navigate)
            if (FileListBox.SelectedItem is FileItem item && item.IsFolder)
            {
                var folderName = item.Name.TrimEnd('/');
                NavigateInto(folderName);

                // Reset selection để có thể click lại folder đó nếu muốn
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
            // Thêm vào đường dẫn
            Breadcrumbs.Add(folderName);

            // TODO: Ở các bước sau, bạn cần bổ sung hàm "ChangeDirectory" vào FtpClientService 
            // để Server thực sự chuyển thư mục. Hiện tại ta cứ gọi Refresh list.
            _ = LoadFilesFromServer();
        }

        private void NavigateToBreadcrumb(int index)
        {
            // Xóa các breadcrumb phía sau
            while (Breadcrumbs.Count - 1 > index)
                Breadcrumbs.RemoveAt(Breadcrumbs.Count - 1);

            // Reload lại list (Đúng ra là phải gửi lệnh "CD .." hoặc đường dẫn tuyệt đối)
            _ = LoadFilesFromServer();
        }

        // =========================================================
        // USER MODEL & SAMPLE DATA (Giữ nguyên cho giao diện Share)
        // =========================================================

        public class UserItem
        {
            public string Name { get; set; } = "";
            public string AvatarPath { get; set; } = "";
        }


        // =========================================================
        // SHARE PANEL LOGIC (Giữ nguyên)
        // =========================================================

        private async void BtnShare_Click(object sender, RoutedEventArgs e)
        {
            // Lấy danh sách tên user (string)
            var realUsers = await FtpClientService.Instance.GetUsersAsync();

            _users.Clear();
            // Duyệt qua từng tên và tạo UserItem
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
            bool hasFile = !string.IsNullOrWhiteSpace(TxtSelectedFile.Text) && TxtSelectedFile.Text != "(Chưa chọn file)";
            BtnDoShare.IsEnabled = hasRecipient && hasFile;
        }

        private void BtnDoShare_Click(object sender, RoutedEventArgs e)
        {
            if (RecipientList.SelectedItem is not UserItem user)
            {
                MessageBox.Show("Vui lòng chọn người nhận.");
                return;
            }

            var fileName = TxtSelectedFile.Text;
            if (string.IsNullOrWhiteSpace(fileName) || fileName == "(Chưa chọn file)")
            {
                MessageBox.Show("Vui lòng chọn file để chia sẻ.");
                return;
            }

            // Gọi logic chia sẻ thật ở đây (nếu có tính năng chia sẻ trong DB Server)
            MessageBox.Show($"Đã gửi yêu cầu chia sẻ file: {fileName}\nĐến: {user.Name}", "Chia sẻ thành công", MessageBoxButton.OK, MessageBoxImage.Information);
            CloseSharePanel_Click(sender, e);
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
                        // Tải danh sách người dùng từ server
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
        private async Task LoadUsersAndShowSharePanel(FileItem file)
        {
            try
            {
                // Lấy danh sách tên user (string) từ server
                var realUsers = await FtpClientService.Instance.GetUsersAsync();

                _users.Clear();
                // Duyệt qua từng tên và tạo UserItem
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
            // 1. Mở hộp thoại chọn file
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Multiselect = false; // Chỉ chọn 1 file
            openFileDialog.Title = "Chọn file để tải lên";

            // 2. Nếu người dùng chọn file và ấn OK
            if (openFileDialog.ShowDialog() == true)
            {
                string filePath = openFileDialog.FileName;

                // 3. Gọi Service để upload
                string result = await FtpClientService.Instance.UploadFileAsync(filePath);

                // 4. Kiểm tra kết quả
                if (result == "OK")
                {
                    MessageBox.Show("Tải lên thành công!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);

                    // 5. Tải lại danh sách để thấy file mới
                    await LoadFilesFromServer();
                }
                else
                {
                    MessageBox.Show(result, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        // --- 1. SỰ KIỆN NÚT TẢI XUỐNG ---
        private async void BtnDownload_Click(object sender, RoutedEventArgs e)
        {
            // Kiểm tra xem người dùng đã chọn file nào trong danh sách chưa
            if (FileListBox.SelectedItem is not FileItem item || item.IsFolder)
            {
                MessageBox.Show("Vui lòng chọn một file (không phải thư mục) để tải xuống.");
                return;
            }

            // Mở hộp thoại lưu file
            SaveFileDialog dlg = new SaveFileDialog();
            dlg.FileName = item.Name; // Gợi ý tên file gốc

            if (dlg.ShowDialog() == true)
            {
                // Gọi Service để tải
                bool ok = await FtpClientService.Instance.DownloadFileAsync(item.Id, dlg.FileName, item.SizeBytes);

                if (ok) MessageBox.Show("Tải xuống thành công!", "Thông báo");
                else MessageBox.Show("Tải thất bại. Vui lòng kiểm tra kết nối.", "Lỗi");
            }
        }

        // --- 2. SỰ KIỆN NÚT XÓA ---
        private async void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            // Kiểm tra xem đã chọn mục nào chưa
            if (FileListBox.SelectedItem is not FileItem item)
            {
                MessageBox.Show("Vui lòng chọn một mục để xóa.");
                return;
            }

            // Hỏi xác nhận
            if (MessageBox.Show($"Bạn có chắc muốn chuyển '{item.Name}' vào thùng rác?", "Xác nhận", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                // Gọi Service xóa
                bool ok = await FtpClientService.Instance.DeleteFileAsync(item.Id);

                if (ok)
                {
                    MessageBox.Show("Đã chuyển vào thùng rác.", "Thành công");
                    // Refresh lại danh sách để item đó biến mất
                    await LoadFilesFromServer();
                }
                else
                {
                    MessageBox.Show("Xóa thất bại.", "Lỗi");
                }
            }
        }

        // --- 3. SỰ KIỆN NÚT LÀM MỚI (REFRESH) ---
        private async void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            await LoadFilesFromServer();
        }
    }
}