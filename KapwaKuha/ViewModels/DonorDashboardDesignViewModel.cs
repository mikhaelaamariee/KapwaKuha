// FILE: ViewModels/DonorDashboardDesignViewModel.cs
using KapwaKuha.Commands;
using KapwaKuha.Models;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace KapwaKuha.ViewModels
{
    public class DonorDashboardDesignViewModel
    {
        public string WelcomeText { get; } = "Welcome back, Juan Dela Cruz!";
        public string UserLabel { get; } = "Donor: juandc";
        public bool IsSidebarOpen { get; } = true;
        public bool HasPicture { get; } = false;
        public string ProfilePicturePath { get; } = string.Empty;
        public string TransactionStatus { get; } = "2 completed donation(s)";
        public bool IsLoadingTransactions { get; } = false;

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

        public ICommand HamburgerCommand { get; } = new RelayCommand(_ => { });
        public ICommand PostItemCommand { get; } = new RelayCommand(_ => { });
        public ICommand MyImpactCommand { get; } = new RelayCommand(_ => { });
        public ICommand HighPriorityNeedsCommand { get; } = new RelayCommand(_ => { });
        public ICommand ActiveListingsCommand { get; } = new RelayCommand(_ => { });
        public ICommand ClaimTrackerCommand { get; } = new RelayCommand(_ => { });
        public ICommand ChatCommand { get; } = new RelayCommand(_ => { });
        public ICommand LogoutCommand { get; } = new RelayCommand(_ => { });
        public ICommand MyAccountCommand { get; } = new RelayCommand(_ => { });
    }
}