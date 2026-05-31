// FILE: ViewModels/ChooseRoleViewModel.cs  (UPDATED — adds Admin + IndepBene routes)
using System.Windows.Input;
using KapwaKuha.Commands;
using KapwaKuha.Services;

namespace KapwaKuha.ViewModels
{
    public class ChooseRoleViewModel : ObservableObject
    {
        public ICommand DonorCommand { get; }
        public ICommand BeneficiaryCommand { get; }
        public ICommand IndependentBeneficiaryCommand { get; }
        public ICommand AdminCommand { get; }

        public ChooseRoleViewModel()
        {
            DonorCommand = new RelayCommand(_ =>
                NavigationService.Navigate(new View.DonorLoginWindow()));

            // Institutional Beneficiary uses the existing BeneficiaryLoginWindow
            BeneficiaryCommand = new RelayCommand(_ =>
                NavigationService.Navigate(new View.BeneficiaryLoginWindow()));

            // Independent Beneficiary: separate login that calls LoginIndependentBeneficiary
            // FIX: Use parameterless constructor, then set mode property if needed
            IndependentBeneficiaryCommand = new RelayCommand(_ =>
                NavigationService.Navigate(new View.BeneficiaryLoginWindow()));

            // Admin: navigates to a dedicated AdminLoginWindow
            // (Create AdminLoginWindow.xaml + code-behind using AdminLoginViewModel)
            AdminCommand = new RelayCommand(_ =>
                NavigationService.Navigate(new View.AdminLoginWindow()));
        }
    }
}