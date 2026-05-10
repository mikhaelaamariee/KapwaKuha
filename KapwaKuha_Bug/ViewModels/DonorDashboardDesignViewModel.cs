// FILE: ViewModels/DonorDashboardDesignViewModel.cs
using KapwaKuha.Commands;
using KapwaKuha.Models;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace KapwaKuha.ViewModels
{
    /// <summary>Design-time data context for the Donor Dashboard.</summary>
    public class DonorDashboardDesignViewModel
    {
        public string WelcomeText { get; } = "Welcome back, Juan Dela Cruz!";
        public string UserLabel { get; } = "Donor: juandc";
        public bool IsSidebarOpen { get; } = true;
        public bool HasPicture { get; } = false;
        public string ProfilePicturePath { get; } = string.Empty;

        // ── Impact metrics ────────────────────────────────────────────────────
        public int TotalDonated { get; } = 3;
        public int TotalClaimed { get; } = 0;
        public int ActiveItems { get; } = 3;
        public int FulfilledNeeds { get; } = 0;  // REQUIREMENT 1

        // ── Transaction history ───────────────────────────────────────────────
        public string TransactionStatus { get; } = "2 completed donation(s)";
        public bool IsLoadingTransactions { get; } = false;
        public bool HasNoTransactions { get; } = false;

        public ObservableCollection<TransactionRow> Transactions { get; } = new()
        {
            new TransactionRow
            {
                Claim_ID          = "CL001",
                Item_Name         = "School Bag",
                Category_Name     = "School Supplies",
                Beneficiary_Name  = "Ana Reyes",
                Organization_Name = "Barangay San Jose",
                Claim_Status      = "Released",
                Handoff_Type      = "Pickup",
                Claim_Date        = System.DateTime.Now.AddDays(-3)
            },
            new TransactionRow
            {
                Claim_ID          = "CL002",
                Item_Name         = "Children's Clothing Set",
                Category_Name     = "Clothing",
                Beneficiary_Name  = "Carlo Santos",
                Organization_Name = "SISC Student Welfare",
                Claim_Status      = "Released",
                Handoff_Type      = "Delivery",
                Claim_Date        = System.DateTime.Now.AddDays(-1)
            }
        };

        // ── My Posts (design samples) ─────────────────────────────────────────
        public bool HasNoChats { get; } = false;

        public ObservableCollection<DashboardChatRow> RecentChats { get; } = new()
        {
            new DashboardChatRow { UserId = "B001", DisplayName = "Ana Reyes",    LastMessage = "",  UnreadCount = 0 },
            new DashboardChatRow { UserId = "B002", DisplayName = "Carlo Santos", LastMessage = "",  UnreadCount = 0 },
        };

        // ── Commands (no-ops for design time) ─────────────────────────────────
        public ICommand HamburgerCommand { get; } = new RelayCommand(_ => { });
        public ICommand NavigateDashboardCommand { get; } = new RelayCommand(_ => { }); // REQUIREMENT 2
        public ICommand PostItemCommand { get; } = new RelayCommand(_ => { });
        public ICommand MyImpactCommand { get; } = new RelayCommand(_ => { });
        public ICommand HighPriorityNeedsCommand { get; } = new RelayCommand(_ => { });
        public ICommand ActiveListingsCommand { get; } = new RelayCommand(_ => { });
        public ICommand ClaimTrackerCommand { get; } = new RelayCommand(_ => { });
        public ICommand ChatCommand { get; } = new RelayCommand(_ => { });
        public ICommand LogoutCommand { get; } = new RelayCommand(_ => { });
        public ICommand MyAccountCommand { get; } = new RelayCommand(_ => { });
        public ICommand ViewDescriptionCommand { get; } = new RelayCommand(_ => { }); // REQUIREMENT 5
        public ICommand EditPostCommand { get; } = new RelayCommand(_ => { }); // REQUIREMENT 5
        public ICommand OpenChatWithCommand { get; } = new RelayCommand(_ => { });
        public ICommand CarouselLeftCommand { get; } = new RelayCommand(_ => { }); // REQUIREMENT 4
        public ICommand CarouselRightCommand { get; } = new RelayCommand(_ => { }); // REQUIREMENT 4
    }
}