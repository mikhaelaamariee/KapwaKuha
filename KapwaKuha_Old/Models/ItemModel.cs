// FILE: Models/ItemModel.cs
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

        public DateTime Date_Found { get; set; } = DateTime.Now;
        public string Donor_ID { get; set; } = string.Empty;
        public string Donor_Name { get; set; } = string.Empty;
        public string Category_ID { get; set; } = string.Empty;
        public string Category_Name { get; set; } = string.Empty;
        public string PostType { get; set; } = "GeneralPost";
        public string TargetBeneficiary_ID { get; set; } = string.Empty;
        public string Item_Description { get; set; } = string.Empty;



        // Computed helper used by GetAvailableItems filter
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

        // Checks the file actually exists so the Image control doesn't crash on a stale path
        public bool HasItemImage =>
            !string.IsNullOrEmpty(Item_ImagePath) &&
            System.IO.File.Exists(Item_ImagePath);

        public int StorageDays => (DateTime.Now - Date_Found).Days;
        public string StorageDaysDisplay
        {
            get => StorageDays == 0 ? "Posted today" :
                   StorageDays == 1 ? "1 day posted" :
                   $"{StorageDays} days posted";

            // Add this empty setter to stop WPF from crashing on OneWayToSource/TwoWay bindings
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