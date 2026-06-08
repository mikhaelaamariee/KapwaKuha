// FILE: ViewModels/LoginViewModel.cs
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

        // Exposed so IndependentBeneficiaryLoginWindow can sync show-pw TextBox
        private string _plainPassword = string.Empty;
        public string PlainPassword
        {
            get => _plainPassword;
            set { _plainPassword = value; OnPropertyChanged(); }
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
            // Accept PasswordBox OR plain string (from show-password TextBox)
            if (parameter is PasswordBox pb)
                CurrentUser.Password = pb.Password;
            else if (parameter is string s)
                CurrentUser.Password = s;
            else if (!string.IsNullOrEmpty(PlainPassword))
                CurrentUser.Password = PlainPassword;

            if (string.IsNullOrWhiteSpace(CurrentUser.UserID))
            {
                ErrorMessage = "Username is required.";
                ErrorVisible = true;
                return;
            }

            // Institutional and Donor: strict no-spaces rule on UserID
            // Independent: relaxed — just needs non-empty username (low-barrier model)
            if (CurrentUser.Role != "IndependentBeneficiary" &&
                CurrentUser.UserID.Contains(" "))
            {
                ErrorMessage = "Username cannot contain spaces.";
                ErrorVisible = true;
                return;
            }

            if (string.IsNullOrWhiteSpace(CurrentUser.Password))
            {
                ErrorMessage = "Password is required.";
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
                        // Low-barrier: same LoginIndependentBeneficiary call, but no
                        // extra front-end gating beyond non-empty username/password
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
                            NavigationService.Navigate(new View.BeneficiaryDashboardWindow(userId));
                        }
                        else { ErrorMessage = "Username or password not recognized."; ErrorVisible = true; }
                        break;
                    }
            }
        }
    }
}