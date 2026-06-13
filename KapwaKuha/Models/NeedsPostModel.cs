// FILE: Models/NeedsPostModel.cs
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
        public string RequesterBeneficiaryId { get; set; } = string.Empty;
        public string ImagePath { get; set; } = string.Empty;
        public bool HasImage => !string.IsNullOrEmpty(ImagePath) && System.IO.File.Exists(ImagePath);
        public DateTime Post_Date { get; set; } = DateTime.Now;
        public string PostDateDisplay => Post_Date.ToString("MMM dd, yyyy");

        // ── Edit snapshot (null = first submission; populated = re-edit after rejection) ──
        public string? PreviousTitle { get; set; }
        public string? PreviousDescription { get; set; }
        public string? PreviousUrgency { get; set; }

        // True when this is a re-edit submission — drives the diff panel in admin view
        public bool IsEdit => !string.IsNullOrEmpty(PreviousTitle);

        // ── Rejection note written by admin ──────────────────────────────────
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


        // FILE: Models/NeedsPostModel.cs
        // Replace the computed BeneTypeBadge property (and remove IsIndependent) with a settable backing field:

        private string _beneTypeBadge = "Institutional";
        public string BeneTypeBadge
        {
            get => _beneTypeBadge;
            set
            {
                _beneTypeBadge = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(BeneTypeBadgeColor));
                OnPropertyChanged(nameof(BeneTypeBadgeTextColor));
            }
        }
        public string BeneTypeBadgeColor => BeneTypeBadge == "Institutional" ? "#EFF6FF" : "#F0FDF4";
        public string BeneTypeBadgeTextColor => BeneTypeBadge == "Institutional" ? "#1D4ED8" : "#15803D";

        public bool HasRejectionNote => !string.IsNullOrEmpty(RejectionNote);

        // ── Urgency ──────────────────────────────────────────────────────────
        private string _urgency = "Medium";
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
        public string UrgencyColor => Urgency switch
        {
            "High" => "#FFF0F0",
            "Medium" => "#FFF8E6",
            _ => "#E8F5E9"
        };
        public string UrgencyTextColor => Urgency switch
        {
            "High" => "#C0304A",
            "Medium" => "#B8860B",
            _ => "#2E7D52"
        };

        // ── Status ───────────────────────────────────────────────────────────
        private string _status = "Open";
        public string Status
        {
            get => _status;
            set { _status = value; OnPropertyChanged(); }
        }

        // ── Admin Approval ───────────────────────────────────────────────────
        private string _adminApprovalStatus = "Pending";
        public string Admin_Approval_Status
        {
            get => _adminApprovalStatus;
            set
            {
                _adminApprovalStatus = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ApprovalBadgeBackground));
                OnPropertyChanged(nameof(ApprovalBadgeText));
                OnPropertyChanged(nameof(ApprovalBadgeTextColor));
                OnPropertyChanged(nameof(IsApproved));
                OnPropertyChanged(nameof(IsRejected));
                OnPropertyChanged(nameof(CanEdit));
            }
        }
        public bool IsApproved => Admin_Approval_Status == "Approved";
        public bool IsRejected => Admin_Approval_Status == "Rejected";
        // Beneficiary can edit if not yet Approved
        public bool CanEdit => Admin_Approval_Status != "Approved";

        public string ApprovalBadgeBackground => Admin_Approval_Status switch
        {
            "Approved" => "#DCFCE7",
            "Rejected" => "#FEE2E2",
            _ => "#FEF9C3"
        };
        public string ApprovalBadgeText => Admin_Approval_Status switch
        {
            "Approved" => "✅ Live",
            "Rejected" => "❌ Rejected — edit & resubmit",
            _ => "⏳ Awaiting Admin Approval"
        };
        public string ApprovalBadgeTextColor => Admin_Approval_Status switch
        {
            "Approved" => "#166534",
            "Rejected" => "#991B1B",
            _ => "#854D0E"
        };
    }
}