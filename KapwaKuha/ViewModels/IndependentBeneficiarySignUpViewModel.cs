// FILE: ViewModels/IndependentBeneficiarySignUpViewModel.cs
using System;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using KapwaKuha.Commands;
using KapwaKuha.Models;
using KapwaKuha.Services;

namespace KapwaKuha.ViewModels
{
    public class IndependentBeneficiarySignUpViewModel : ObservableObject
    {
        private string _fName = string.Empty;
        private string _lName = string.Empty;
        private string _username = string.Empty;
        private string _contact = string.Empty;
        private string _address = string.Empty;
        private string _selectedSex = "Male";
        private string _password = string.Empty;
        private string _confirmPass = string.Empty;
        private string _securityQuestion = "What is your pet name?";
        private string _securityAnswer = string.Empty;
        private string _profilePicturePath = string.Empty;
        private string _errorMessage = string.Empty;
        private bool _errorVisible;
        private bool _isLoading;

        public string FName
        {
            get => _fName;
            set
            {
                if (!string.IsNullOrEmpty(value) && value.Any(c => !char.IsLetter(c) && !char.IsWhiteSpace(c)))
                { ShowError("Name must be letters only."); return; }
                _fName = value; OnPropertyChanged(); ClearError();
            }
        }
        public string LName
        {
            get => _lName;
            set
            {
                if (!string.IsNullOrEmpty(value) && value.Any(c => !char.IsLetter(c) && !char.IsWhiteSpace(c)))
                { ShowError("Name must be letters only."); return; }
                _lName = value; OnPropertyChanged(); ClearError();
            }
        }
        public string Username
        {
            get => _username;
            set
            {
                if (!string.IsNullOrEmpty(value) && value.Contains(" "))
                { ShowError("Username cannot contain spaces."); return; }
                _username = value; OnPropertyChanged(); ClearError();
            }
        }
        public string Contact
        {
            get => _contact;
            set
            {
                if (!string.IsNullOrEmpty(value) && value.Any(c => !char.IsDigit(c)))
                { ShowError("Contact must be digits only. e.g. 09171234567"); return; }
                _contact = value; OnPropertyChanged(); ClearError();
            }
        }
        public string Address
        {
            get => _address;
            set { _address = value; OnPropertyChanged(); }
        }
        public string SelectedSex
        {
            get => _selectedSex;
            set { _selectedSex = value; OnPropertyChanged(); }
        }
        public string Password { get => _password; set { _password = value; OnPropertyChanged(); } }
        public string ConfirmPass { get => _confirmPass; set { _confirmPass = value; OnPropertyChanged(); } }
        public string SecurityQuestion { get => _securityQuestion; set { _securityQuestion = value; OnPropertyChanged(); } }
        public string SecurityAnswer { get => _securityAnswer; set { _securityAnswer = value; OnPropertyChanged(); } }
        public string ProfilePicturePath
        {
            get => _profilePicturePath;
            set { _profilePicturePath = value; OnPropertyChanged(); OnPropertyChanged(nameof(HasPicture)); }
        }
        public bool HasPicture =>
            !string.IsNullOrEmpty(_profilePicturePath) && System.IO.File.Exists(_profilePicturePath);
        public string ErrorMessage { get => _errorMessage; set { _errorMessage = value; OnPropertyChanged(); } }
        public bool ErrorVisible { get => _errorVisible; set { _errorVisible = value; OnPropertyChanged(); } }
        public bool IsLoading { get => _isLoading; set { _isLoading = value; OnPropertyChanged(); } }

        private string _email = string.Empty;
        public string Email
        {
            get => _email;
            set { _email = value; OnPropertyChanged(); }
        }

        public ICommand RegisterCommand { get; }
        public ICommand BackCommand { get; }
        public ICommand BrowsePictureCommand { get; }

