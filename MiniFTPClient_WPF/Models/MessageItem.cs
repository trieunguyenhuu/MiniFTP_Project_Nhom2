using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace MiniFTPClient_WPF.Models
{
    public class MessageItem
    {
        public int FileId { get; set; }
        public long SizeInBytes { get; set; }  // Để sắp xếp theo dung lượng
        public bool IsRead { get; set; }       // Đánh dấu đã đọc
        public string Sender { get; set; }
        public string Time { get; set; }
        public string FileName { get; set; }
        public string Size { get; set; }
        public bool IsReceived { get; set; }
        public DateTime Date { get; set; }
        public string AvatarPath { get; set; }

        private bool _isAccepted;

        public DateTime DateOnly => Date.Date;
        public string FilePath { get; set; }
        public string Initial
        {
            get
            {
                if (string.IsNullOrWhiteSpace(Sender))
                    return "?";
                return Sender.Substring(0, 1).ToUpper();
            }
        }
        public bool IsAccepted
        {
            get => _isAccepted;
            set
            {
                _isAccepted = value;
                OnPropertyChanged(nameof(IsAccepted));
                OnPropertyChanged(nameof(ShowDownloadButton));
                OnPropertyChanged(nameof(ShowAcceptDeclineButtons));
            }
        }

        public Visibility ShowDownloadButton => IsAccepted ? Visibility.Visible : Visibility.Collapsed;
        public Visibility ShowAcceptDeclineButtons => IsAccepted ? Visibility.Collapsed : Visibility.Visible;
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

    }
}
