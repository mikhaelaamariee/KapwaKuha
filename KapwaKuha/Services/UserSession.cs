// Services/UserSession.cs
namespace KapwaKuha
{
    /// <summary>
    /// Singleton session state — set at login, read everywhere.
    /// </summary>
    public static class UserSession
    {
        // ── Canonical properties ───────────────────────────────────────────────
        public static string CurrentUserId { get; set; } = string.Empty;
        public static string CurrentRole { get; set; } = string.Empty;
        public static string CurrentUsername { get; set; } = string.Empty;
        public static string CurrentFullName { get; set; } = string.Empty;
        public static string CurrentEmail { get; set; } = string.Empty;
        public static string CurrentPhone { get; set; } = string.Empty;
        public static string NotificationPreference { get; set; } = "Email";

        // ── Short aliases (used across all ViewModels) ─────────────────────────
        public static string UserId { get => CurrentUserId; set => CurrentUserId = value; }
        public static string Username { get => CurrentUsername; set => CurrentUsername = value; }
        public static string FullName { get => CurrentFullName; set => CurrentFullName = value; }
        public static string Role { get => CurrentRole; set => CurrentRole = value; }

        public static bool IsLoggedIn => !string.IsNullOrEmpty(CurrentUserId);

        public static void Clear()
        {
            CurrentUserId = string.Empty;
            CurrentRole = string.Empty;
            CurrentUsername = string.Empty;
            CurrentFullName = string.Empty;
            CurrentEmail = string.Empty;
            CurrentPhone = string.Empty;
            NotificationPreference = "Email";
        }
    }
}