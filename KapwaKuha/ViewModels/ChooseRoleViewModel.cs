// FILE: ViewModels/ChooseRoleViewModel.cs
using System.Windows.Input;
using KapwaKuha.Commands;
using KapwaKuha.Services;

namespace KapwaKuha.ViewModels
{
    public class ChooseRoleViewModel : ObservableObject
    {
        public ICommand DonorCommand { get; }
        public ICommand BeneficiaryCommand { get; }
        public ICommand AdminCommand { get; }

        public ChooseRoleViewModel()
        {
            DonorCommand = new RelayCommand(_ =>
                NavigationService.Navigate(new View.DonorLoginWindow()));

            // Beneficiary button now goes to the type-select gate first
            BeneficiaryCommand = new RelayCommand(_ =>
                NavigationService.Navigate(new View.BeneficiaryTypeSelectWindow()));

            AdminCommand = new RelayCommand(_ =>
                NavigationService.Navigate(new View.AdminLoginWindow()));
        }
    }
}