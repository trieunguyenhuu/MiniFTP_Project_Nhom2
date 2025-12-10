using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MiniFTPClient_WPF.Models
{
    public class FileItem
    {
        public int Id { get; set; }          // ID từ Database Server
        public string Name { get; set; }     // Tên file
        public long SizeBytes { get; set; }  // Kích thước gốc (byte)
        public string Size { get; set; }     // Kích thước hiển thị (KB, MB)
        public bool IsFolder { get; set; }   // Là thư mục?

        // Icon hiển thị trên giao diện
        public string Icon => IsFolder ? "📁" : "📄";

        public FileItem(int id, string name, bool isFolder, long sizeBytes)
        {
            Id = id;
            Name = name;
            IsFolder = isFolder;
            SizeBytes = sizeBytes;
            Size = FormatSize(sizeBytes);
        }

        // Hàm đổi byte sang KB, MB cho đẹp
        private string FormatSize(long bytes)
        {
            if (IsFolder) return "";
            string[] suffixes = { "B", "KB", "MB", "GB" };
            int counter = 0;
            double number = bytes;
            while (Math.Round(number / 1024) >= 1)
            {
                number /= 1024;
                counter++;
            }
            return string.Format("{0:n1} {1}", number, suffixes[counter]);
        }
    }
}