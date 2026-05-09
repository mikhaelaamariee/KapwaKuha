// FILE: Models/ChatMessage.cs
using KapwaKuha.ViewModels;

namespace KapwaKuha.Models
{
    public class ChatMessage : ObservableObject
    {
        public int Id { get; set; }
        public string SenderId { get; set; } = string.Empty;
        public string ReceiverId { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
        public string Time { get; set; } = string.Empty;

        // Stored per-message — never shared between messages
        public string LinkedItemId { get; set; } = string.Empty;
        public string LinkedItemPath { get; set; } = string.Empty;

        // Add to ChatMessage.cs:
        private bool _isDeclined;
        public bool IsDeclined
        {
            get => _isDeclined;
            set
            {
                _isDeclined = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ShowItemPreview));
            }
        }

        // Image only shows when: it's a DirectTarget message AND not yet declined
        public bool ShowItemPreview => IsSystemDirectTarget && !IsDeclined;

        // Controls button visibility — set to false after Accept or Decline
        private bool _isActionable = true;
        public bool IsActionable
        {
            get => _isActionable;
            set
            {
                _isActionable = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsAcceptableByReceiver));
                OnPropertyChanged(nameof(ShowActionButtons));
            }
        }

        // True only when: system direct-target message + receiver is viewing + not yet acted on
        public bool IsSystemDirectTarget =>
            !string.IsNullOrEmpty(LinkedItemId) && Text.Contains("reserved for you");

        public bool IsAcceptableByReceiver =>
            IsSystemDirectTarget && !IsFromUser && IsActionable;

        // Alias used by XAML BoolToVis for the button panel
        public bool ShowActionButtons => IsAcceptableByReceiver;

        // Is this an image-only message (sent via Send Image button)
        public bool IsImageMessage => Text.StartsWith("[IMG]");
        public string ImagePath => IsImageMessage ? Text[5..] : string.Empty;

        private bool _isFromUser;
        public bool IsFromUser
        {
            get => _isFromUser;
            set
            {
                _isFromUser = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(Alignment));
                OnPropertyChanged(nameof(BubbleBackground));
                OnPropertyChanged(nameof(IsAcceptableByReceiver));
                OnPropertyChanged(nameof(ShowActionButtons));
            }
        }

        public string Alignment => IsFromUser ? "Right" : "Left";
        public string BubbleBackground => IsFromUser ? "#00B4D8" : "#FFFFFF";
        public string BubbleTextColor => IsFromUser ? "#FFFFFF" : "#03045E";
    }
}