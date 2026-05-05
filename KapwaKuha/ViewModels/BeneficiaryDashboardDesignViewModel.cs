// FILE: ViewModels/BeneficiaryDashboardDesignViewModel.cs
using KapwaKuha.Commands;
using KapwaKuha.Models;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace KapwaKuha.ViewModels
{
    public class BeneficiaryDashboardDesignViewModel
    {
        public string WelcomeText { get; } = "Welcome, Ana Reyes!";
        public string UserLabel { get; } = "Beneficiary: B001";
        public bool IsSidebarOpen { get; } = true;

        public string ProfilePicturePath { get; } = string.Empty;
        public bool HasPicture { get; } = false;

        public string TransactionStatus { get; } = "1 received donation(s)";

        // Sample transactions for designer view
        public ObservableCollection<TransactionRow> Transactions { get; } = new()
        {
            new TransactionRow
            {
                Claim_ID          = "CL001",
                Item_Name         = "School Supplies for Grade 1 Students",
                Category_Name     = "School Supplies",
                Beneficiary_Name  = "Ana Reyes",
                Organization_Name = "Barangay San Jose",
                Claim_Status      = "Released",
                Handoff_Type      = "Pickup",
                Claim_Date        = System.DateTime.Now.AddDays(-2)
            }
        };

        // Stub commands
        public ICommand HamburgerCommand { get; } = new RelayCommand(_ => { });
        public ICommand ClaimTrackerCommand { get; } = new RelayCommand(_ => { });
        public ICommand BrowseItemsCommand { get; } = new RelayCommand(_ => { });
        public ICommand NeedsWishlistCommand { get; } = new RelayCommand(_ => { });
        public ICommand ChatCommand { get; } = new RelayCommand(_ => { });
        public ICommand LogoutCommand { get; } = new RelayCommand(_ => { });
        public ICommand MyBAccountCommand { get; } = new RelayCommand(_ => { });
        public ICommand ClaimHistoryCommand { get; } = new RelayCommand(_ => { });
        public ICommand EditNeedsPostsCommand { get; } = new RelayCommand(_ => { });
    }
}