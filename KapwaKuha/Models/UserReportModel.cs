// FILE: Models/UserReportModel.cs
using System;
using KapwaKuha.ViewModels;

namespace KapwaKuha.Models
{
    public class UserReportModel : ObservableObject
    {
        public string Report_ID { get; set; } = string.Empty;
        public string Reporter_ID { get; set; } = string.Empty;
        public string Reported_ID { get; set; } = string.Empty;
        public string Reported_Name { get; set; } = string.Empty;
        public string Reporter_Name { get; set; } = string.Empty;

        // Alias properties for XAML bindings
        public string ReportedName => Reported_Name;
        public string ReporterName => Reporter_Name;
        public string ReportType => Report_Type;

        // FakeItem | Fraud | Spam | Inappropriate
        public string Report_Type { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;

        private string _status = "Open";
        public string Status
        {
            get => _status;
            set { _status = value; OnPropertyChanged(); OnPropertyChanged(nameof(StatusColor)); }
        }

        public string Admin_Action_Taken { get; set; } = "None";
        public DateTime Filed_At { get; set; } = DateTime.Now;
        public string Admin_Notes { get; set; } = string.Empty;

        public string FiledAtDisplay => Filed_At.ToString("MMM dd, yyyy HH:mm");
        public string StatusColor => Status switch
        {
            "Open" => "#C0304A",
            "Reviewed" => "#2DC653",
            "Dismissed" => "#9E9E9E",
            _ => "#9E9E9E"
        };
    }
}