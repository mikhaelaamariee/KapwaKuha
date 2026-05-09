// FILE: ChooseRoleViewModel.cs
// Window: ChooseRoleWindow.xaml
// Parallel to ChooseRoleViewModel in CarRentals
using System.Windows;
using System.Windows.Input;
using KapwaKuha.Commands;
using KapwaKuha.Services;

namespace KapwaKuha.ViewModels
{
    public class ChooseRoleViewModel : ObservableObject
    {
        public ICommand DonorCommand { get; }
        public ICommand BeneficiaryCommand { get; }
        public ICommand ExitCommand { get; }

        public ChooseRoleViewModel()
        {
            DonorCommand = new RelayCommand(_ =>
                NavigationService.Navigate(new View.DonorLoginWindow()));

            BeneficiaryCommand = new RelayCommand(_ =>
                NavigationService.Navigate(new View.BeneficiaryLoginWindow()));

            ExitCommand = new RelayCommand(_ =>
            {
                var r = MessageBox.Show("Exit KapwaKuha?", "Exit",
                    MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (r == MessageBoxResult.Yes) Application.Current.Shutdown();
            });
        }
    }
}