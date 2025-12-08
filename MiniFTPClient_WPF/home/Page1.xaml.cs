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
    public partial class Page1 : Page
    {
        // Exposed collections for binding
        public ObservableCollection<string> Breadcrumbs { get; } = new ObservableCollection<string>();
        public ObservableCollection<FileItem> Files { get; } = new ObservableCollection<FileItem>();

        // 🔹 Danh sách người dùng mẫu
        private readonly ObservableCollection<UserItem> _users = new ObservableCollection<UserItem>();

        private string _selectedFilePath = null;

        public Page1()
        {
            InitializeComponent();

            this.DataContext = this;

            Breadcrumbs.Add("Home");
            LoadFilesFor("Home");

            // 🔹 Khởi tạo list người dùng & bind vào RecipientList
            InitSampleUsers();
            RecipientList.ItemsSource = _users;

            UpdateShareButtonState();
        }


        // =========================================================
        // BREADCRUMB + FILE LIST
        // =========================================================

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
            LoadFilesFor(folderName);
        }

        private void NavigateToBreadcrumb(int index)
        {
            while (Breadcrumbs.Count - 1 > index)
                Breadcrumbs.RemoveAt(Breadcrumbs.Count - 1);

            var current = Breadcrumbs[index];
            LoadFilesFor(current);
        }

        private void LoadFilesFor(string location)
        {
            Files.Clear();

            if (location == "Home")
            {
                Files.Add(new FileItem("Work/", true));
                Files.Add(new FileItem("Personal/", true));

                Files.Add(new FileItem("Documents/", true));
                Files.Add(new FileItem("Downloads/", true));
                Files.Add(new FileItem("Music/", true));
                Files.Add(new FileItem("Videos/", true));
                Files.Add(new FileItem("Pictures/", true));
                Files.Add(new FileItem("Projects/", true));
                Files.Add(new FileItem("Archive/", true));
                Files.Add(new FileItem("Backup/", true));
                Files.Add(new FileItem("Temp/", true));
                Files.Add(new FileItem("Shared/", true));

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

        // 🔹 Model người dùng
        public class UserItem
        {
            public string Name { get; set; } = "";

            public string AvatarPath { get; set; } = "";

        }

        // 🔹 Tạo dữ liệu người dùng mẫu
        private void InitSampleUsers()
        {
            _users.Add(new UserItem
            {
                Name = "Kiều Dung",
                AvatarPath = "pack://application:,,,/MiniFTPClient_WPF;component/anh/karina.jpg"
            });
            _users.Add(new UserItem
            {
                Name = "Sly 🐰",
                AvatarPath = "pack://application:,,,/MiniFTPClient_WPF;component/anh/karina.jpg"
            });
            _users.Add(new UserItem
            {
                Name = "Mai Kiều Trang",
                AvatarPath = "pack://application:,,,/MiniFTPClient_WPF;component/anh/karina.jpg"
            });
            _users.Add(new UserItem
            {
                Name = "thuận ngu",
                AvatarPath = "pack://application:,,,/MiniFTPClient_WPF;component/anh/karina.jpg"
            });
            _users.Add(new UserItem
            {
                Name = "Hà Thủy",
                AvatarPath = "/anh/karina.jpg"
            });

            // ... thêm mấy user khác nếu muốn, có thể dùng chung ảnh
        }


        // =========================================================
        // SHARE PANEL
        // =========================================================

        private void BtnShare_Click(object sender, RoutedEventArgs e)
        {
            Overlay.Visibility = Visibility.Visible;
            Panel.SetZIndex(Overlay, 999);
            Panel.SetZIndex(SharePanel, 1000);
            SharePanel.Visibility = Visibility.Visible;

            // focus list người nhận
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

        // 🔹 Khi chọn người nhận trong ListBox
        private void RecipientList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateShareButtonState();
        }

        // 🔹 Bật/tắt nút Chia sẻ: cần có file + có người nhận
        private void UpdateShareButtonState()
        {
            bool hasRecipient = RecipientList.SelectedItem != null;
            bool hasFile = !string.IsNullOrWhiteSpace(TxtSelectedFile.Text)
                           && TxtSelectedFile.Text != "(Chưa chọn file)";
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

            MessageBox.Show(
                $"Chia sẻ file:\n\nFile: {fileName}\nNgười nhận: {user.Name}",
                "Chia sẻ",
                MessageBoxButton.OK,
                MessageBoxImage.Information);

            CloseSharePanel_Click(sender, e);
        }

        private void ShowShareFor(FileItem file)
        {
            if (file == null) return;

            _selectedFilePath = null;
            TxtSelectedFile.Text = file.Name;

            var lower = file.Name?.ToLower() ?? "";
            bool isImage = lower.EndsWith(".png") || lower.EndsWith(".jpg") ||
                           lower.EndsWith(".jpeg") || lower.EndsWith(".gif");

            if (isImage)
            {
                try
                {
                    _selectedFilePath = "/mnt/data/bf3c4751-8c00-4dbf-bc20-60ffb4361a21.png";
                }
                catch { }
            }

            Overlay.Visibility = Visibility.Visible;
            Panel.SetZIndex(SharePanel, 1000);
            SharePanel.Visibility = Visibility.Visible;

            RecipientList.Focus();
            UpdateShareButtonState();
        }

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
                    miShare.Click += (s, args) =>
                    {
                        ShowShareFor(file);
                    };
                    cm.Items.Add(miShare);

                    var miDownload = new MenuItem { Header = "Tải xuống" };
                    miDownload.Click += (s, args) =>
                    {
                        // TODO: xử lý tải xuống
                    };
                    cm.Items.Add(miDownload);

                    cm.Placement = PlacementMode.MousePoint;
                    cm.IsOpen = true;
                }
            }
        }

        private void SearchBox1_TextChanged(object sender, TextChangedEventArgs e)
        {
            SearchPlaceholder1.Visibility =
                string.IsNullOrWhiteSpace(SearchBox1.Text)
                    ? Visibility.Visible
                    : Visibility.Collapsed;
        }
    }
}
