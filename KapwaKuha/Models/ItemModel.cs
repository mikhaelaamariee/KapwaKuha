// FILE: Models/ItemModel.cs  (UPDATED — adds Admin_Approval_Status)
using System;
using KapwaKuha.ViewModels;

namespace KapwaKuha.Models
{
    public class ItemModel : ObservableObject
    {
        public string Item_ID { get; set; } = string.Empty;
        public string Item_Name { get; set; } = string.Empty;
        public string Item_Condition { get; set; } = "Good";

        private string _itemStatus = "Available";

        public string Item_Status
        {
            get => _itemStatus;
            set
            {
                _itemStatus = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(StatusColor));
                OnPropertyChanged(nameof(StatusBadgeBackground));
                OnPropertyChanged(nameof(StatusBadgeColor));
            }
        }

        private string _adminApprovalStatus = "Pending";
        public string Admin_Approval_Status
        {
            get => _adminApprovalStatus;
            set
            {
                _adminApprovalStatus = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ApprovalBadgeColor));
                OnPropertyChanged(nameof(ApprovalBadgeBackground));
                OnPropertyChanged(nameof(IsApproved));
            }
        }
        private double _donorAverageRating;
        public double DonorAverageRating
        {
            get => _donorAverageRating;
            set
            {
                _donorAverageRating = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(DonorRatingDisplay));
            }
        }
        public string DonorRatingDisplay =>
            _donorAverageRating > 0 ? $"{_donorAverageRating:F1}★" : "—";

        public bool IsApproved => Admin_Approval_Status == "Approved";

        public string ApprovalBadgeBackground => Admin_Approval_Status switch
        {
            "Approved" => "#E8F5E9",
            "Rejected" => "#FFF0F0",
            _ => "#FFF8E6"   // Pending
        };

        public string ApprovalBadgeColor => Admin_Approval_Status switch
        {
            "Approved" => "#2DC653",
            "Rejected" => "#C0304A",
            _ => "#B8860B"
        };

        // ── Rejection note from admin ─────────────────────────────────────────────

        private string _rejectionNote = string.Empty;
        public string RejectionNote
        {
            get => _rejectionNote;
            set
            {
                _rejectionNote = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(HasRejectionNote));
            }
        }
        public bool HasRejectionNote => !string.IsNullOrEmpty(RejectionNote);
        public bool IsRejected => Admin_Approval_Status == "Rejected";
        // Pending: can edit if Available
        // Rejected: can edit regardless of status (admin rejected it, donor should fix it)
        public bool CanDonorEdit =>
            (Admin_Approval_Status == "Pending" && Item_Status == "Available") ||
            (Admin_Approval_Status == "Rejected");



        public DateTime Date_Found { get; set; } = DateTime.Now;
        public string Donor_ID { get; set; } = string.Empty;
        public string Donor_Name { get; set; } = string.Empty;
        public string Category_ID { get; set; } = string.Empty;
        public string Category_Name { get; set; } = string.Empty;
        public string PostType { get; set; } = "GeneralPost";
        public string TargetBeneficiary_ID { get; set; } = string.Empty;
        public string Item_Description { get; set; } = string.Empty;

        public bool IsGeneralPost => PostType == "GeneralPost";
        public bool IsDirectTarget => PostType == "DirectTarget";

        private string _imagePath = string.Empty;
        public string Item_ImagePath
        {
            get => _imagePath;
            set
            {
                _imagePath = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(HasItemImage));
            }
        }

        public bool HasItemImage =>
            !string.IsNullOrEmpty(Item_ImagePath) &&
            System.IO.File.Exists(Item_ImagePath);

        public int StorageDays => (DateTime.Now - Date_Found).Days;
        public string StorageDaysDisplay
        {
            get => StorageDays == 0 ? "Posted today" :
                   StorageDays == 1 ? "1 day posted" :
                   $"{StorageDays} days posted";
            set { }
        }

        public string StatusBadgeBackground => Item_Status switch
        {
            "Available" => "#EBF7FB",
            "Claimed" => "#E8F4F0",
            "Reserved" => "#FFF8E6",
            _ => "#F5F5F5"
        };
        public string StatusBadgeColor => Item_Status switch
        {
            "Available" => "#0077B6",
            "Claimed" => "#2DC653",
            "Reserved" => "#B8860B",
            _ => "#9E9E9E"
        };
        public string StatusColor => Item_Status switch
        {
            "Available" => "#00B4D8",
            "Claimed" => "#9E9E9E",
            "Reserved" => "#FFA500",
            _ => "#9E9E9E"
        };
    }
}