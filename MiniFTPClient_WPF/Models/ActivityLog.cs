using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;

namespace MiniFTPClient_WPF.Models
{
    public class ActivityLog : INotifyPropertyChanged
    {
        public string Message { get; set; }
        public DateTime Timestamp { get; set; }
        public string Level { get; set; } // "Start", "Info", "Stop"

        public string DisplayMessage => $"[{Timestamp:HH:mm:ss}] {Message}";

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
