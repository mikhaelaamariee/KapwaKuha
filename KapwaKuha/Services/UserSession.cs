// FILE: Services/UserSession.cs  (UPDATED)
namespace KapwaKuha.Services
{
    public static class UserSession
    {
        public static string UserId { get; set; } = string.Empty;
        public static string Username { get; set; } = string.Empty;
        public static string FullName { get; set; } = string.Empty;

        // Donor | InstitutionalBeneficiary | IndependentBeneficiary | Admin
        public static string Role { get; set; } = string.Empty;

        // Convenience booleans used by all ViewModels
        public static bool IsDonor =>
            Role == "Donor";
        public static bool IsInstitutionalBeneficiary =>
            Role == "InstitutionalBeneficiary";
        public static bool IsIndependentBeneficiary =>
            Role == "IndependentBeneficiary";
        public static bool IsAnyBeneficiary =>
            IsInstitutionalBeneficiary || IsIndependentBeneficiary;
        public static bool IsAdmin =>
            Role == "Admin";

        public static void Clear() =>
            UserId = Username = FullName = Role = string.Empty;
    }
}