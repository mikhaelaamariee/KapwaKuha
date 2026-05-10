// FILE: ViewModels/BeneficiaryDashboardDesignViewModel.cs
using KapwaKuha.Commands;
using KapwaKuha.Models;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;

namespace KapwaKuha.ViewModels
{
    public class BeneficiaryDashboardDesignViewModel
    {
        public string WelcomeText { get; } = "Welcome back, Ana Reyes!";
        public string UserLabel { get; } = "Beneficiary: B001";
        public bool IsSidebarOpen { get; } = true;
        public GridLength MessagesColumnWidth { get; } = new GridLength(240);

        public string ProfilePicturePath { get; } = string.Empty;
        public bool HasPicture { get; } = false;

        // Needs Wishlist
        public bool HasNoNeedsPosts { get; } = false;
        public ObservableCollection<NeedsPostModel> MyNeedsPosts { get; } = new()
        {
            new NeedsPostModel
            {
                NeedsPost_ID = "NP001",
                Title        = "School Supplies for Grade 1",
                Org_Name     = "Barangay San Jose",
                Description  = "Notebooks, pencils, and rulers needed.",
                Urgency      = "High"
            },
            new NeedsPostModel
            {
                NeedsPost_ID = "NP002",
                Title        = "Children's Clothing",
                Org_Name     = "Caritas Manila",
                Description  = "Assorted clothing for toddlers.",
                Urgency      = "Medium"
            }
        };

        // Transaction History
        public string TransactionStatus { get; } = "1 received donation(s)";
        public bool HasNoTransactions { get; } = false;
        public bool HasNoChats { get; } = false;

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

        // DashboardChatRow defined in DonorDashboardViewModel.cs
        public ObservableCollection<DashboardChatRow> RecentChats { get; } = new()
        {
            new DashboardChatRow { UserId = "D001", DisplayName = "Juan Dela Cruz", LastMessage = "", UnreadCount = 0 },
            new DashboardChatRow { UserId = "D002", DisplayName = "Red Cross Mla",  LastMessage = "", UnreadCount = 0 }
        };

        // Commands (no-ops for design time)
        public ICommand HamburgerCommand { get; } = new RelayCommand(_ => { });
        public ICommand NavigateDashboardCommand { get; } = new RelayCommand(_ => { });
        public ICommand ClaimTrackerCommand { get; } = new RelayCommand(_ => { });
        public ICommand ClaimHistoryCommand { get; } = new RelayCommand(_ => { });
        public ICommand BrowseItemsCommand { get; } = new RelayCommand(_ => { });
        public ICommand BrowseByCategoryCommand { get; } = new RelayCommand(_ => { });
        public ICommand NeedsWishlistCommand { get; } = new RelayCommand(_ => { });
        public ICommand ChatCommand { get; } = new RelayCommand(_ => { });
        public ICommand LogoutCommand { get; } = new RelayCommand(_ => { });
        public ICommand MyBAccountCommand { get; } = new RelayCommand(_ => { });
        public ICommand EditNeedsPostCommand { get; } = new RelayCommand(_ => { });
        public ICommand EditNeedsPostsCommand { get; } = new RelayCommand(_ => { });
        public ICommand OpenChatWithCommand { get; } = new RelayCommand(_ => { });
        public ICommand CarouselLeftCommand { get; } = new RelayCommand(_ => { });
        public ICommand CarouselRightCommand { get; } = new RelayCommand(_ => { });
        public ICommand CategoryCommand { get; } = new RelayCommand(_ => { });
        public ICommand AddNeedCommand { get; } = new RelayCommand(_ => { });
    }
}