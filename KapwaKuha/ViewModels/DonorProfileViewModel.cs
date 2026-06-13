// FILE: ViewModels/DonorProfileViewModel.cs
using System.Windows;
using System.Windows.Input;
using KapwaKuha.Commands;
using KapwaKuha.Services;

namespace KapwaKuha.ViewModels
{
    public class DonorProfileViewModel : ObservableObject
    {
        private readonly string _donorId;

        private string _fullName = string.Empty;
        private string _username = string.Empty;
        private string _contact = string.Empty;
        private string _address = string.Empty;
        private string _picturePath = string.Empty;
        private bool _isBusy;
        private string _donorStatus = "Active";

    

        private string _email = string.Empty;
        public string Email
        {
            get => _email;
            set { _email = value; OnPropertyChanged(); }
        }

        public string FullName
        {
            get => _fullName;
            set { _fullName = value; OnPropertyChanged(); }
        }
        public string Username
        {
            get => _username;
            set { _username = value; OnPropertyChanged(); }
        }
        public string Contact
        {
            get => _contact;
            set { _contact = value; OnPropertyChanged(); }
        }
        public string Address
        {
            get => _address;
            set { _address = value; OnPropertyChanged(); }
        }

        public string DonorStatus
        {
            get => _donorStatus;
            set { _donorStatus = value; OnPropertyChanged(); }
        }
        public string PicturePath
        {
            get => _picturePath;
            set { _picturePath = value; OnPropertyChanged(); OnPropertyChanged(nameof(HasPicture)); }
        }
        public bool HasPicture => !string.IsNullOrEmpty(PicturePath) && System.IO.File.Exists(PicturePath);
        public bool IsBusy { get => _isBusy; set { _isBusy = value; OnPropertyChanged(); } }

        public ICommand BackCommand { get; }
        public ICommand BrowsePictureCommand { get; }
        public ICommand SaveCommand { get; }

        public ICommand DeactivateCommand { get; }

        public DonorProfileViewModel(string donorId)
        {
            _donorId = donorId;

            BackCommand = new RelayCommand(_ =>
                NavigationService.Navigate(new View.DonorDashboardWindow(_donorId)));

            BrowsePictureCommand = new RelayCommand(_ =>
            {
                var dlg = new Microsoft.Win32.OpenFileDialog
                {
                    Filter = "Image files (*.png;*.jpg;*.jpeg)|*.png;*.jpg;*.jpeg",
                    Title = "Select Profile Picture"
                };
                if (dlg.ShowDialog() == true) PicturePath = dlg.FileName;
            });

            SaveCommand = new AsyncRelayCommand(async _ =>
            {
                if (string.IsNullOrWhiteSpace(Username))
                {
                    MessageBox.Show("Username cannot be empty.", "Validation",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                try
                {
                    IsBusy = true;
                    // FIX: pass Address as 4th argument
                    await KapwaDataService.UpdateDonorProfile(_donorId, Username, PicturePath, Address);
                    UserSession.Username = Username;
                    MessageBox.Show("✅ Profile updated!", "Saved",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch { }
                finally { IsBusy = false; }
            });

            DeactivateCommand = new AsyncRelayCommand(async _ =>
            {
                var confirm = MessageBox.Show(
                    "Are you sure you want to deactivate your account?\n\n" +
                    "You will be logged out and will not be able to log in again until reactivated.",
                    "Deactivate Account",
                    MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (confirm != MessageBoxResult.Yes) return;

                try
                {
                    IsBusy = true;
                    await KapwaDataService.DeactivateAccount(_donorId);
                    MessageBox.Show("Your account has been deactivated.",
                        "Account Deactivated", MessageBoxButton.OK, MessageBoxImage.Information);
                    UserSession.Clear();
                    NavigationService.Navigate(new View.ChooseRoleWindow());
                }
                catch { }
                finally { IsBusy = false; }
            });

            LoadProfile();
        }

        private async void LoadProfile()
        {
            try
            {
                var donor = await KapwaDataService.GetDonorById(_donorId);
                if (donor == null) return;
                FullName = donor.Donor_FullName;
                Username = donor.Donor_Username;
                Contact = donor.Donor_ContactNumber;
                Address = donor.Donor_Address ?? "";
                PicturePath = donor.ProfilePicturePath ?? "";
                DonorStatus = donor.Donor_AccountStatus ?? "Active";
                Email = donor.Email ?? "";
            }
            catch { }
        }
    }
}