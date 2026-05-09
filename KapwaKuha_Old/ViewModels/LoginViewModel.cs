// FILE: LoginViewModel.cs  
// Shared by DonorLoginWindow.xaml and BeneficiaryLoginWindow.xaml
// Role passed from code-behind: "Donor" | "Beneficiary"
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using KapwaKuha.Commands;
using KapwaKuha.Models;
using KapwaKuha.Services;

namespace KapwaKuha.ViewModels
{
    public class LoginViewModel : ObservableObject
    {
        public UserModel CurrentUser { get; }

        public ICommand LoginCommand { get; }
        public ICommand BackCommand { get; }
        public ICommand ForgotPasswordCommand { get; }
        public ICommand SignUpCommand { get; }

        private bool _errorVisible;
        public bool ErrorVisible
        {
            get => _errorVisible;
            set { _errorVisible = value; OnPropertyChanged(); }
        }

        private string _errorMessage = string.Empty;
        public string ErrorMessage
        {
            get => _errorMessage;
            set { _errorMessage = value; OnPropertyChanged(); }
        }

        public string RoleLabel => CurrentUser.Role == "Donor"
            ? "Donor Login" : "Beneficiary Login";
        public string RoleHint => CurrentUser.Role == "Donor"
            ? "Username  (e.g. juandc)" : "Username  (e.g. anareyes)";

        public LoginViewModel(string role)
        {
            CurrentUser = new UserModel { Role = role };

            LoginCommand = new RelayCommand(ExecuteLogin);

            BackCommand = new RelayCommand(_ =>
                NavigationService.Navigate(new View.ChooseRoleWindow()));

            ForgotPasswordCommand = new RelayCommand(_ =>
                NavigationService.Navigate(new View.ForgotPasswordWindow(role)));

            SignUpCommand = new RelayCommand(_ =>
                NavigationService.Navigate(new View.SignUpWindow(CurrentUser.Role)));
        }

        private async void ExecuteLogin(object? parameter)
        {
            if (parameter is PasswordBox pb)
                CurrentUser.Password = pb.Password;

            if (string.IsNullOrWhiteSpace(CurrentUser.UserID) ||
                CurrentUser.UserID.Contains(" ") ||
                string.IsNullOrWhiteSpace(CurrentUser.Password))
            {
                ErrorMessage = "Username/ID and password are required. No spaces allowed.";
                ErrorVisible = true;
                return;
            }

            if (CurrentUser.Role == "Donor")
            {
                var (ok, userId, fullName, username) =
                    await KapwaDataService.LoginDonor(CurrentUser.UserID, CurrentUser.Password);

                if (ok)
                {
                    ErrorVisible = false;
                    UserSession.UserId = userId;
                    UserSession.Username = username;
                    UserSession.FullName = fullName;
                    UserSession.Role = "Donor";
                    NavigationService.Navigate(new View.DonorDashboardWindow(userId));
                }
                else
                {
                    ErrorMessage = "Invalid username or password.";
                    ErrorVisible = true;
                }
            }
            else // Beneficiary
            {
                var (ok, userId, fullName, username) =
                    await KapwaDataService.LoginBeneficiary(CurrentUser.UserID, CurrentUser.Password);

                if (ok)
                {
                    ErrorVisible = false;
                    UserSession.UserId = userId;
                    UserSession.Username = username;
                    UserSession.FullName = fullName;
                    UserSession.Role = "Beneficiary";
                    NavigationService.Navigate(new View.BeneficiaryDashboardWindow(userId));
                }
                else
                {
                    ErrorMessage = "Invalid User ID or password.";
                    ErrorVisible = true;
                }
            }
        }
    }
}