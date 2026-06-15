// FILE: ViewModels/LoginViewModel.cs
using System;
using System.Threading.Tasks;
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
        public ICommand GoogleLoginCommand { get; }

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

            GoogleLoginCommand = new RelayCommand(_ => _ = ExecuteGoogleLogin());
        }

        // ── GOOGLE LOGIN ──────────────────────────────────────────────────────

        private async Task ExecuteGoogleLogin()
        {
            ErrorVisible = false;
            try
            {
                var (email, _) = await GoogleAuthService.GoogleLoginAsync();

                switch (CurrentUser.Role)
                {
                    case "Donor":
                        {
                            var (ok, userId, fullName, username) =
                                await KapwaDataService.LoginDonorByEmail(email);
                            if (ok)
                            {
                                UserSession.UserId = userId;
                                UserSession.Username = username;
                                UserSession.FullName = fullName;
                                UserSession.Role = "Donor";
                                await CheckAndNotifyStrikes(userId);
                                NavigationService.Navigate(new View.DonorDashboardWindow(userId));
                            }
                            else
                            {
                                ErrorMessage = $"No Donor account is linked to {email}.\nPlease log in with username/password instead.";
                                ErrorVisible = true;
                            }
                            break;
                        }
                    case "InstitutionalBeneficiary":
                        {
                            var (ok, userId, fullName, username) =
                                await KapwaDataService.LoginBeneficiaryByEmail(email);
                            if (ok)
                            {
                                UserSession.UserId = userId;
                                UserSession.Username = username;
                                UserSession.FullName = fullName;
                                UserSession.Role = "InstitutionalBeneficiary";
                                await CheckAndNotifyStrikes(userId);
                                NavigationService.Navigate(new View.BeneficiaryDashboardWindow(userId));
                            }
                            else
                            {
                                ErrorMessage = $"No Institutional Beneficiary account is linked to {email}.";
                                ErrorVisible = true;
                            }
                            break;
                        }
                    case "IndependentBeneficiary":
                        {
                            var (ok, userId, fullName, username) =
                                await KapwaDataService.LoginIndependentBeneficiaryByEmail(email);
                            if (ok)
                            {
                                UserSession.UserId = userId;
                                UserSession.Username = username;
                                UserSession.FullName = fullName;
                                UserSession.Role = "IndependentBeneficiary";
                                await CheckAndNotifyStrikes(userId);
                                NavigationService.Navigate(new View.BeneficiaryDashboardWindow(userId));
                            }
                            else
                            {
                                ErrorMessage = $"No Independent Beneficiary account is linked to {email}.";
                                ErrorVisible = true;
                            }
                            break;
                        }
                }
            }
            catch (OperationCanceledException)
            {
                // User closed the browser — silently ignore
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Google sign-in error: {ex.Message}";
                ErrorVisible = true;
            }
        }

        // ── USERNAME / PASSWORD LOGIN ─────────────────────────────────────────

        private async void ExecuteLogin(object? parameter)
        {
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
                            await CheckAndNotifyStrikes(userId);
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
                            await CheckAndNotifyStrikes(userId);
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
                            await CheckAndNotifyStrikes(userId);
                            NavigationService.Navigate(new View.BeneficiaryDashboardWindow(userId));
                        }
                        else { ErrorMessage = "Username or password not recognized."; ErrorVisible = true; }
                        break;
                    }
            }
        }

        private async Task CheckAndNotifyStrikes(string userId)
        {
            try
            {
                var (strikes, _) = await KapwaDataService.GetUserStrikesAndBanInfo(userId);
                if (strikes > 0 && strikes < 3)
                {
                    MessageBox.Show(
                        $"⚠️ Warning: You have {strikes} strike(s) on your account.\n\n" +
                        $"Receiving 3 strikes will result in permanent ban.\n\n" +
                        $"Please follow community guidelines to avoid further violations.",
                        "Account Strike Warning",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                }
            }
            catch { }
        }
    }
}