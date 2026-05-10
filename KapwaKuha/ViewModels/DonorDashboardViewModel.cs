// FILE: ViewModels/DonorDashboardViewModel.cs
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using KapwaKuha.Commands;
using KapwaKuha.Models;
using KapwaKuha.Services;

namespace KapwaKuha.ViewModels
{
    // ── Lightweight chat preview row shown on the dashboard ───────────────────
    public class DashboardChatRow : ObservableObject
    {
        public string UserId { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string LastMessage { get; set; } = string.Empty;
        public int UnreadCount { get; set; }
        public bool HasUnread => UnreadCount > 0;

        // Profile picture support for chat list avatars
        private string _profilePicturePath = string.Empty;
        public string ProfilePicturePath
        {
            get => _profilePicturePath;
            set { _profilePicturePath = value; OnPropertyChanged(); OnPropertyChanged(nameof(HasProfilePic)); }
        }
        public bool HasProfilePic =>
    !string.IsNullOrEmpty(_profilePicturePath);
    }

    // ── Main runtime ViewModel ────────────────────────────────────────────────
    public class DonorDashboardViewModel : ObservableObject
    {
        private readonly string _donorId;

        // ── Sidebar ───────────────────────────────────────────────────────────
        private bool _isSidebarOpen = false;
        public bool IsSidebarOpen
        {
            get => _isSidebarOpen;
            set
            {
                _isSidebarOpen = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(MessagesColumnWidth));
            }
        }

        public GridLength MessagesColumnWidth =>
            IsSidebarOpen ? new GridLength(240) : new GridLength(300);

        // ── Identity ──────────────────────────────────────────────────────────
        public string WelcomeText { get; }
        public string UserLabel { get; }

        // ── Profile picture ───────────────────────────────────────────────────
        private string _profilePicturePath = string.Empty;
        public string ProfilePicturePath
        {
            get => _profilePicturePath;
            set { _profilePicturePath = value; OnPropertyChanged(); OnPropertyChanged(nameof(HasPicture)); }
        }
        public bool HasPicture =>
            !string.IsNullOrEmpty(_profilePicturePath) && System.IO.File.Exists(_profilePicturePath);

        // ── Impact metrics ────────────────────────────────────────────────────
        private int _totalDonated;
        public int TotalDonated
        {
            get => _totalDonated;
            set { _totalDonated = value; OnPropertyChanged(); }
        }

        private int _totalClaimed;
        public int TotalClaimed
        {
            get => _totalClaimed;
            set { _totalClaimed = value; OnPropertyChanged(); }
        }

        private int _activeItems;
        public int ActiveItems
        {
            get => _activeItems;
            set { _activeItems = value; OnPropertyChanged(); }
        }

        // ── REQUIREMENT 1: Needs Fulfilled (replaces AvgTimeToClaim) ─────────
        private int _fulfilledNeeds;
        public int FulfilledNeeds
        {
            get => _fulfilledNeeds;
            set { _fulfilledNeeds = value; OnPropertyChanged(); }
        }

        // ── My Posts ──────────────────────────────────────────────────────────
        public ObservableCollection<ItemModel> MyPosts { get; } = new();

        // ── Recent Chats ──────────────────────────────────────────────────────
        public ObservableCollection<DashboardChatRow> RecentChats { get; } = new();

        private bool _hasNoChats = true;
        public bool HasNoChats
        {
            get => _hasNoChats;
            set { _hasNoChats = value; OnPropertyChanged(); }
        }

        // ── Transaction History ───────────────────────────────────────────────
        public ObservableCollection<TransactionRow> Transactions { get; } = new();

        private bool _isLoadingTransactions;
        public bool IsLoadingTransactions
        {
            get => _isLoadingTransactions;
            set { _isLoadingTransactions = value; OnPropertyChanged(); }
        }

        private string _transactionStatus = string.Empty;
        public string TransactionStatus
        {
            get => _transactionStatus;
            set { _transactionStatus = value; OnPropertyChanged(); }
        }

