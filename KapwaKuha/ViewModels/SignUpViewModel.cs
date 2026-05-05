// FILE: SignUpViewModel.cs
// Window: SignUpWindow.xaml
// Parallel to SignUpViewModel in CarRentals — handles both Donor and Beneficiary
using System;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using KapwaKuha.Commands;
using KapwaKuha.Models;
using KapwaKuha.Services;

namespace KapwaKuha.ViewModels
{
    public class SignUpViewModel : ObservableObject
    {
        private readonly string _role;

        // ── Common fields ──────────────────────────────────────────────────────
        private string _fName = string.Empty;
        private string _lName = string.Empty;
        private string _contact = string.Empty;
        private string _password = string.Empty;
        private string _confirmPass = string.Empty;
        private string _securityQuestion = "What is your pet name?";
        private string _securityAnswer = string.Empty;
        private string _errorMessage = string.Empty;
        private bool _errorVisible = false;
        private bool _isLoading = false;

        private string _profilePicturePath = string.Empty;
        public string ProfilePicturePath
        {
            get => _profilePicturePath;
            set { _profilePicturePath = value; OnPropertyChanged(); OnPropertyChanged(nameof(HasPicture)); }
        }
        public bool HasPicture =>
            !string.IsNullOrEmpty(_profilePicturePath) && System.IO.File.Exists(_profilePicturePath);

        // Donor-specific
        private string _username = string.Empty;

        // Beneficiary-specific
        private string _sex = "Male";
        private string _selectedOrgId = string.Empty;
        private string _selectedOrgName = string.Empty;

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
        public string Password { get => _password; set { _password = value; OnPropertyChanged(); } }
        public string ConfirmPass { get => _confirmPass; set { _confirmPass = value; OnPropertyChanged(); } }
        public string SecurityQuestion { get => _securityQuestion; set { _securityQuestion = value; OnPropertyChanged(); } }
        public string SecurityAnswer { get => _securityAnswer; set { _securityAnswer = value; OnPropertyChanged(); } }
        public string Sex { get => _sex; set { _sex = value; OnPropertyChanged(); } }

        public string SelectedOrgId
        {
            get => _selectedOrgId;
            set { _selectedOrgId = value; OnPropertyChanged(); }
        }
        public string SelectedOrgName
        {
            get => _selectedOrgName;
            set { _selectedOrgName = value; OnPropertyChanged(); }
        }

        public string ErrorMessage { get => _errorMessage; set { _errorMessage = value; OnPropertyChanged(); } }
        public bool ErrorVisible { get => _errorVisible; set { _errorVisible = value; OnPropertyChanged(); } }
        public bool IsLoading { get => _isLoading; set { _isLoading = value; OnPropertyChanged(); } }

        public bool IsDonor => _role == "Donor";
        public bool IsBeneficiary => _role == "Beneficiary";
        public string RoleLabel => $"Create {_role} Account";

        public System.Collections.ObjectModel.ObservableCollection<(string Id, string Name)> Organizations { get; } = new();

        public ICommand RegisterCommand { get; }
        public ICommand BackCommand { get; }

        public ICommand BrowsePictureCommand { get; }

