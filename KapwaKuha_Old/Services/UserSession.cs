namespace KapwaKuha.Services
{
    public static class UserSession
    {
        public static string UserId { get; set; } = string.Empty;
        public static string Username { get; set; } = string.Empty;
        public static string FullName { get; set; } = string.Empty;
        public static string Role { get; set; } = string.Empty; // "Admin" | "Donor" | "Beneficiary"

        public static void Clear()
        {
            UserId = Username = FullName = Role = string.Empty;
        }
    }
}