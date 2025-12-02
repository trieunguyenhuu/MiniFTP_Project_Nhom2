using Microsoft.Win32;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;

namespace MiniFTPClient_WPF.home
{
    /// <summary>
    /// Interaction logic for Page1.xaml
    /// </summary>
    /// 
    public partial class Page1 : Page
    {
        // Exposed collections for binding
        public ObservableCollection<string> Breadcrumbs { get; } = new ObservableCollection<string>();
        public ObservableCollection<FileItem> Files { get; } = new ObservableCollection<FileItem>();

        private string _selectedFilePath = null;

        public Page1()
        {
            InitializeComponent();

            // set DataContext so XAML bindings work
            this.DataContext = this;

            // init breadcrumb and file list
            Breadcrumbs.Add("Home");
            LoadFilesFor("Home");
        }
        // CLICK 1 LẦN: nếu folder -> navigate into
        private void FileListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (FileListBox.SelectedItem is FileItem item && item.IsFolder)
            {
                // folder names in our demo may include trailing '/', remove it when storing breadcrumb
                var folderName = item.Name.TrimEnd('/');
                NavigateInto(folderName);

                // Optional: Bỏ selection để có thể click lại cùng folder
                FileListBox.SelectedItem = null;
            }
        }

        // Click on breadcrumb to navigate up
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

        // Add crumb and load files
        private void NavigateInto(string folderName)
        {
            Breadcrumbs.Add(folderName);
            LoadFilesFor(folderName);
        }

        // Keep breadcrumbs up to index and reload files
        private void NavigateToBreadcrumb(int index)
        {
            while (Breadcrumbs.Count - 1 > index)
                Breadcrumbs.RemoveAt(Breadcrumbs.Count - 1);

            var current = Breadcrumbs[index];
            LoadFilesFor(current);
        }

        // Demo: load files for a "location". Replace with FTP or real data retrieval as needed.
        private void LoadFilesFor(string location)
        {
            Files.Clear();

            if (location == "Home")
            {
                Files.Add(new FileItem("Work/", true));
                Files.Add(new FileItem("Personal/", true));
                Files.Add(new FileItem("report_2024.docx", false, "145 KB"));
                Files.Add(new FileItem("presentation.pptx", false, "3.2 MB"));
            }
            else if (location == "Personal")
            {
                Files.Add(new FileItem("photos/", true));
                Files.Add(new FileItem("resume.pdf", false, "250 KB"));
            }
            else if (location == "Work")
            {
                Files.Add(new FileItem("project.zip", false, "12 MB"));
                Files.Add(new FileItem("specs.docx", false, "78 KB"));
            }
            else if (location == "photos")
            {
                // example deeper folder
                Files.Add(new FileItem("IMG_001.jpg", false, "2.1 MB"));
                Files.Add(new FileItem("IMG_002.jpg", false, "1.9 MB"));
            }
            else
            {
                // default empty
            }
        }

        // Simple FileItem type used by ListBox
        public class FileItem
        {
            public string Name { get; set; }
            public string Size { get; set; }
            public bool IsFolder { get; set; }

            public string Icon => IsFolder ? "📁" : "📄";

            public FileItem(string name, bool isFolder, string size = "")
            {
                Name = name;
                IsFolder = isFolder;
                Size = size;
            }
        }

        private void BtnShare_Click(object sender, RoutedEventArgs e)
        {
            // show overlay + panel
            Overlay.Visibility = Visibility.Visible;
            Panel.SetZIndex(Overlay, 999);          // overlay phía dưới panel
            Panel.SetZIndex(SharePanel, 1000);
            SharePanel.Visibility = Visibility.Visible;

            // focus recipient
            TxtRecipient.Focus();

            // cập nhật trạng thái nút
            UpdateShareButtonState();
        }

        private void CloseSharePanel_Click(object sender, RoutedEventArgs e)
        {
            // 1) Ẩn panel và overlay
            SharePanel.Visibility = Visibility.Collapsed;
            Overlay.Visibility = Visibility.Collapsed;

            // 2) Reset ZIndex để tránh trường hợp overlay vẫn chặn tương tác
            Panel.SetZIndex(SharePanel, 0);
            Panel.SetZIndex(Overlay, 0);

            // 3) (tuỳ bạn) reset form fields / trạng thái
            _selectedFilePath = null;
            TxtSelectedFile.Text = "(Chưa chọn file)";
            TxtRecipient.Text = string.Empty;

            BtnDoShare.IsEnabled = false;
        }

        private void Overlay_MouseDown(object sender, MouseButtonEventArgs e)
        {
            // khi click ra ngoài overlay thì đóng panel (và ẩn overlay)
            CloseSharePanel_Click(sender, null);
        }


