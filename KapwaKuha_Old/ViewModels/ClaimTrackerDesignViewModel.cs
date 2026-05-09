// FILE: ViewModels/ClaimTrackerDesignViewModel.cs
using System;
using System.Collections.ObjectModel;
using System.Windows.Input;
using KapwaKuha.Commands;
using KapwaKuha.Models;

namespace KapwaKuha.ViewModels          // ← was missing namespace
{
    public class ClaimTrackerDesignViewModel
    {
        public ObservableCollection<ClaimModel> Claims { get; } = new()
        {
            new ClaimModel
            {
                Claim_ID = "CL001", Item_Name = "School Bag",
                Category_Name = "School Supplies",
                Beneficiary_Name = "Ana Reyes",
                Item_ImagePath = string.Empty,
                Claim_Status = "Pending",  Handoff_Type = "Pickup",
                Claim_Date = DateTime.Now.AddDays(-2)
            },
            new ClaimModel
            {
                Claim_ID = "CL002", Item_Name = "Winter Blanket",
                Category_Name = "Clothing",
                Beneficiary_Name = "Carlo Santos",
                Item_ImagePath = string.Empty,
                Claim_Status = "Verified", Handoff_Type = "Delivery",
                Claim_Date = DateTime.Now.AddDays(-5)
            },
            new ClaimModel
            {
                Claim_ID = "CL003", Item_Name = "Vitamins Pack",
                Category_Name = "Medicine",
                Beneficiary_Name = "Ana Reyes",
                Item_ImagePath = string.Empty,
                Claim_Status = "Released", Handoff_Type = "Donation Drive",
                Claim_Date = DateTime.Now.AddDays(-8)
            },
        };

        public string StatusMessage { get; } = "3 claim(s) found.";
        public string SearchText { get; } = string.Empty;
        public string FilterCategory { get; } = "All";
        public string FilterStatus { get; } = "All";
        public bool IsBusy { get; } = false;

        // Commands (stub for designer)
        public ICommand BackCommand { get; } = new RelayCommand(_ => { });
        public ICommand RefreshCommand { get; } = new RelayCommand(_ => { });
        public ICommand ConfirmReceiptCommand { get; } = new RelayCommand(_ => { });
        public ICommand UpdateClaimStatusCommand { get; } = new RelayCommand(_ => { });
        public ICommand ReleaseItemCommand { get; } = new RelayCommand(_ => { });
    }
}