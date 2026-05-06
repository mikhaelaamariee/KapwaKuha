// FILE: ViewModels/NeedsWishlistDesignViewModel.cs
using System;
using System.Collections.ObjectModel;
using System.Windows.Input;
using KapwaKuha.Commands;
using KapwaKuha.Models;

namespace KapwaKuha.ViewModels
{
    public class NeedsWishlistDesignViewModel
    {
        public string Title { get; } = "Blankets for Street Children";
        public string Description { get; } = "We urgently need 50 warm blankets.";
        public string Urgency { get; } = "High";
        public string ImagePath { get; } = string.Empty;
        public bool HasImage { get; } = false;

        public bool IsLow { get; } = false;
        public bool IsMedium { get; } = false;
        public bool IsHigh { get; } = true;

        public bool IsBusy { get; } = false;
        public bool ErrorVisible { get; } = false;
        public string ErrorMessage { get; } = string.Empty;

        // Edit mode properties
        public bool IsEditing { get; } = false;
        public bool IsCreating { get; } = true;

        public ObservableCollection<NeedsPostModel> MyPosts { get; } = new()
        {
            new NeedsPostModel
            {
                NeedsPost_ID = "NP001", Title = "Blankets for Street Children",
                Urgency = "High", Status = "Open",
                Post_Date = DateTime.Now.AddDays(-1)
            },
            new NeedsPostModel
            {
                NeedsPost_ID = "NP002", Title = "School Supplies Kit",
                Urgency = "Medium", Status = "Fulfilled",
                Post_Date = DateTime.Now.AddDays(-7)
            },
        };

        // Commands
        public ICommand BackCommand { get; } = new RelayCommand(_ => { });
        public ICommand PostNeedCommand { get; } = new RelayCommand(_ => { });
        public ICommand BrowseImageCommand { get; } = new RelayCommand(_ => { });
        public ICommand SetLowCommand { get; } = new RelayCommand(_ => { });
        public ICommand SetMediumCommand { get; } = new RelayCommand(_ => { });
        public ICommand SetHighCommand { get; } = new RelayCommand(_ => { });
        public ICommand SelectPostCommand { get; } = new RelayCommand(_ => { });
        public ICommand ClearSelectionCommand { get; } = new RelayCommand(_ => { });
        public ICommand UpdateNeedCommand { get; } = new RelayCommand(_ => { });
        public ICommand DeleteNeedCommand { get; } = new RelayCommand(_ => { });
    }
}