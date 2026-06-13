// FILE: ViewModels/BeneficiaryProfileViewModel.cs
using System;
using System.Net;
using System.Windows;
using System.Windows.Input;
using KapwaKuha.Commands;
using KapwaKuha.Models;
using KapwaKuha.Services;

namespace KapwaKuha.ViewModels
{
    public class BeneficiaryProfileViewModel : ObservableObject
    {
        private readonly string _beneficiaryId;

        private string _fullName = string.Empty;
        private string _username = string.Empty;
        private string _contact = string.Empty;
        private string _orgName = string.Empty;
        private string _orgAddress = string.Empty;
        private string _orgContact = string.Empty;
        private string _picturePath = string.Empty;
        private bool _isBusy;

        public string FullName { get => _fullName; set { _fullName = value; OnPropertyChanged(); } }
        public string Username { get => _username; set { _username = value; OnPropertyChanged(); } }
        public string Contact { get => _contact; set { _contact = value; OnPropertyChanged(); } }
        public string OrganizationName { get => _orgName; set { _orgName = value; OnPropertyChanged(); } }
        public string OrgAddress { get => _orgAddress; set { _orgAddress = value; OnPropertyChanged(); } }
        public string OrgContact { get => _orgContact; set { _orgContact = value; OnPropertyChanged(); } }
        public string PicturePath { get => _picturePath; set { _picturePath = value; OnPropertyChanged(); OnPropertyChanged(nameof(HasPicture)); } }
        public bool IsBusy { get => _isBusy; set { _isBusy = value; OnPropertyChanged(); } }
        public bool HasPicture => !string.IsNullOrEmpty(_picturePath) && System.IO.File.Exists(_picturePath);
        public string BeneficiaryIdLabel => $"ID: {_beneficiaryId}";

        // Keep OrgName alias so any old XAML binding doesn't break
        public string OrgName { get => _orgName; set { _orgName = value; OnPropertyChanged(); OnPropertyChanged(nameof(OrganizationName)); } }

        private bool _isInstitutional = true;
        public bool IsInstitutional
        {
            get => _isInstitutional;
            set { _isInstitutional = value; OnPropertyChanged(); OnPropertyChanged(nameof(IsIndependent)); }
        }
        public bool IsIndependent => !_isInstitutional;

        private string _address = string.Empty;
        public string Address
        {
            get => _address;
            set { _address = value; OnPropertyChanged(); }
        }


        private string _email = string.Empty;
        public string Email
        {
            get => _email;
            set { _email = value; OnPropertyChanged(); }
        }

        public ICommand BackCommand { get; }
        public ICommand SaveCommand { get; }
        public ICommand BrowsePictureCommand { get; }
        public ICommand DeactivateCommand { get; }
        public ICommand MyBAccountCommand { get; }

        public BeneficiaryProfileViewModel(string beneficiaryId)
        {
            _beneficiaryId = beneficiaryId;

            BackCommand = new RelayCommand(_ =>
                NavigationService.Navigate(new View.BeneficiaryDashboardWindow(_beneficiaryId)));

            BrowsePictureCommand = new RelayCommand(_ =>
            {
                var dlg = new Microsoft.Win32.OpenFileDialog
                {
                    Filter = "Images (*.png;*.jpg;*.jpeg;*.bmp)|*.png;*.jpg;*.jpeg;*.bmp",
                    Title = "Select Profile Picture"
                };
                if (dlg.ShowDialog() == true) PicturePath = dlg.FileName;
            });

            SaveCommand = new AsyncRelayCommand(async _ =>
            {
                try
                {
                    IsBusy = true;
                    if (IsInstitutional)
                    {
                        await KapwaDataService.UpdateBeneficiaryProfile(
                            _beneficiaryId, Username, PicturePath,
                            OrganizationName, OrgAddress, OrgContact);
                    }
                    else
                    {
                        await KapwaDataService.UpdateIndepBeneficiaryProfile(
                            _beneficiaryId, Username, PicturePath, Address);
                    }
                    MessageBox.Show("✅ Profile updated!", "Saved",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Save failed: " + ex.Message,
                        "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                finally { IsBusy = false; }
            });

            DeactivateCommand = new AsyncRelayCommand(async _ =>
            {
                var confirm = MessageBox.Show(
                    "Are you sure you want to deactivate your account?\n\n" +
                    "You will be logged out and cannot log in until reactivated.",
                    "Deactivate Account", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (confirm != MessageBoxResult.Yes) return;
                try
                {
                    IsBusy = true;
                    await KapwaDataService.DeactivateAccount(_beneficiaryId);
                    MessageBox.Show("Your account has been deactivated.",
                        "Account Deactivated", MessageBoxButton.OK, MessageBoxImage.Information);
                    UserSession.Clear();
                    NavigationService.Navigate(new View.ChooseRoleWindow());
                }
                catch { }
                finally { IsBusy = false; }
            });

            MyBAccountCommand = new RelayCommand(_ => { });

            LoadProfile();
        }

        // FILE: ViewModels/BeneficiaryProfileViewModel.cs
        // REPLACE the LoadProfile method entirely:

        private async void LoadProfile()
        {
            try
            {
                var bene = await KapwaDataService.GetBeneficiaryById(_beneficiaryId);
                if (bene != null)
                {
                    FullName = bene.Beneficiary_FullName;
                    Username = bene.Beneficiary_Username;
                    Contact = bene.Beneficiary_Contact;
                    OrganizationName = bene.Organization_Name;
                    OrgAddress = bene.Organization_Address;
                    OrgContact = bene.Organization_Contact;
                    PicturePath = bene.ProfilePicturePath ?? string.Empty;
                    IsInstitutional = true;
                }
                else
                {
                    var (fullName, username, contact, address, picPath, _, indepEmail) =
      await KapwaDataService.GetIndepBeneficiaryById(_beneficiaryId);
                    FullName = fullName;
                    Username = username;
                    Contact = contact;
                    Address = address;
                    PicturePath = picPath;
                    IsInstitutional = false;
                    Email = indepEmail;  // ← taken directly, skips the extra GetUserById call
                }
                // Load email for all bene types
                var user = await KapwaDataService.GetUserById(_beneficiaryId);
                Email = user?.Email ?? "";
            }
            catch { }
        }
    }
}