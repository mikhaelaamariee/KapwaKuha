using KapwaKuha.ViewModels;

namespace KapwaKuha.Models
{
    public class UserModel : ObservableObject
    {
        private string _userID = string.Empty;
        private string _password = string.Empty;
        private string _role = string.Empty;

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

        // | "Donor" | "Beneficiary"
        public string Role
        {
            get => _role;
            set { _role = value; OnPropertyChanged(); }
        }
    }
}