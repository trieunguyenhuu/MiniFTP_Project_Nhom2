using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MiniFTPClient_WPF.Models
{
    public class SharedFileItem
    {
        public int FileId { get; set; }
        public string FileName { get; set; }
        public long FileSize { get; set; }
        public string AccessLevel { get; set; }
        public string OwnerName { get; set; }

        public string FormattedSize => FormatFileSize(FileSize);

        private string FormatFileSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB" };
            double len = bytes;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len /= 1024;
            }
            return $"{len:0.##} {sizes[order]}";
        }
    }
}