        private bool _hasNoTransactions = true;
        public bool HasNoTransactions
        {
            get => _hasNoTransactions;
            set { _hasNoTransactions = value; OnPropertyChanged(); }
        }

        // ── REQUIREMENT 4: Carousel scroll hook (filled by code-behind) ───────
        /// <summary>
        /// The code-behind assigns this after InitializeComponent so the ViewModel
        /// can programmatically scroll the named ScrollViewer.
        /// Positive offset scrolls right; negative scrolls left.
        /// </summary>
        public Action<double>? CarouselScrollAction { get; set; }

        // ── Commands ──────────────────────────────────────────────────────────
        public ICommand HamburgerCommand { get; }
        public ICommand NavigateDashboardCommand { get; }   // REQUIREMENT 2: sidebar Dashboard button
        public ICommand PostItemCommand { get; }
        public ICommand MyImpactCommand { get; }
        public ICommand HighPriorityNeedsCommand { get; }
        public ICommand ActiveListingsCommand { get; }
        public ICommand ClaimTrackerCommand { get; }
        public ICommand ChatCommand { get; }
        public ICommand LogoutCommand { get; }
        public ICommand MyAccountCommand { get; }
        public ICommand ViewDescriptionCommand { get; }   // REQUIREMENT 5: pinpoint
        public ICommand EditPostCommand { get; }   // REQUIREMENT 5: pinpoint
        public ICommand OpenChatWithCommand { get; }
        public ICommand CarouselLeftCommand { get; }   // REQUIREMENT 4
        public ICommand CarouselRightCommand { get; }   // REQUIREMENT 4

        // Carousel scroll step in pixels
        private const double CarouselStep = 234.0; // card width 220 + margin 14

