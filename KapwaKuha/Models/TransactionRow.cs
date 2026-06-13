// FILE: Models/TransactionRow.cs
using System;
using KapwaKuha.ViewModels;

namespace KapwaKuha.Models
{
    public class TransactionRow : ObservableObject
    {
        public string Claim_ID { get; set; } = string.Empty;
        public string Item_ID { get; set; } = string.Empty;
        public string Item_Name { get; set; } = string.Empty;
        public string Item_ImagePath { get; set; } = string.Empty;
        public string Item_Description { get; set; } = string.Empty;
        public string Category_Name { get; set; } = string.Empty;
        public string Item_Condition { get; set; } = string.Empty;
        public string Beneficiary_Name { get; set; } = string.Empty;
        public string Organization_Name { get; set; } = string.Empty;
        public DateTime Claim_Date { get; set; } = DateTime.Now;
        public string Claim_Status { get; set; } = string.Empty;
        public string Handoff_Type { get; set; } = string.Empty;
        public int DaysToRelease { get; set; }
        public string Donor_FullName { get; set; } = string.Empty;

        public string ClaimDateDisplay => Claim_Date.ToString("MMM dd, yyyy  HH:mm");

        // Used by DonorProfileWindow / donor-side UserProfile
        public bool HasImage =>
            !string.IsNullOrEmpty(Item_ImagePath) &&
            System.IO.File.Exists(Item_ImagePath);

        // Used by UserProfileWindow bene "Items Received" list (XAML binds HasItem)
        public bool HasItem => HasImage;
    }
}