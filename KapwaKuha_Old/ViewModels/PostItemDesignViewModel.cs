// FILE: ViewModels/PostItemDesignViewModel.cs
using System.Collections.ObjectModel;
using System.Windows.Input;
using KapwaKuha.Commands;

namespace KapwaKuha.ViewModels          // ← was missing namespace
{
    public class PostItemDesignViewModel
    {
        public string DonorLabel { get; } = "Donor: juandc";
        public string ItemName { get; } = "Assorted School Supplies";
        public string Description { get; } = "A sample item description.";
        public string SelectedCategory { get; } = "School Supplies";
        public string SelectedCondition { get; } = "New";
        public string ImagePath { get; } = string.Empty;
        public bool HasImage { get; } = false;
        public bool IsGeneralPost { get; } = true;
        public bool IsDirectTarget { get; } = false;
        public bool IsModeLocked { get; } = false;
        public string LockedBeneficiaryDisplay { get; } = string.Empty;

        public ObservableCollection<string> Categories { get; } =
            new() { "Clothing", "Food", "Electronics", "Medicine", "School Supplies" };

        public ObservableCollection<string> Conditions { get; } =
            new() { "New", "Good", "Fair", "Poor" };

        public ObservableCollection<BeneficiaryRow> Beneficiaries { get; } = new()
        {
            new BeneficiaryRow { Id = "B001", DisplayName = "Ana Reyes — Barangay San Jose" },
            new BeneficiaryRow { Id = "B002", DisplayName = "Carlo Santos — SISC Student Welfare" },
        };

        public bool IsBusy { get; } = false;
        public bool ErrorVisible { get; } = false;
        public string ErrorMessage { get; } = string.Empty;

        // Commands
        public ICommand BackCommand { get; } = new RelayCommand(_ => { });
        public ICommand SaveCommand { get; } = new RelayCommand(_ => { });
        public ICommand BrowseImageCommand { get; } = new RelayCommand(_ => { });
        public ICommand SetGeneralPostCommand { get; } = new RelayCommand(_ => { });
        public ICommand SetDirectTargetCommand { get; } = new RelayCommand(_ => { });
    }
}