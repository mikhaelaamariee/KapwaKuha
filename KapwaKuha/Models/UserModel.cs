// FILE: Models/UserModel.cs
using KapwaKuha.ViewModels;

namespace KapwaKuha.Models
{
    public class UserModel : ObservableObject
    {
        private string _userID = string.Empty;
        private string _password = string.Empty;
        private string _role = string.Empty;
        private string _email = string.Empty;
        private bool _isBlacklisted;
        private int _strikesCount;
        private string _adminApprovalStatus = "Approved";

        public string UserID
        {
            get => _userID;
            set { _userID = value; OnPropertyChanged(); }
        }
        public string Password
        {
            get => _password;
            set { _password = value; OnPropertyChanged(); }
        }
        // Donor | InstitutionalBeneficiary | IndependentBeneficiary | Admin
        public string Role
        {
            get => _role;
            set { _role = value; OnPropertyChanged(); }
        }
        public string Email
        {
            get => _email;
            set { _email = value; OnPropertyChanged(); }
        }
        public bool IsBlacklisted
        {
            get => _isBlacklisted;
            set { _isBlacklisted = value; OnPropertyChanged(); }
        }
        public int StrikesCount
        {
            get => _strikesCount;
            set { _strikesCount = value; OnPropertyChanged(); }
        }
        public string Admin_Approval_Status
        {
            get => _adminApprovalStatus;
            set { _adminApprovalStatus = value; OnPropertyChanged(); }
        }
    }
}