        public IndependentBeneficiarySignUpViewModel()
        {
            BackCommand = new RelayCommand(_ =>
                NavigationService.Navigate(new View.ChooseRoleWindow()));

            BrowsePictureCommand = new RelayCommand(_ =>
            {
                var dlg = new Microsoft.Win32.OpenFileDialog
                {
                    Filter = "Image files (*.png;*.jpg;*.jpeg;*.bmp)|*.png;*.jpg;*.jpeg;*.bmp",
                    Title = "Select Profile Picture"
                };
                if (dlg.ShowDialog() == true) ProfilePicturePath = dlg.FileName;
            });

            RegisterCommand = new AsyncRelayCommand(async _ =>
            {
                ErrorVisible = false;
                if (string.IsNullOrWhiteSpace(FName))
                { ShowError("First name is required."); return; }
                if (string.IsNullOrWhiteSpace(LName))
                { ShowError("Last name is required."); return; }
                if (string.IsNullOrWhiteSpace(Username))
                { ShowError("Username is required."); return; }
                if (string.IsNullOrWhiteSpace(Contact) || Contact.Length != 11 || !Contact.StartsWith("09"))
                { ShowError("Contact must be 11 digits starting with 09."); return; }
                if (string.IsNullOrWhiteSpace(Password) || Password.Length < 6)
                { ShowError("Password must be at least 6 characters."); return; }
                if (Password != ConfirmPass)
                { ShowError("Passwords do not match."); return; }
                if (string.IsNullOrWhiteSpace(SecurityAnswer))
                { ShowError("Security answer is required."); return; }

                // EMAIL VALIDATION added here
                var (emailOk, emailErr) = ValidateEmail(Email, "IndependentBeneficiary");
                if (!emailOk) { ShowError(emailErr); return; }

                var confirm = MessageBox.Show(
                    $"Register as Independent Beneficiary?\n\nName: {FName} {LName}\nUsername: {Username}\nContact: {Contact}",
                    "Confirm Registration", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (confirm != MessageBoxResult.Yes) return;

                try
                {
                    IsLoading = true;
                    string id = await KapwaDataService.GetNextIndependentBeneficiaryId();
                    var bene = new IndependentBeneficiaryModel
                    {
                        IndepBene_ID = id,
                        FullName = $"{FName} {LName}",
                        Username = Username,
                        Sex = SelectedSex,
                        ContactNumber = Contact,
                        Address = Address,
                        ProfilePicturePath = ProfilePicturePath
                    };
                    await KapwaDataService.RegisterIndependentBeneficiary(
                        bene, Password, SecurityQuestion, SecurityAnswer, Email);

                    MessageBox.Show(
                        $"✅ Registered! Your ID: {id}\nYour account is pending Admin approval.\nYou will be able to log in once approved.",
                        "Registration Submitted", MessageBoxButton.OK, MessageBoxImage.Information);

                    NavigationService.Navigate(new View.ChooseRoleWindow());
                }
                catch { }
                finally { IsLoading = false; }
            });
        }

        // Email validation helper method moved outside the constructor
        private static (bool Valid, string Error) ValidateEmail(string email, string role)
        {
            if (string.IsNullOrWhiteSpace(email))
                return (false, "Email is required.");

            var atIndex = email.IndexOf('@');
            if (atIndex <= 0 || atIndex != email.LastIndexOf('@') || atIndex == email.Length - 1)
                return (false, "Enter a valid email address (e.g. juan@gmail.com).");

            var domain = email[(atIndex + 1)..].ToLowerInvariant();
            var dotIdx = domain.LastIndexOf('.');
            if (dotIdx <= 0 || dotIdx == domain.Length - 1)
                return (false, "Email domain is invalid (e.g. @gmail.com).");

            return (true, string.Empty);
        }

        private void ShowError(string msg) { ErrorMessage = msg; ErrorVisible = true; }
        private void ClearError() { if (_errorVisible) { ErrorMessage = string.Empty; ErrorVisible = false; } }
    }
}