using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MiniFTPServer_WPF.Models
{
    internal class FileMeta
    {
        public int Id { get; set; }
        public int OwnerId { get; set; }
        public int? ParentId { get; set; } // Có thể null (thư mục gốc)
        public string Name { get; set; }
        public bool IsFolder { get; set; }
        public long Size { get; set; }
        public string StoragePath { get; set; } // Tên file vật lý
        public DateTime CreatedAt { get; set; }
        public int IsDeleted { get; set; }

    }
}
