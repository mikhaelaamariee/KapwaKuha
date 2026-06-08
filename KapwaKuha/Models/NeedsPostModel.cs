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

        // ── Before/After edit snapshot (null when this is a fresh post) ──────
        public string? PreviousTitle { get; set; }
        public string? PreviousDescription { get; set; }
        public string? PreviousUrgency { get; set; }
        /// <summary>True when this pending item is a re-submission of an existing post.</summary>
        public bool IsEdit => !string.IsNullOrEmpty(PreviousTitle);

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
                OnPropertyChanged(nameof(CanEdit));
            }
        }
        public bool IsApproved => Admin_Approval_Status == "Approved";
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