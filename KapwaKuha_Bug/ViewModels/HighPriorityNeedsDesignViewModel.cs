// FILE: ViewModels/HighPriorityNeedsDesignViewModel.cs
using System;
using System.Collections.ObjectModel;
using System.Windows.Input;
using KapwaKuha.Commands;
using KapwaKuha.Models;

namespace KapwaKuha.ViewModels
{
    public class HighPriorityNeedsDesignViewModel
    {
        public ObservableCollection<NeedsPostModel> NeedsPosts { get; } = new()
        {
            new NeedsPostModel
            {
                NeedsPost_ID = "NP001", Title = "Blankets for Street Children",
                Org_Name = "Barangay San Jose", Urgency = "High",
                Description = "We urgently need 50 warm blankets for homeless children.",
                Post_Date = DateTime.Now.AddDays(-1)
            },
            new NeedsPostModel
            {
                NeedsPost_ID = "NP002", Title = "School Supplies Kit",
                Org_Name = "SISC Student Welfare", Urgency = "Medium",
                Description = "Back-to-school kits needed for 30 students.",
                Post_Date = DateTime.Now.AddDays(-3)
            },
            new NeedsPostModel
            {
                NeedsPost_ID = "NP003", Title = "Canned Goods Drive",
                Org_Name = "Barangay San Jose", Urgency = "Low",
                Description = "Accepting canned goods donations for community pantry.",
                Post_Date = DateTime.Now.AddDays(-5)
            },
        };

        public bool IsBusy { get; } = false;
        public string FilterUrgency { get; } = "All";

        // Commands
        public ICommand BackCommand { get; } = new RelayCommand(_ => { });
        public ICommand RefreshCommand { get; } = new RelayCommand(_ => { });
        public ICommand DonateToNeedCommand { get; } = new RelayCommand(_ => { });
    }
}