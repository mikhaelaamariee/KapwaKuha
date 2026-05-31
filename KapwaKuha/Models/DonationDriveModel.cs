// FILE: Models/DonationDriveModel.cs  (NEW — for finals)
using System;
using KapwaKuha.ViewModels;

namespace KapwaKuha.Models
{
    public class DonationDriveModel : ObservableObject
    {
        public string Drive_ID { get; set; } = string.Empty;
        public string Drive_Name { get; set; } = string.Empty;
        public string Drive_Description { get; set; } = string.Empty;
        public string Organization_ID { get; set; } = string.Empty;
        public string Organization_Name { get; set; } = string.Empty;
        public DateTime Drive_Date { get; set; } = DateTime.Now;
        public string Location { get; set; } = string.Empty;    // OSM coordinate target

        private string _driveStatus = "Upcoming";
        public string Drive_Status
        {
            get => _driveStatus;
            set { _driveStatus = value; OnPropertyChanged(); OnPropertyChanged(nameof(StatusColor)); }
        }

        private string _adminApprovalStatus = "Pending";
        public string Admin_Approval_Status
        {
            get => _adminApprovalStatus;
            set { _adminApprovalStatus = value; OnPropertyChanged(); }
        }

        public string ImagePath { get; set; } = string.Empty;
        public bool HasImage =>
            !string.IsNullOrEmpty(ImagePath) && System.IO.File.Exists(ImagePath);

        public string DriveDateDisplay => Drive_Date.ToString("MMM dd, yyyy  HH:mm");
        public string StatusColor => Drive_Status switch
        {
            "Upcoming" => "#0077B6",
            "Ongoing" => "#2DC653",
            "Completed" => "#9E9E9E",
            _ => "#9E9E9E"
        };
    }
}