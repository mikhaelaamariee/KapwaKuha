// FILE: ViewModels/ChatListDesignViewModel.cs
using System.Collections.ObjectModel;
using System.Windows.Input;
using KapwaKuha.Commands;

namespace KapwaKuha.ViewModels
{
    public class ChatListDesignViewModel
    {
        public ObservableCollection<ChatUserRow> ChatUsers { get; } = new()
        {
            new ChatUserRow
            {
                UserId = "D001", DisplayName = "Juan Dela Cruz",
                SubText = "@juandc  ·  Donor",
                LastMessage = "Can I pick up tomorrow?",
                UnreadCount = 2,
                ProfilePicturePath = string.Empty
            },
            new ChatUserRow
            {
                UserId = "D002", DisplayName = "Maria Santos",
                SubText = "@mariasantos  ·  Donor",
                LastMessage = "Thanks for the donation!",
                UnreadCount = 0,
                ProfilePicturePath = string.Empty
            },
            new ChatUserRow
            {
                UserId = "B001", DisplayName = "Ana Reyes",
                SubText = "@anareyes  ·  Barangay San Jose",
                LastMessage = "Where is the pickup point?",
                UnreadCount = 1,
                ProfilePicturePath = string.Empty
            },
        };

        public string SearchText { get; } = string.Empty;

        // Commands
        public ICommand BackCommand { get; } = new RelayCommand(_ => { });
        public ICommand OpenChatCommand { get; } = new RelayCommand(_ => { });
    }
}