// FILE: ViewModels/MyImpactDesignViewModel.cs
using System.Windows.Input;
using KapwaKuha.Commands;

namespace KapwaKuha.ViewModels
{
    public class MyImpactDesignViewModel
    {
        public string DonorName { get; } = "Donor: Juan Dela Cruz";
        public int TotalDonated { get; } = 12;
        public int TotalClaimed { get; } = 9;
        public int ActiveItems { get; } = 3;
        public int FulfilledNeeds { get; } = 4;
        public int ActiveBeneficiaries { get; } = 18;
        public double AvgStorageDays { get; } = 4.5;
        public string AvgStorageDisplay { get; } = "4.5 days avg. to claim";
        public bool IsBusy { get; } = false;

        public ICommand BackCommand { get; } = new RelayCommand(_ => { });
    }
}