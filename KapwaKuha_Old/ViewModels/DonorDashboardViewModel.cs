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
    public class DonorDashboardViewModel : ObservableObject
    {
        private readonly string _donorId;

        private bool _isSidebarOpen = true;
        public bool IsSidebarOpen
        {
            get => _isSidebarOpen;
            set { _isSidebarOpen = value; OnPropertyChanged(); }
        }

        public string WelcomeText { get; }
        public string UserLabel { get; }

        private string _profilePicturePath = string.Empty;
        public string ProfilePicturePath
        {
            get => _profilePicturePath;
            set { _profilePicturePath = value; OnPropertyChanged(); OnPropertyChanged(nameof(HasPicture)); }
        }
        public bool HasPicture =>
            !string.IsNullOrEmpty(_profilePicturePath) && System.IO.File.Exists(_profilePicturePath);

        private int _totalDonated;
        public int TotalDonated
        { get => _totalDonated; set { _totalDonated = value; OnPropertyChanged(); } }

        private int _successfullyClaimed;
        public int SuccessfullyClaimed
        { get => _successfullyClaimed; set { _successfullyClaimed = value; OnPropertyChanged(); } }

        private int _stillAvailable;
        public int StillAvailable
        { get => _stillAvailable; set { _stillAvailable = value; OnPropertyChanged(); } }

        private string _avgTimeToClaim = "0.0 days avg. to claim";
        public string AvgTimeToClaim
        { get => _avgTimeToClaim; set { _avgTimeToClaim = value; OnPropertyChanged(); } }

        public ObservableCollection<ItemModel> MyPosts { get; } = new();
        public ObservableCollection<TransactionRow> Transactions { get; } = new();

        private string _transactionStatus = string.Empty;
        public string TransactionStatus
        { get => _transactionStatus; set { _transactionStatus = value; OnPropertyChanged(); } }

        public ICommand HamburgerCommand { get; }
        public ICommand PostItemCommand { get; }
        public ICommand MyImpactCommand { get; }
        public ICommand HighPriorityNeedsCommand { get; }
        public ICommand ActiveListingsCommand { get; }
        public ICommand ClaimTrackerCommand { get; }
        public ICommand ChatCommand { get; }
        public ICommand LogoutCommand { get; }
        public ICommand MyAccountCommand { get; }
      

        public DonorDashboardViewModel(string donorId)
        {
            _donorId = donorId;
            WelcomeText = $"Welcome back, {UserSession.FullName}!";
            UserLabel = $"Donor: {UserSession.Username}";

            HamburgerCommand = new RelayCommand(_ => IsSidebarOpen = !IsSidebarOpen);
            PostItemCommand = new RelayCommand(_ => NavigationService.Navigate(new View.PostItemWindow(_donorId)));
            MyImpactCommand = new RelayCommand(_ => NavigationService.Navigate(new View.MyImpactWindow(_donorId)));
            HighPriorityNeedsCommand = new RelayCommand(_ => NavigationService.Navigate(new View.HighPriorityNeedsWindow(_donorId)));
            ActiveListingsCommand = new RelayCommand(_ => NavigationService.Navigate(new View.ActiveListingsWindow(_donorId)));
            ClaimTrackerCommand = new RelayCommand(_ => NavigationService.Navigate(new View.DonorClaimTrackerWindow(_donorId)));
            ChatCommand = new RelayCommand(_ => NavigationService.Navigate(new View.ChatListWindow(_donorId, "Donor")));
            MyAccountCommand = new RelayCommand(_ => NavigationService.Navigate(new View.DonorProfileWindow(_donorId)));

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

         
            LoadProfileDataAsync();
            LoadImpactMetricsAsync();
            LoadMyPostsAsync();
            LoadTransactionHistoryAsync();
        }

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
                var (total, claimed, active, _, _) = await KapwaDataService.GetImpactMetrics(_donorId);
                TotalDonated = total;
                SuccessfullyClaimed = claimed;
                StillAvailable = active;

                var txns = await KapwaDataService.GetDonorTransactionHistory(_donorId);
                AvgTimeToClaim = txns.Count > 0
                    ? $"{txns.Average(t => t.DaysToRelease):F1} days avg. to claim"
                    : "0.0 days avg. to claim";
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
                    foreach (var i in items) MyPosts.Add(i);
                });
            }
            catch { }
        }

        private async void LoadTransactionHistoryAsync()
        {
            try
            {
                var txns = await KapwaDataService.GetDonorTransactionHistory(_donorId);
                Application.Current.Dispatcher.Invoke(() =>
                {
                    Transactions.Clear();
                    foreach (var t in txns) Transactions.Add(t);
                    TransactionStatus = Transactions.Count > 0
                        ? $"{Transactions.Count} completed donation(s)"
                        : "No completed donations yet.";
                });
            }
            catch { TransactionStatus = "Failed to load history."; }
        }
    }

    //public class DonorDashboardDesignViewModel
    //{
    //    public bool IsSidebarOpen => true;
    //    public string WelcomeText => "Welcome back, Juan Dela Cruz!";
    //    public string UserLabel => "Donor: juandc";
    //    public string ProfilePicturePath => string.Empty;
    //    public bool HasPicture => false;
    //    public int TotalDonated => 2;
    //    public int SuccessfullyClaimed => 0;
    //    public int StillAvailable => 2;
    //    public string AvgTimeToClaim => "0.0 days avg. to claim";
    //    public string TransactionStatus => "No completed donations yet.";
    //    public ObservableCollection<ItemModel> MyPosts { get; } = new();
    //    public ObservableCollection<TransactionRow> Transactions { get; } = new();
    //}
}