        // Khi người nhận thay đổi nội dung -> cập nhật trạng thái nút chia sẻ
        private void TxtRecipient_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateShareButtonState();
        }

        // Bật/tắt nút Chia sẻ theo điều kiện: có file + có tên người nhận
        private void UpdateShareButtonState()
        {
            bool hasRecipient = !string.IsNullOrWhiteSpace(TxtRecipient.Text);
            BtnDoShare.IsEnabled = hasRecipient;
        }

        // Xử lý hành động chia sẻ (demo): ở đây bạn sẽ gọi logic gửi file...
        private void BtnDoShare_Click(object sender, RoutedEventArgs e)
        {
            // Lấy dữ liệu
            var filePath = _selectedFilePath;
            var recipient = TxtRecipient.Text.Trim();

            // Demo: hiển thị messagebox. Thay chỗ này bằng logic thực tế (upload, gửi mail...)
            MessageBox.Show($"Chia sẻ file:\n\nFile: {System.IO.Path.GetFileName(filePath)}\nNgười nhận: {recipient}",
                            "Chia sẻ", MessageBoxButton.OK, MessageBoxImage.Information);

            // Đóng panel sau khi chia sẻ
            CloseSharePanel_Click(sender, null);
        }


        private void ShowShareFor(FileItem file)
        {
            if (file == null) return;

            // nếu bạn có đường dẫn thực của file từ FTP hoặc local, gán vào _selectedFilePath
            // Ở demo này chỉ lưu tên file để hiển thị; nếu cần đường dẫn đầy đủ, gán _selectedFilePath tương ứng
            _selectedFilePath = null; // nếu không có path thực
            TxtSelectedFile.Text = file.Name;

            // nếu file là ảnh (theo phần mở rộng), hiển thị preview demo (dùng file bạn đã upload)
            var lower = file.Name?.ToLower() ?? "";
            bool isImage = lower.EndsWith(".png") || lower.EndsWith(".jpg") || lower.EndsWith(".jpeg") || lower.EndsWith(".gif");
            if (isImage)
            {
                // nếu bạn có Image control trong SharePanel, gán source ở đây
                // ví dụ: SharePreviewImage.Source = new BitmapImage(new Uri("file:///mnt/data/bf3c4751-8c00-4dbf-bc20-60ffb4361a21.png"));
                // Nhưng trong cấu trúc hiện tại bạn chỉ cần hiển thị tên file.
                try
                {
                    // demo: gán SelectedFilePath tới local demo image (theo dev instruction)
                    _selectedFilePath = "/mnt/data/bf3c4751-8c00-4dbf-bc20-60ffb4361a21.png";
                    // nếu bạn có Image control tên SharePreviewImage, uncomment sau:
                    // SharePreviewImage.Source = new BitmapImage(new Uri("file:///mnt/data/bf3c4751-8c00-4dbf-bc20-60ffb4361a21.png"));
                    // SharePreviewImage.Visibility = Visibility.Visible;
                }
                catch { /* ignore preview errors in demo */ }
            }
            else
            {
                // nếu không, ẩn preview (nếu có control)
                // SharePreviewImage.Visibility = Visibility.Collapsed;
            }

            // show overlay + panel (giữ logic bạn đã có)
            Overlay.Visibility = Visibility.Visible;
            Panel.SetZIndex(SharePanel, 1000);
            SharePanel.Visibility = Visibility.Visible;

            // focus vào recipient để người dùng nhập nhanh
            TxtRecipient.Focus();

        }

        private static T VisualUpwardSearch<T>(DependencyObject source) where T : DependencyObject
        {
            while (source != null && !(source is T))
            {
                source = VisualTreeHelper.GetParent(source);
            }
            return source as T;
        }

        // Bắt sự kiện nhấn chuột phải trên ListBox -> chọn item và hiện ContextMenu tại chuột
        private void FileListBox_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            var dep = (DependencyObject)e.OriginalSource;
            var item = VisualUpwardSearch<ListBoxItem>(dep);

            if (item != null)
            {
                // đảm bảo ListBox chọn đúng item dưới chuột
                item.IsSelected = true;

                // ngăn ListBox xử lý tiếp (nếu cần)
                e.Handled = true;

                // Lấy FileItem từ DataContext của item
                if (item.DataContext is FileItem file)
                {
                    // Tạo context menu động
                    var cm = new ContextMenu();

                    // Share menu item
                    var miShare = new MenuItem {
                        Header = "Chia sẻ",
                        //Style = (Style)FindResource("FooterCancelButton")
                    };
                    miShare.Click += (s, args) =>
                    {
                        // gọi mở share cho file được chọn
                        ShowShareFor(file);
                    };
                    cm.Items.Add(miShare);

                    // (Tùy chọn) thêm menu khác, ví dụ Delete, Download...
                    var miDownload = new MenuItem { Header = "Tải xuống" };
                    miDownload.Click += (s, args) =>
                    {
                       
                    };
                    cm.Items.Add(miDownload);

                    // Hiển thị context menu tại vị trí chuột
                    cm.Placement = PlacementMode.MousePoint;
                    cm.IsOpen = true;
                }
            }
        }

    }
}