        public SignUpViewModel(string role)
        {
            _role = role;

            BackCommand = new RelayCommand(_ =>
            {
                if (role == "Donor")
                    NavigationService.Navigate(new View.DonorLoginWindow());
                else
                    NavigationService.Navigate(new View.BeneficiaryLoginWindow());
            });

            RegisterCommand = new AsyncRelayCommand(async _ =>
            {
                ErrorVisible = false;

                // Validate common fields
                if (string.IsNullOrWhiteSpace(FName))
                { ShowError("First name is required."); return; }
                if (string.IsNullOrWhiteSpace(LName))
                { ShowError("Last name is required."); return; }
                if (string.IsNullOrWhiteSpace(Contact) || Contact.Length != 11 || !Contact.StartsWith("09"))
                { ShowError("Contact must be 11 digits starting with 09."); return; }
                if (string.IsNullOrWhiteSpace(Password) || Password.Length < 6)
                { ShowError("Password must be at least 6 characters."); return; }
                if (Password.Contains(" "))
                { ShowError("Password cannot contain spaces."); return; }
                if (Password != ConfirmPass)
                { ShowError("Passwords do not match."); return; }
                if (string.IsNullOrWhiteSpace(SecurityAnswer))
                { ShowError("Security answer is required."); return; }

                if (_role == "Donor")
                {
                    if (string.IsNullOrWhiteSpace(Username))
                    { ShowError("Username is required."); return; }

                    var confirm = MessageBox.Show(
                        $"Register as Donor?\n\nName: {FName} {LName}\nUsername: {Username}\nContact: {Contact}",
                        "Confirm Registration", MessageBoxButton.YesNo, MessageBoxImage.Question);
                    if (confirm != MessageBoxResult.Yes) return;

                    try
                    {
                        IsLoading = true;
                        string id = await KapwaDataService.GetNextDonorId();
                        var donor = new DonorModel
                        {
                            Donor_ID = id,
                            Donor_FullName = $"{FName} {LName}",
                            Donor_Username = Username,
                            Donor_ContactNumber = Contact,
                            ProfilePicturePath = ProfilePicturePath  // ADD THIS LINE
                        };
                        await KapwaDataService.RegisterDonor(donor, Password, SecurityQuestion, SecurityAnswer);
                        MessageBox.Show($"✅ Registered! Your Donor ID: {id}\nLogin with username: {Username}",
                            "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                        NavigationService.Navigate(new View.DonorLoginWindow());
                    }
                    catch { /* error shown by service */ }
                    finally { IsLoading = false; }
                }
                else // Beneficiary
                {
                    if (string.IsNullOrWhiteSpace(SelectedOrgId))
                    { ShowError("Please select an organization."); return; }

                    var confirm = MessageBox.Show(
                        $"Register as Beneficiary?\n\nName: {FName} {LName}\nOrg: {SelectedOrgName}\nContact: {Contact}",
                        "Confirm Registration", MessageBoxButton.YesNo, MessageBoxImage.Question);
                    if (confirm != MessageBoxResult.Yes) return;

                    try
                    {
                        IsLoading = true;
                        string id = await KapwaDataService.GetNextBeneficiaryId();
                        var bene = new BeneficiaryModel
                        {
                            Beneficiary_ID = id,
                            Beneficiary_FName = FName,
                            Beneficiary_LName = LName,
                            Beneficiary_Sex = Sex,
                            Beneficiary_Contact = Contact,
                            Organization_ID = SelectedOrgId,
                            ProfilePicturePath = ProfilePicturePath  // ADD THIS LINE
                        };
                        await KapwaDataService.RegisterBeneficiary(bene, Password, SecurityQuestion, SecurityAnswer);
                        MessageBox.Show($"✅ Registered! Your Beneficiary ID: {id}\nLogin with ID: {id}",
                            "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                        NavigationService.Navigate(new View.BeneficiaryLoginWindow());
                    }
                    catch { /* error shown by service */ }
                    finally { IsLoading = false; }
                }
            });

            BrowsePictureCommand = new RelayCommand(_ =>
            {
                var dlg = new Microsoft.Win32.OpenFileDialog
                {
                    Filter = "Image files (*.png;*.jpg;*.jpeg;*.bmp)|*.png;*.jpg;*.jpeg;*.bmp",
                    Title = "Select Profile Picture"
                };
                if (dlg.ShowDialog() == true) ProfilePicturePath = dlg.FileName;
            });

            if (_role == "Beneficiary")
                LoadOrganizations();
        }

        private async void LoadOrganizations()
        {
            var orgs = await KapwaDataService.GetAllOrganizations();
            Application.Current.Dispatcher.Invoke(() =>
            {
                Organizations.Clear();
                foreach (var o in orgs) Organizations.Add(o);
            });
        }

        private void ShowError(string msg) { ErrorMessage = msg; ErrorVisible = true; }
        private void ClearError() { if (_errorVisible) { ErrorMessage = string.Empty; ErrorVisible = false; } }
    }
}