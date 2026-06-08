// FILE: ViewModels/BeneficiaryTypeSelectViewModel.cs
using System.Windows.Input;
using KapwaKuha.Commands;
using KapwaKuha.Services;

namespace KapwaKuha.ViewModels
{
    public class BeneficiaryTypeSelectViewModel : ObservableObject
    {
        public ICommand InstitutionalCommand { get; }
        public ICommand IndependentCommand { get; }
        public ICommand BackCommand { get; }
        public ICommand ExitCommand { get; }

        public BeneficiaryTypeSelectViewModel()
        {
            // Institutional → existing full-credential BeneficiaryLoginWindow
            InstitutionalCommand = new RelayCommand(_ =>
                NavigationService.Navigate(new View.BeneficiaryLoginWindow()));

            // Independent → dedicated low-barrier login
            IndependentCommand = new RelayCommand(_ =>
                NavigationService.Navigate(new View.IndependentBeneficiaryLoginWindow()));

            BackCommand = new RelayCommand(_ =>
                NavigationService.Navigate(new View.ChooseRoleWindow()));

            ExitCommand = new RelayCommand(_ =>
                System.Windows.Application.Current.Shutdown());
        }
    }
}