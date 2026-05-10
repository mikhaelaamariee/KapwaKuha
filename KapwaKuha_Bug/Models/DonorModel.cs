// FILE: DonorModel.cs
// DB Table: Donors — Strong Entity (parallel to CustomerModel)
using KapwaKuha.ViewModels;

namespace KapwaKuha.Models
{
    public class DonorModel : ObservableObject
    {
        public string Donor_ID { get; set; } = string.Empty;

        private string _fullName = string.Empty;
        public string Donor_FullName
        {
            get => _fullName;
            set { _fullName = value; OnPropertyChanged(); }
        }

        private string _username = string.Empty;
        public string Donor_Username
        {
            get => _username;
            set { _username = value; OnPropertyChanged(); }
        }

        public string Donor_Address { get; set; } = string.Empty;
        public string Donor_ContactNumber { get; set; } = string.Empty;
        public string Donor_Password { get; set; } = string.Empty;

        private string _status = "Active";
        public string Donor_AccountStatus
        {
            get => _status;
            set { _status = value; OnPropertyChanged(); }
        }

        public string ProfilePicturePath { get; set; } = string.Empty;
        public string SecurityQuestion { get; set; } = "What is your pet name?";
        public string SecurityAnswer { get; set; } = string.Empty;
    }
}