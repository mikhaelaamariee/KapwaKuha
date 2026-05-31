// FILE: ViewModels/LoginViewModel.cs  (UPDATED — handles 3 login types)
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

        public string RoleLabel => CurrentUser.Role switch
        {
            "Donor" => "Donor Login",
            "InstitutionalBeneficiary" => "Institutional Beneficiary Login",
            "IndependentBeneficiary" => "Independent Beneficiary Login",
            _ => "Login"
        };

        public string RoleHint => CurrentUser.Role switch
        {
            "Donor" => "Username  (e.g. juandc)",
            "InstitutionalBeneficiary" => "Username  (e.g. anareyes)",
            "IndependentBeneficiary" => "Username  (e.g. juanreyes)",
            _ => "Username"
        };

        public string SignUpLabel => CurrentUser.Role == "Donor"
            ? "New donor? Sign up" : "New here? Sign up";

        // True if this login window should show a SignUp link
        public bool ShowSignUp =>
            CurrentUser.Role == "Donor" ||
            CurrentUser.Role == "IndependentBeneficiary";

        public LoginViewModel(string role)
        {
            CurrentUser = new UserModel { Role = role };

            LoginCommand = new RelayCommand(ExecuteLogin);

            BackCommand = new RelayCommand(_ =>
                NavigationService.Navigate(new View.ChooseRoleWindow()));

            ForgotPasswordCommand = new RelayCommand(_ =>
                NavigationService.Navigate(new View.ForgotPasswordWindow(role)));

            SignUpCommand = new RelayCommand(_ =>
            {
                if (role == "Donor")
                    NavigationService.Navigate(new View.SignUpWindow("Donor"));
                else
                    NavigationService.Navigate(new View.IndependentBeneficiarySignUpWindow());
            });
        }

        private async void ExecuteLogin(object? parameter)
        {
            if (parameter is PasswordBox pb)
                CurrentUser.Password = pb.Password;

            if (string.IsNullOrWhiteSpace(CurrentUser.UserID) ||
                CurrentUser.UserID.Contains(" ") ||
                string.IsNullOrWhiteSpace(CurrentUser.Password))
            {
                ErrorMessage = "Username and password are required. No spaces allowed.";
                ErrorVisible = true;
                return;
            }

            switch (CurrentUser.Role)
            {
                case "Donor":
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
                        else { ErrorMessage = "Invalid username or password."; ErrorVisible = true; }
                        break;
                    }

                case "InstitutionalBeneficiary":
                    {
                        var (ok, userId, fullName, username) =
                            await KapwaDataService.LoginBeneficiary(CurrentUser.UserID, CurrentUser.Password);
                        if (ok)
                        {
                            ErrorVisible = false;
                            UserSession.UserId = userId;
                            UserSession.Username = username;
                            UserSession.FullName = fullName;
                            UserSession.Role = "InstitutionalBeneficiary";
                            NavigationService.Navigate(new View.BeneficiaryDashboardWindow(userId));
                        }
                        else { ErrorMessage = "Invalid username or password."; ErrorVisible = true; }
                        break;
                    }

                case "IndependentBeneficiary":
                    {
                        var (ok, userId, fullName, username) =
                            await KapwaDataService.LoginIndependentBeneficiary(
                                CurrentUser.UserID, CurrentUser.Password);
                        if (ok)
                        {
                            ErrorVisible = false;
                            UserSession.UserId = userId;
                            UserSession.Username = username;
                            UserSession.FullName = fullName;
                            UserSession.Role = "IndependentBeneficiary";
                            // IndependentBeneficiaries use the same dashboard as Institutional for now
                            NavigationService.Navigate(new View.BeneficiaryDashboardWindow(userId));
                        }
                        else { ErrorMessage = "Invalid username or password."; ErrorVisible = true; }
                        break;
                    }
            }
        }
    }
}