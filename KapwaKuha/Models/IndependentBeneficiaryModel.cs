// FILE: Models/IndependentBeneficiaryModel.cs  (NEW — for finals)
using KapwaKuha.ViewModels;

namespace KapwaKuha.Models
{
    public class IndependentBeneficiaryModel : ObservableObject
    {
        public string IndepBene_ID { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Sex { get; set; } = "Male";
        public string ContactNumber { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;

        private string _accountStatus = "Active";
        public string AccountStatus
        {
            get => _accountStatus;
            set { _accountStatus = value; OnPropertyChanged(); }
        }

        public string ProfilePicturePath { get; set; } = string.Empty;
        public string SecurityQuestion { get; set; } = "What is your pet name?";
        public string SecurityAnswer { get; set; } = string.Empty;

        private string _adminApprovalStatus = "Pending";
        public string Admin_Approval_Status
        {
            get => _adminApprovalStatus;
            set { _adminApprovalStatus = value; OnPropertyChanged(); }
        }

        public string Email { get; set; } = string.Empty;

        public string DisplayName => $"{FullName} (Independent)";
        public bool HasPicture =>
            !string.IsNullOrEmpty(ProfilePicturePath) &&
            System.IO.File.Exists(ProfilePicturePath);
    }
}