        // ── Constructor ───────────────────────────────────────────────────────
        public DonorDashboardViewModel(string donorId)
        {
            _donorId = donorId;
            WelcomeText = $"Welcome back, {UserSession.FullName}!";
            UserLabel = $"Donor: {UserSession.Username}";

            // ── REQUIREMENT 2: Hamburger toggles sidebar ──────────────────────
            HamburgerCommand = new RelayCommand(_ => IsSidebarOpen = !IsSidebarOpen);

            // ── REQUIREMENT 2: Dashboard sidebar button ────────────────────────
            // Re-opens the same window type (already on dashboard, so just
            // make it a no-op or re-navigate as needed). A simple navigate to self.
            NavigateDashboardCommand = new RelayCommand(_ =>
                NavigationService.Navigate(new View.DonorDashboardWindow(_donorId)));

            PostItemCommand = new RelayCommand(_ =>
                NavigationService.Navigate(new View.PostItemWindow(_donorId)));

            MyImpactCommand = new RelayCommand(_ =>
                NavigationService.Navigate(new View.MyImpactWindow(_donorId)));

            HighPriorityNeedsCommand = new RelayCommand(_ =>
                NavigationService.Navigate(new View.HighPriorityNeedsWindow(_donorId)));

            ActiveListingsCommand = new RelayCommand(_ =>
                NavigationService.Navigate(new View.ActiveListingsWindow(_donorId)));

            ClaimTrackerCommand = new RelayCommand(_ =>
                NavigationService.Navigate(new View.DonorClaimTrackerWindow(_donorId)));

            ChatCommand = new RelayCommand(_ =>
                NavigationService.Navigate(new View.ChatListWindow(_donorId, "Donor")));

            MyAccountCommand = new RelayCommand(_ =>
                NavigationService.Navigate(new View.DonorProfileWindow(_donorId)));

            LogoutCommand = new RelayCommand(_ =>
            {
                var r = MessageBox.Show("Are you sure you want to log out?", "Confirm Logout",
                    MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (r == MessageBoxResult.Yes)
                {
                    UserSession.Clear();
                    NavigationService.Navigate(new View.ChooseRoleWindow());
                }
            });

            // ── REQUIREMENT 5: Pinpoint navigation ────────────────────────────
            // Pass the clicked item's ID to ActiveListingsWindow so it can
            // pre-select and scroll to that specific row.
            ViewDescriptionCommand = new RelayCommand(param =>
            {
                var targetId = (param as ItemModel)?.Item_ID ?? string.Empty;
                NavigationService.Navigate(new View.ActiveListingsWindow(_donorId, targetId));
            });

            EditPostCommand = new RelayCommand(param =>
            {
                var targetId = (param as ItemModel)?.Item_ID ?? string.Empty;
                NavigationService.Navigate(new View.ActiveListingsWindow(_donorId, targetId));
            });

            OpenChatWithCommand = new RelayCommand(param =>
            {
                if (param is DashboardChatRow row)
                    NavigationService.Navigate(
                        new View.ChatWindow(_donorId, row.UserId, row.DisplayName, "Donor"));
            });

            // ── REQUIREMENT 4: Carousel arrow commands ─────────────────────────
            CarouselLeftCommand = new RelayCommand(_ => CarouselScrollAction?.Invoke(-CarouselStep));
            CarouselRightCommand = new RelayCommand(_ => CarouselScrollAction?.Invoke(+CarouselStep));

            LoadProfileDataAsync();
            LoadImpactMetricsAsync();
            LoadMyPostsAsync();
            LoadRecentChatsAsync();
            LoadTransactionHistoryAsync();
        }

        // ── Data loaders ──────────────────────────────────────────────────────

        private async void LoadProfileDataAsync()
        {
            try
            {
                var donor = await KapwaDataService.GetDonorById(_donorId);
                if (donor != null)
                    ProfilePicturePath = donor.ProfilePicturePath ?? string.Empty;
            }
            catch { }
        }

        private async void LoadImpactMetricsAsync()
        {
            try
            {
                // sp_GetImpactMetrics returns: TotalDonated, TotalClaimed, ActiveItems, FulfilledNeeds, ActiveBeneficiaries
                var (total, claimed, active, fulfilled, _) =
                    await KapwaDataService.GetImpactMetrics(_donorId);

                Application.Current.Dispatcher.Invoke(() =>
                {
                    TotalDonated = total;
                    TotalClaimed = claimed;
                    ActiveItems = active;
                    FulfilledNeeds = fulfilled;  // REQUIREMENT 1
                });
            }
            catch { }
        }

        private async void LoadMyPostsAsync()
        {
            try
            {
                var items = await KapwaDataService.GetItemsByDonor(_donorId);
                Application.Current.Dispatcher.Invoke(() =>
                {
                    MyPosts.Clear();
                    foreach (var item in items) MyPosts.Add(item);
                });
            }
            catch { }
        }

        private async void LoadRecentChatsAsync()
        {
            try
            {
                var allBenes = await KapwaDataService.GetAllBeneficiariesForChat();
                Application.Current.Dispatcher.Invoke(() =>
                {
                    RecentChats.Clear();
                    foreach (var b in allBenes.Take(4))
                    {
                        RecentChats.Add(new DashboardChatRow
                        {
                            UserId = b.Beneficiary_ID,
                            DisplayName = b.Beneficiary_FullName,
                            LastMessage = string.Empty,
                            UnreadCount = 0,
                            ProfilePicturePath = b.ProfilePicturePath ?? string.Empty
                        });
                    }
                    HasNoChats = !RecentChats.Any();
                });
            }
            catch { HasNoChats = true; }
        }

        private async void LoadTransactionHistoryAsync()
        {
            IsLoadingTransactions = true;
            try
            {
                var rows = await KapwaDataService.GetDonorTransactionHistory(_donorId);
                Application.Current.Dispatcher.Invoke(() =>
                {
                    Transactions.Clear();
                    foreach (var row in rows) Transactions.Add(row);
                    HasNoTransactions = !Transactions.Any();
                    TransactionStatus = Transactions.Any()
                        ? $"{Transactions.Count} completed donation(s)"
                        : "No transactions yet";
                });
            }
            catch { HasNoTransactions = true; }
            finally { IsLoadingTransactions = false; }
        }
    }
}