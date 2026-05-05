// FILE: ViewModels/DonorDashboardViewModel.cs
using System;
using System.Collections.ObjectModel;
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

        // --- Properties ---

        private bool _isSidebarOpen;
        public bool IsSidebarOpen
        {
            get => _isSidebarOpen;
            set { _isSidebarOpen = value; OnPropertyChanged(); }
        }

        public string WelcomeText { get; }
        public string UserLabel { get; }

        // Profile Picture Properties for the Top Bar Avatar
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

        // Transaction History Properties
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

        // --- Commands ---
        public ICommand HamburgerCommand { get; }
        public ICommand PostItemCommand { get; }
        public ICommand MyImpactCommand { get; }
        public ICommand HighPriorityNeedsCommand { get; }
        public ICommand ActiveListingsCommand { get; }
        public ICommand ClaimTrackerCommand { get; }
        public ICommand ChatCommand { get; }
        public ICommand LogoutCommand { get; }
        public ICommand MyAccountCommand { get; }

        // --- Constructor ---
        public DonorDashboardViewModel(string donorId)
        {
            _donorId = donorId;

            // Using UserSession data for instant text rendering
            WelcomeText = $"Welcome back, {UserSession.FullName}!";
            UserLabel = $"Donor: {UserSession.Username}";

            _isSidebarOpen = true; 

            // Initialize Commands
            HamburgerCommand = new RelayCommand(_ => IsSidebarOpen = !IsSidebarOpen);

            PostItemCommand = new RelayCommand(_ =>
                NavigationService.Navigate(new View.PostItemWindow(_donorId)));

            MyImpactCommand = new RelayCommand(_ =>
                NavigationService.Navigate(new View.MyImpactWindow(_donorId)));

            HighPriorityNeedsCommand = new RelayCommand(_ =>
                NavigationService.Navigate(new View.HighPriorityNeedsWindow(_donorId)));

            ActiveListingsCommand = new RelayCommand(_ =>
                NavigationService.Navigate(new View.ActiveListingsWindow(_donorId)));

            // Donor sees their items' claims
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

            // Load initial data
            LoadProfileDataAsync();
            LoadTransactionHistoryAsync();
        }

        // --- Methods ---

        private async void LoadProfileDataAsync()
        {
            try
            {
                // Fetch the donor profile to get the latest Profile Picture
                // Ensure you have a GetDonorById method in your KapwaDataService
                var donor = await KapwaDataService.GetDonorById(_donorId);
                if (donor != null)
                {
                    ProfilePicturePath = donor.ProfilePicturePath ?? string.Empty;
                }
            }
            catch { /* Ignore or log error silently for dashboard */ }
        }

        private async void LoadTransactionHistoryAsync()
        {
            IsLoadingTransactions = true;
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
            catch
            {
                TransactionStatus = "Failed to load history.";
            }
            finally
            {
                IsLoadingTransactions = false;
            }
        }
    }

    // --- Design Time ViewModel (For XAML Previewer) ---
   
}