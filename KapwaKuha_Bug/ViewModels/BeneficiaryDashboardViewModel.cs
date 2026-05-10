// FILE: ViewModels/BeneficiaryDashboardViewModel.cs
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
    // NOTE: DashboardChatRow is defined in DonorDashboardViewModel.cs

    public class BeneficiaryDashboardViewModel : ObservableObject
    {
        private readonly string _beneficiaryId;

        // ── Sidebar ───────────────────────────────────────────────────────────
        private bool _isSidebarOpen = false;
        public bool IsSidebarOpen
        {
            get => _isSidebarOpen;
            set { _isSidebarOpen = value; OnPropertyChanged(); OnPropertyChanged(nameof(MessagesColumnWidth)); }
        }
        public GridLength MessagesColumnWidth =>
            IsSidebarOpen ? new GridLength(240) : new GridLength(320);

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

        // ── UI State Flags ────────────────────────────────────────────────────
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

        private bool _hasNoChats = true;
        public bool HasNoChats
        {
            get => _hasNoChats;
            set { _hasNoChats = value; OnPropertyChanged(); }
        }

        private bool _hasNoNeedsPosts = false;
        public bool HasNoNeedsPosts
        {
            get => _hasNoNeedsPosts;
            set { _hasNoNeedsPosts = value; OnPropertyChanged(); }
        }

        // ── Collections ───────────────────────────────────────────────────────
        public ObservableCollection<TransactionRow> Transactions { get; } = new();
        public ObservableCollection<DashboardChatRow> RecentChats { get; } = new();
        public ObservableCollection<NeedsPostModel> MyNeedsPosts { get; } = new();

        // ── Carousel scroll callback (wired by code-behind) ───────────────────
        public Action<double>? CarouselScrollRequested { get; set; }

        // ── Commands ──────────────────────────────────────────────────────────
        public ICommand HamburgerCommand { get; }
        public ICommand NavigateDashboardCommand { get; }
        public ICommand BrowseItemsCommand { get; }
        public ICommand BrowseByCategoryCommand { get; }
        public ICommand NeedsWishlistCommand { get; }
        public ICommand ClaimTrackerCommand { get; }
        public ICommand ChatCommand { get; }
        public ICommand MyBAccountCommand { get; }
        public ICommand LogoutCommand { get; }
        public ICommand CategoryCommand { get; }
        public ICommand AddNeedCommand { get; }
        public ICommand EditNeedsPostsCommand { get; }
        public ICommand EditNeedsPostCommand { get; }
        public ICommand OpenChatWithCommand { get; }
        public ICommand CarouselLeftCommand { get; }
        public ICommand CarouselRightCommand { get; }

        public BeneficiaryDashboardViewModel(string beneficiaryId)
        {
            _beneficiaryId = beneficiaryId;
            WelcomeText = $"Welcome back, {UserSession.FullName}!";
            UserLabel = $"Beneficiary: {UserSession.UserId}";

            HamburgerCommand = new RelayCommand(_ => IsSidebarOpen = !IsSidebarOpen);
            NavigateDashboardCommand = new RelayCommand(_ => { });

            BrowseItemsCommand = new RelayCommand(param =>
            {
                string category = param is string s && !string.IsNullOrWhiteSpace(s) ? s : "All";
                NavigationService.Navigate(new View.BrowseItemsWindow(_beneficiaryId, category));
            });

            BrowseByCategoryCommand = new RelayCommand(param =>
            {
                string category = param is string s && !string.IsNullOrWhiteSpace(s) ? s : "All";
                NavigationService.Navigate(new View.BrowseItemsWindow(_beneficiaryId, category));
            });

            CategoryCommand = new RelayCommand(param =>
            {
                string category = param is string s && !string.IsNullOrWhiteSpace(s) ? s : "All";
                NavigationService.Navigate(new View.BrowseItemsWindow(_beneficiaryId, category));
            });

            NeedsWishlistCommand = new RelayCommand(_ =>
                NavigationService.Navigate(new View.NeedsWishlistWindow(_beneficiaryId)));

            ClaimTrackerCommand = new RelayCommand(_ =>
                NavigationService.Navigate(new View.ClaimTrackerWindow(_beneficiaryId, "Beneficiary")));

            ChatCommand = new RelayCommand(_ =>
                NavigationService.Navigate(new View.ChatListWindow(_beneficiaryId, "Beneficiary")));

            MyBAccountCommand = new RelayCommand(_ =>
                NavigationService.Navigate(new View.BeneficiaryProfileWindow(_beneficiaryId)));

            LogoutCommand = new RelayCommand(_ =>
            {
                var r = MessageBox.Show("Log out?", "Confirm",
                    MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (r == MessageBoxResult.Yes)
                {
                    UserSession.Clear();
                    NavigationService.Navigate(new View.ChooseRoleWindow());
                }
            });

            AddNeedCommand = new RelayCommand(_ =>
                NavigationService.Navigate(new View.NeedsWishlistWindow(_beneficiaryId)));

            // Sidebar "Edit My Needs" — opens edit list, no specific post pre-selected
            EditNeedsPostsCommand = new RelayCommand(_ =>
            {
                _ = System.Threading.Tasks.Task.Run(async () =>
                {
                    var bene = await KapwaDataService.GetBeneficiaryById(_beneficiaryId);
                    if (bene == null) return;
                    Application.Current.Dispatcher.Invoke(() =>
                        NavigationService.Navigate(
                            new View.EditNeedsPostUrgencyWindow(_beneficiaryId, bene.Organization_ID)));
                });
            });

            // Per-card "Edit Needs" button — passes the specific NeedsPostModel as param
            EditNeedsPostCommand = new RelayCommand(param =>
            {
                var post = param as NeedsPostModel;
                _ = System.Threading.Tasks.Task.Run(async () =>
                {
                    var bene = await KapwaDataService.GetBeneficiaryById(_beneficiaryId);
                    if (bene == null) return;
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        var win = new View.EditNeedsPostUrgencyWindow(_beneficiaryId, bene.Organization_ID);
                        // Pre-select the specific post if available
                        if (post != null && win.DataContext is EditNeedsPostUrgencyViewModel vm)
                            vm.PreSelectPost(post);
                        NavigationService.Navigate(win);
                    });
                });
            });

            OpenChatWithCommand = new RelayCommand(param =>
            {
                if (param is DashboardChatRow row)
                    NavigationService.Navigate(
                        new View.ChatWindow(_beneficiaryId, row.UserId, row.DisplayName, "Beneficiary"));
            });

            // Carousel: invoke scroll callback wired from code-behind
            CarouselLeftCommand = new RelayCommand(_ => CarouselScrollRequested?.Invoke(-250));
            CarouselRightCommand = new RelayCommand(_ => CarouselScrollRequested?.Invoke(250));

            LoadProfileDataAsync();
            LoadBeneficiaryTransactionsAsync();
            LoadNeedsPostsAsync();
            LoadRecentChatsAsync();
        }

        private async void LoadProfileDataAsync()
        {
            try
            {
                var bene = await KapwaDataService.GetBeneficiaryById(_beneficiaryId);
                if (bene != null)
                    ProfilePicturePath = bene.ProfilePicturePath ?? string.Empty;
            }
            catch { }
        }

        private async void LoadBeneficiaryTransactionsAsync()
        {
            try
            {
                var txns = await KapwaDataService.GetBeneficiaryTransactionHistory(_beneficiaryId);
                Application.Current.Dispatcher.Invoke(() =>
                {
                    Transactions.Clear();
                    foreach (var t in txns) Transactions.Add(t);
                    HasNoTransactions = !Transactions.Any();
                    TransactionStatus = Transactions.Any()
                        ? $"{Transactions.Count} item(s) received"
                        : "No received donations yet";
                });
            }
            catch { HasNoTransactions = true; }
        }

        private async void LoadNeedsPostsAsync()
        {
            try
            {
                var bene = await KapwaDataService.GetBeneficiaryById(_beneficiaryId);
                if (bene == null) return;
                var posts = await KapwaDataService.GetNeedsPostsByOrg(bene.Organization_ID);
                Application.Current.Dispatcher.Invoke(() =>
                {
                    MyNeedsPosts.Clear();
                    foreach (var p in posts) MyNeedsPosts.Add(p);
                    HasNoNeedsPosts = !MyNeedsPosts.Any();
                });
            }
            catch { HasNoNeedsPosts = true; }
        }

        private async void LoadRecentChatsAsync()
        {
            try
            {
                var donors = await KapwaDataService.GetAllDonorsForChat();
                Application.Current.Dispatcher.Invoke(() =>
                {
                    RecentChats.Clear();
                    foreach (var d in donors.Take(5))
                    {
                        RecentChats.Add(new DashboardChatRow
                        {
                            UserId = d.Donor_ID,
                            DisplayName = d.Donor_FullName,
                            LastMessage = string.Empty,
                            UnreadCount = 0,
                            ProfilePicturePath = d.ProfilePicturePath ?? string.Empty  // ← profile pic
                        });
                    }
                    HasNoChats = !RecentChats.Any();
                });
            }
            catch { HasNoChats = true; }
        }
    }
}