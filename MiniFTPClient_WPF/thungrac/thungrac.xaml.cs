using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Collections.ObjectModel;
using System.Windows.Media;

namespace MiniFTPClient_WPF.thungrac
{

    public class TrashItem
    {
        public bool IsSelected { get; set; }       // cho cột CheckBox
        public string FileName { get; set; } = string.Empty;      // Tên file
        public string OriginalPath { get; set; } = string.Empty;  // Đường dẫn gốc
        public DateTime DeletedDate { get; set; } = DateTime.Now; // Ngày xóa
        public string Size { get; set; } = string.Empty;          // Kích thước
    }

    public partial class Thungrac : Page
    {
        private ObservableCollection<TrashItem> _items =
            new ObservableCollection<TrashItem>();

        public Thungrac()
        {
            InitializeComponent();
            LoadSampleData();
        }

        private void LoadSampleData()
        {
            _items = new ObservableCollection<TrashItem>
            {
                new TrashItem
                {
                    FileName = "DuAn2024",
                    OriginalPath = @"C:\Documents\Projects",
                    DeletedDate = DateTime.Now.AddDays(-1),
                    Size = "245 MB"
                },
                new TrashItem
                {
                    FileName = "BaoCaoThang10.docx",
                    OriginalPath = @"C:\Documents",
                    DeletedDate = DateTime.Now.AddDays(-2),
                    Size = "2.4 MB"
                },
                new TrashItem
                {
                    FileName = "AnhDuLich",
                    OriginalPath = @"D:\Pictures",
                    DeletedDate = DateTime.Now.AddDays(-3),
                    Size = "1.2 GB"
                },
                new TrashItem
                {
                    FileName = "presentation.pdf",
                    OriginalPath = @"C:\Work",
                    DeletedDate = DateTime.Now.AddDays(-4),
                    Size = "5.8 MB"
                },
                new TrashItem
                {
                    FileName = "video.mp4",
                    OriginalPath = @"D:\Videos",
                    DeletedDate = DateTime.Now.AddDays(-5),
                    Size = "850 MB"
                },

                new TrashItem
                {
                    FileName = "DuAn2024",
                    OriginalPath = @"C:\Documents\Projects",
                    DeletedDate = DateTime.Now.AddDays(-1),
                    Size = "245 MB"
                },
                new TrashItem
                {
                    FileName = "BaoCaoThang10.docx",
                    OriginalPath = @"C:\Documents",
                    DeletedDate = DateTime.Now.AddDays(-2),
                    Size = "2.4 MB"
                },
                new TrashItem
                {
                    FileName = "AnhDuLich",
                    OriginalPath = @"D:\Pictures",
                    DeletedDate = DateTime.Now.AddDays(-3),
                    Size = "1.2 GB"
                },
                new TrashItem
                {
                    FileName = "presentation.pdf",
                    OriginalPath = @"C:\Work",
                    DeletedDate = DateTime.Now.AddDays(-4),
                    Size = "5.8 MB"
                },
                new TrashItem
                {
                    FileName = "video.mp4",
                    OriginalPath = @"D:\Videos",
                    DeletedDate = DateTime.Now.AddDays(-5),
                    Size = "850 MB"
                },

                new TrashItem
                {
                    FileName = "DuAn2024",
                    OriginalPath = @"C:\Documents\Projects",
                    DeletedDate = DateTime.Now.AddDays(-1),
                    Size = "245 MB"
                },
                new TrashItem
                {
                    FileName = "BaoCaoThang10.docx",
                    OriginalPath = @"C:\Documents",
                    DeletedDate = DateTime.Now.AddDays(-2),
                    Size = "2.4 MB"
                },
                new TrashItem
                {
                    FileName = "AnhDuLich",
                    OriginalPath = @"D:\Pictures",
                    DeletedDate = DateTime.Now.AddDays(-3),
                    Size = "1.2 GB"
                },
                new TrashItem
                {
                    FileName = "presentation.pdf",
                    OriginalPath = @"C:\Work",
                    DeletedDate = DateTime.Now.AddDays(-4),
                    Size = "5.8 MB"
                },
                new TrashItem
                {
                    FileName = "video.mp4",
                    OriginalPath = @"D:\Videos",
                    DeletedDate = DateTime.Now.AddDays(-5),
                    Size = "850 MB"
                },
                new TrashItem
                {
                    FileName = "presentation.pdf",
                    OriginalPath = @"C:\Work",
                    DeletedDate = DateTime.Now.AddDays(-4),
                    Size = "5.8 MB"
                },
                new TrashItem
                {
                    FileName = "video.mp4",
                    OriginalPath = @"D:\Videos",
                    DeletedDate = DateTime.Now.AddDays(-5),
                    Size = "850 MB"
                },

                new TrashItem
                {
                    FileName = "DuAn2024",
                    OriginalPath = @"C:\Documents\Projects",
                    DeletedDate = DateTime.Now.AddDays(-1),
                    Size = "245 MB"
                },
                new TrashItem
                {
                    FileName = "BaoCaoThang10.docx",
                    OriginalPath = @"C:\Documents",
                    DeletedDate = DateTime.Now.AddDays(-2),
                    Size = "2.4 MB"
                },
                new TrashItem
                {
                    FileName = "AnhDuLich",
                    OriginalPath = @"D:\Pictures",
                    DeletedDate = DateTime.Now.AddDays(-3),
                    Size = "1.2 GB"
                },
                new TrashItem
                {
                    FileName = "presentation.pdf",
                    OriginalPath = @"C:\Work",
                    DeletedDate = DateTime.Now.AddDays(-4),
                    Size = "5.8 MB"
                },
                new TrashItem
                {
                    FileName = "video.mp4",
                    OriginalPath = @"D:\Videos",
                    DeletedDate = DateTime.Now.AddDays(-5),
                    Size = "850 MB"
                },

                new TrashItem
                {
                    FileName = "DuAn2024",
                    OriginalPath = @"C:\Documents\Projects",
                    DeletedDate = DateTime.Now.AddDays(-1),
                    Size = "245 MB"
                },
                new TrashItem
                {
                    FileName = "BaoCaoThang10.docx",
                    OriginalPath = @"C:\Documents",
                    DeletedDate = DateTime.Now.AddDays(-2),
                    Size = "2.4 MB"
                },
                new TrashItem
                {
                    FileName = "AnhDuLich",
                    OriginalPath = @"D:\Pictures",
                    DeletedDate = DateTime.Now.AddDays(-3),
                    Size = "1.2 GB"
                },
                new TrashItem
                {
                    FileName = "presentation.pdf",
                    OriginalPath = @"C:\Work",
                    DeletedDate = DateTime.Now.AddDays(-4),
                    Size = "5.8 MB"
                },
                new TrashItem
                {
                    FileName = "video.mp4",
                    OriginalPath = @"D:\Videos",
                    DeletedDate = DateTime.Now.AddDays(-5),
                    Size = "850 MB"
                }

            };

            // Gán data vào DataGrid
            filedatagrid.ItemsSource = _items;
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            SearchPlaceholder.Visibility =
                string.IsNullOrWhiteSpace(SearchBox.Text)
                    ? Visibility.Visible
                    : Visibility.Collapsed;
        }
    }
}
