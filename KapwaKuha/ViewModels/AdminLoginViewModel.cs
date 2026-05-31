// FILE: ViewModels/AdminLoginViewModel.cs  (NEW)
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using KapwaKuha.Commands;
using KapwaKuha.Services;

namespace KapwaKuha.ViewModels
{
    public class AdminLoginViewModel : ObservableObject
    {
        private string _adminId = string.Empty;
        public string AdminId
        {
            get => _adminId;
            set { _adminId = value; OnPropertyChanged(); }
        }

        private string _errorMessage = string.Empty;
        public string ErrorMessage
        {
            get => _errorMessage;
            set { _errorMessage = value; OnPropertyChanged(); }
        }

        private bool _errorVisible;
        public bool ErrorVisible
        {
            get => _errorVisible;
            set { _errorVisible = value; OnPropertyChanged(); }
        }

        public ICommand LoginCommand { get; }
        public ICommand BackCommand { get; }

        public AdminLoginViewModel()
        {
            BackCommand = new RelayCommand(_ =>
                NavigationService.Navigate(new View.ChooseRoleWindow()));

            LoginCommand = new RelayCommand(async param =>
            {
                string password = (param is PasswordBox pb) ? pb.Password : string.Empty;

                if (string.IsNullOrWhiteSpace(AdminId) || string.IsNullOrWhiteSpace(password))
                {
                    ErrorMessage = "Admin ID and password are required.";
                    ErrorVisible = true;
                    return;
                }

                var (ok, userId, fullName) =
                    await KapwaDataService.LoginAdmin(AdminId, password);

                if (ok)
                {
                    ErrorVisible = false;
                    UserSession.UserId = userId;
                    UserSession.FullName = fullName;
                    UserSession.Username = userId;
                    UserSession.Role = "Admin";
                    NavigationService.Navigate(new View.AdminDashboardWindow(userId));
                }
                else
                {
                    ErrorMessage = "Invalid Admin ID or password.";
                    ErrorVisible = true;
                }
            });
        }
    }
}