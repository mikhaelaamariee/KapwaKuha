// FILE: Models/NotificationModel.cs  (NEW — in-app bell UI)
using System;
using KapwaKuha.ViewModels;

namespace KapwaKuha.Models
{
    public class NotificationModel : ObservableObject
    {
        public string Notif_ID { get; set; } = string.Empty;
        public string Recipient_ID { get; set; } = string.Empty;

        // ClaimUpdate | Approval | Message | AccountAlert
        public string Notif_Type { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;

        private bool _isRead;
        public bool IsRead
        {
            get => _isRead;
            set { _isRead = value; OnPropertyChanged(); OnPropertyChanged(nameof(IsUnread)); }
        }

        public bool IsUnread => !_isRead;
        public DateTime SentAt { get; set; } = DateTime.Now;
        public string Reference_ID { get; set; } = string.Empty;

        public string SentAtDisplay => SentAt.ToString("MMM dd  HH:mm");

        public string NotifIcon => Notif_Type switch
        {
            "ClaimUpdate" => "📦",
            "Approval" => "✅",
            "Message" => "💬",
            "AccountAlert" => "⚠️",
            _ => "🔔"
        };
    }
}