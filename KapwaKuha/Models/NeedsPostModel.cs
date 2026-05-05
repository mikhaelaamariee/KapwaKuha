// FILE: NeedsPostModel.cs
// DB Table: NeedsPosts
// Used by: HighPriorityNeedsWindow (Donor view) and NeedsWishlistWindow (Beneficiary view)
using System;
using KapwaKuha.ViewModels;

namespace KapwaKuha.Models
{
    public class NeedsPostModel : ObservableObject
    {
        public string NeedsPost_ID { get; set; } = string.Empty;
        public string Org_ID { get; set; } = string.Empty;
        public string Org_Name { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;

        private string _urgency = "Medium";
        public string RequesterBeneficiaryId { get; set; } = string.Empty;

        public string ImagePath { get; set; } = string.Empty;
        public bool HasImage => !string.IsNullOrEmpty(ImagePath) && System.IO.File.Exists(ImagePath);
        public string Urgency
        {
            get => _urgency;
            set
            {
                _urgency = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(UrgencyColor));
                OnPropertyChanged(nameof(UrgencyTextColor));
            }
        }

        private string _status = "Open";
        public string Status
        {
            get => _status;
            set { _status = value; OnPropertyChanged(); }
        }

        public DateTime Post_Date { get; set; } = DateTime.Now;
        public string PostDateDisplay => Post_Date.ToString("MMM dd, yyyy");

        // Badge colors per doc Section 9.3
        public string UrgencyColor => Urgency switch
        {
            "High" => "#FFF0F0",   // light red background
            "Medium" => "#FFF8E6",   // light yellow background
            _ => "#E8F5E9"    // light GREEN background for Low (was #F0F8FF blue)
        };

        public string UrgencyTextColor => Urgency switch
        {
            "High" => "#C0304A",   // red text
            "Medium" => "#B8860B",   // amber text
            _ => "#2E7D52"    // GREEN text for Low (was #185FA5 blue)
        };
    }
}