// FILE: ViewModels/ClaimProcessDesignViewModel.cs
using System;
using System.Collections.ObjectModel;
using System.Windows.Input;
using KapwaKuha.Commands;
using KapwaKuha.Models;

namespace KapwaKuha.ViewModels          // ← was missing namespace
{
    public class ClaimProcessDesignViewModel : ObservableObject
    {
        public string AdminLabel { get; } = "Agent: A001";
        public string StatusMessage { get; } = "3 items awaiting claim.";
        public bool IsItemSelected { get; } = true;
        public bool IsBusy { get; } = false;
        public string StorageDaysDisplay { get; } = "5 days in storage";

        public ICommand BackCommand { get; } = new RelayCommand(_ => { });
        public ICommand RefreshCommand { get; } = new RelayCommand(_ => { });
        public ICommand ProcessClaimCommand { get; } = new RelayCommand(_ => { });

        public ItemModel SelectedItem { get; } = new ItemModel
        {
            Item_ID = "ITEM001",
            Item_Name = "Assorted Clothing",
            Item_Status = "Available",
            Date_Found = DateTime.Now.AddDays(-5)
        };

        public ObservableCollection<ItemModel> FoundItems { get; } = new()
        {
            new ItemModel { Item_ID="ITEM001", Item_Name="Assorted Clothing",
                Item_Status="Available", Category_Name="Clothing",
                Donor_Name="Juan DC", Date_Found=DateTime.Now.AddDays(-5) },
            new ItemModel { Item_ID="ITEM003", Item_Name="Casio Calculator",
                Item_Status="Available", Category_Name="Electronics",
                Donor_Name="Maria S", Date_Found=DateTime.Now.AddDays(-1) },
            new ItemModel { Item_ID="ITEM005", Item_Name="School Supplies Box",
                Item_Status="Available", Category_Name="School Supplies",
                Donor_Name="Roberto R", Date_Found=DateTime.Now.AddDays(-8) },
        };

        public ObservableCollection<BeneficiaryRow> Beneficiaries { get; } = new()
        {
            new BeneficiaryRow { Id="B001", DisplayName="Ana Reyes — Barangay San Jose" },
            new BeneficiaryRow { Id="B002", DisplayName="Carlo Santos — SISC Welfare" },
        };

        public string VerificationNotes { get; set; } = string.Empty;
        public BeneficiaryRow? SelectedBeneficiary { get; set; }
    }
}