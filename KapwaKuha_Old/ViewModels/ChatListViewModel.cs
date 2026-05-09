// FILE: ChatListViewModel.cs
// Window: ChatListWindow.xaml
// Donor side: shows ALL registered beneficiaries with live search bar
// Beneficiary side: shows donors they have chatted with
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using KapwaKuha.Commands;
using KapwaKuha.Models;
using KapwaKuha.Services;

namespace KapwaKuha.ViewModels
{
    public class ChatListViewModel : ObservableObject
    {
        private readonly string _myId;
        private readonly string _role;

        // Full unfiltered list for search
        private System.Collections.Generic.List<ChatUserRow> _allUsers = new();

        public ObservableCollection<ChatUserRow> ChatUsers { get; } = new();

        private string _searchText = string.Empty;
        public string SearchText
        {
            get => _searchText;
            set
            {
                _searchText = value;
                OnPropertyChanged();
                ApplySearch();
            }
        }

        public ICommand BackCommand { get; }
        public ICommand OpenChatCommand { get; }

        public ChatListViewModel(string myId, string role)
        {
            _myId = myId;
            _role = role;

            BackCommand = new RelayCommand(_ =>
            {
                if (_role == "Donor")
                    NavigationService.Navigate(new View.DonorDashboardWindow(_myId));
                else
                    NavigationService.Navigate(new View.BeneficiaryDashboardWindow(_myId));
            });

            OpenChatCommand = new RelayCommand(user =>
            {
                if (user is ChatUserRow row)
                    NavigationService.Navigate(
                        new View.ChatWindow(_myId, row.UserId, row.DisplayName, _role));
            });

            LoadChats();
        }

        private async void LoadChats()
        {
            if (_role == "Donor")
            {
                // Donors see ALL registered beneficiaries (they initiate)
                var benes = await KapwaDataService.GetAllBeneficiariesForChat();
                Application.Current.Dispatcher.Invoke(() =>
                {
                    _allUsers.Clear();
                    foreach (var b in benes)
                        _allUsers.Add(new ChatUserRow
                        {
                            UserId = b.Beneficiary_ID,
                            DisplayName = b.Beneficiary_FullName,
                            SubText = $"@{b.Beneficiary_Username}  ·  {b.Organization_Name}",
                            LastMessage = "",
                            UnreadCount = 0,
                            ProfilePicturePath = b.ProfilePicturePath ?? string.Empty
                        });
                    ApplySearch();
                });
            }
            else
            {
                // Beneficiaries see only donors they have active chats with
                var donors = await KapwaDataService.GetChatDonorsForBeneficiary(_myId);

                Application.Current.Dispatcher.Invoke(() =>
                {
                    _allUsers.Clear();

                    foreach (var (userId, fullName, lastMsg, unread, picPath) in donors)
                        _allUsers.Add(new ChatUserRow
                        {
                            UserId = userId,
                            DisplayName = fullName,
                            SubText = "Donor",
                            LastMessage = lastMsg,
                            UnreadCount = unread,
                            ProfilePicturePath = picPath ?? string.Empty
                        });

                    ApplySearch();
                });
            }
        }

        private void ApplySearch()
        {
            ChatUsers.Clear();
            var q = _searchText.Trim().ToLower();
            foreach (var row in _allUsers)
            {
                if (string.IsNullOrEmpty(q) ||
                    row.DisplayName.ToLower().Contains(q) ||
                    row.SubText.ToLower().Contains(q))
                    ChatUsers.Add(row);
            }
        }
    }

    public class ChatUserRow : ObservableObject
    {
        public string UserId { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string SubText { get; set; } = string.Empty;
        public string LastMessage { get; set; } = string.Empty;

        // --- PROFILE PICTURE FIXES ---
        private string _profilePicturePath = string.Empty;
        public string ProfilePicturePath
        {
            get => _profilePicturePath;
            set
            {
                _profilePicturePath = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(HasProfilePicture));
            }
        }

        // System.IO.File.Exists removed so URL links load correctly!
        public bool HasProfilePicture => !string.IsNullOrEmpty(ProfilePicturePath);

        private int _unreadCount;
        public int UnreadCount
        {
            get => _unreadCount;
            set
            {
                _unreadCount = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(HasUnread));
            }
        }

        public Visibility HasUnread =>
            _unreadCount > 0 ? Visibility.Visible : Visibility.Collapsed;
    }
}