using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace MiniFTPClient_WPF.Models
{
    public class NotificationItem : INotifyPropertyChanged
    {
        private bool _isRead;

        public string Title { get; set; }
        public string Message { get; set; }
        public string Time { get; set; }
        public DateTime Timestamp { get; set; }

        public bool IsRead
        {
            get => _isRead;
            set
            {
                _isRead = value;
                OnPropertyChanged(nameof(IsRead));
                OnPropertyChanged(nameof(BackgroundColor));
            }
        }

        public Brush BackgroundColor => IsRead
            ? Brushes.White
            : new SolidColorBrush(Color.FromRgb(240, 245, 255));

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
