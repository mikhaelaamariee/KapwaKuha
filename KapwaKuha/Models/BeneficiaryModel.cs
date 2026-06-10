// FILE: Models/BeneficiaryModel.cs
using KapwaKuha.ViewModels;

namespace KapwaKuha.Models
{
    public class BeneficiaryModel : ObservableObject
    {
        public string Beneficiary_ID { get; set; } = string.Empty;
        public string Beneficiary_FullName { get; set; } = string.Empty;
        public string Beneficiary_FName { get; set; } = string.Empty;
        public string Beneficiary_LName { get; set; } = string.Empty;
        public System.DateTime? Beneficiary_Birthdate { get; set; }
        public string Beneficiary_Sex { get; set; } = string.Empty;
        public string Beneficiary_Contact { get; set; } = string.Empty;


        public string Beneficiary_Username { get; set; } = string.Empty ;
        public string Beneficiary_Password { get; set; } = string.Empty;
        public string ProfilePicturePath { get; set; } = string.Empty;

        private string _status = "Active";

        public string Organization_Address { get; set; } = string.Empty;
        public string Organization_Contact { get; set; } = string.Empty;
        public string Beneficiaries_Status
        {
            get => _status;
            set { _status = value; OnPropertyChanged(); }
        }

        public string Organization_ID { get; set; } = string.Empty;
        public string Organization_Name { get; set; } = string.Empty;
        public string SecurityQuestion { get; set; } = "What is your pet name?";
        public string SecurityAnswer { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string DisplayName =>
            $"{(string.IsNullOrWhiteSpace(Beneficiary_FullName)
                ? $"{Beneficiary_FName} {Beneficiary_LName}".Trim()
                : Beneficiary_FullName)} — {Organization_Name}";
    }
}