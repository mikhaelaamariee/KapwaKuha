// FILE: HandoffModel.cs
// DB Table: HandoffDetails
// Used by ClaimItemViewModel when beneficiary picks delivery method
using System;
using KapwaKuha.ViewModels;

namespace KapwaKuha.Models
{
    public class HandoffModel : ObservableObject
    {
        public int Handoff_ID { get; set; }
        public string Claim_ID { get; set; } = string.Empty;

        private string _handoffType = "Pickup";
        public string HandoffType
        {
            get => _handoffType;
            set { _handoffType = value; OnPropertyChanged(); }
        }

        public string Location { get; set; } = string.Empty;
        public string EventName { get; set; } = string.Empty;
        public DateTime? EventDate { get; set; }
    }
}