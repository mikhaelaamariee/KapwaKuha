// FILE: ViewModels/BeneficiaryDashboardViewModel.cs
using System;
using System.Windows;
using System.Windows.Input;
using KapwaKuha.Commands;
using KapwaKuha.Models;
using KapwaKuha.Services;
using System.Collections.ObjectModel;

namespace KapwaKuha.ViewModels
{
    public class BeneficiaryDashboardViewModel : ObservableObject
    {
        private readonly string _beneficiaryId;

        private bool _isSidebarOpen;
        public bool IsSidebarOpen
        {
            get => _isSidebarOpen;
            set { _isSidebarOpen = value; OnPropertyChanged(); }
        }

        public string WelcomeText { get; }
        public string UserLabel { get; }

        // --- Profile Picture Properties for the Top Bar Avatar ---
        private string _profilePicturePath = string.Empty;
        public string ProfilePicturePath
        {
            get => _profilePicturePath;
            set
            {
                _profilePicturePath = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(HasPicture));
            }
        }

        public bool HasPicture =>
            !string.IsNullOrEmpty(_profilePicturePath) && System.IO.File.Exists(_profilePicturePath);


        private string _transactionStatus = string.Empty;
        public string TransactionStatus
        {
            get => _transactionStatus;
            set { _transactionStatus = value; OnPropertyChanged(); }
        }


        // Transaction history (received donations)
        public System.Collections.ObjectModel.ObservableCollection<TransactionRow> Transactions { get; }
            = new();


        public ICommand HamburgerCommand { get; }
        public ICommand ClaimTrackerCommand { get; }
        public ICommand BrowseItemsCommand { get; }
        public ICommand NeedsWishlistCommand { get; }
        public ICommand ChatCommand { get; }
        public ICommand LogoutCommand { get; }
        public ICommand ClaimHistoryCommand { get; }
        public ICommand EditNeedsPostsCommand { get; }

        // MUST MATCH YOUR XAML BINDING
        public ICommand MyBAccountCommand { get; }

        public BeneficiaryDashboardViewModel(string beneficiaryId)
        {
            _beneficiaryId = beneficiaryId;
            WelcomeText = $"Welcome, {UserSession.FullName}!";
            UserLabel = $"Beneficiary: {UserSession.UserId}";

            HamburgerCommand = new RelayCommand(_ => IsSidebarOpen = !IsSidebarOpen);

            // Beneficiary sees only their own claims — pass "Beneficiary" role
            ClaimTrackerCommand = new RelayCommand(_ =>
                NavigationService.Navigate(new View.ClaimTrackerWindow(_beneficiaryId, "Beneficiary")));

            BrowseItemsCommand = new RelayCommand(_ =>
                NavigationService.Navigate(new View.BrowseItemsWindow(_beneficiaryId)));

            NeedsWishlistCommand = new RelayCommand(_ =>
                NavigationService.Navigate(new View.NeedsWishlistWindow(_beneficiaryId)));

            ChatCommand = new RelayCommand(_ =>
                NavigationService.Navigate(new View.ChatListWindow(_beneficiaryId, "Beneficiary")));

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


            //  Navigates to the Profile Window, NOT the Claim Tracker
            MyBAccountCommand = new RelayCommand(_ =>
                NavigationService.Navigate(new View.BeneficiaryProfileWindow(_beneficiaryId)));

            ClaimHistoryCommand = new RelayCommand(_ =>
            NavigationService.Navigate(
            new View.BeneficiaryClaimHistoryWindow(_beneficiaryId)));


            EditNeedsPostsCommand = new RelayCommand(_ =>
            {
                // We need the org ID — load it async then navigate
                _ = System.Threading.Tasks.Task.Run(async () =>
                {
                    var bene = await KapwaDataService.GetBeneficiaryById(_beneficiaryId);
                    if (bene == null) return;
                    Application.Current.Dispatcher.Invoke(() =>
                        NavigationService.Navigate(
                            new View.EditNeedsPostUrgencyWindow(_beneficiaryId, bene.Organization_ID)));
                });
            });

            // Load the profile picture when the dashboard opens
            LoadProfileDataAsync();

            // Load embedded transaction history on dashboard open
            LoadBeneficiaryTransactionsAsync();
        }

        // Fetch the beneficiary's picture from the database
        private async void LoadProfileDataAsync()
        {
            try
            {
                var bene = await KapwaDataService.GetBeneficiaryById(_beneficiaryId);
                if (bene != null)
                {
                    ProfilePicturePath = bene.ProfilePicturePath ?? string.Empty;
                }
            }
            catch { /* Ignore error silently for dashboard */ }
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
                    TransactionStatus = Transactions.Count > 0
                        ? $"{Transactions.Count} item(s) received"
                        : "No received donations yet — keep browsing!";
                });
            }
            catch { }
        }

    }
}