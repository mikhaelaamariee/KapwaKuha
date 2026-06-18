// Services/NotificationManager.cs
using System;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;

namespace KapwaKuha.Services
{
    /// <summary>
    /// Central hub: saves to DB, sends email, shows toast — based on user preference.
    /// Usage:
    ///   await NotificationManager.TriggerNotificationAsync(userId, role, title, message,
    ///       email, phone, preference, notifType, referenceId);
    /// </summary>
    public static class NotificationManager
    {
        public static async Task TriggerNotificationAsync(
            string userId,
            string role,
            string title,
            string message,
            string? email = null,
            string? phone = null,
            string preference = "Email",   // "Email" | "SMS" | "Both"
            string notifType = "AccountAlert",
            string referenceId = "")
        {
            // 1. Always save to DB
            await SaveToDbAsync(userId, role, title, message, notifType, referenceId);

            // 2. Show Windows toast (fire-and-forget on UI thread)
            ShowToast(title, message);

            // 3. Email / SMS based on preference
            bool sendEmail = preference == "Email" || preference == "Both";
            bool sendSms = preference == "SMS" || preference == "Both";

            if (sendEmail && !string.IsNullOrWhiteSpace(email))
            {
                await EmailService.SendAsync(
                    toEmail: email,
                    subject: $"KapwaKuha — {title}",
                    body: BuildEmailBody(title, message));
            }

            if (sendSms && !string.IsNullOrWhiteSpace(phone))
            {
                // Plug in your SMS provider here (e.g. Vonage / Semaphore PH)
                // await SmsService.SendAsync(phone, message);
            }
        }

        // ── DB ────────────────────────────────────────────────────────────────
        private static async Task SaveToDbAsync(
            string userId, string role, string title,
            string message, string notifType, string referenceId)
        {
            try
            {
                await using var conn = new SqlConnection(KapwaDataService.GetConnectionString());
                await conn.OpenAsync();
                await using var cmd = new SqlCommand("sp_InsertNotification", conn)
                {
                    CommandType = System.Data.CommandType.StoredProcedure
                };
                cmd.Parameters.AddWithValue("@RecipientId", userId);
                cmd.Parameters.AddWithValue("@TargetRole", role);
                cmd.Parameters.AddWithValue("@Title", title);
                cmd.Parameters.AddWithValue("@Message", message);
                cmd.Parameters.AddWithValue("@NotifType", notifType);
                cmd.Parameters.AddWithValue("@ReferenceId", referenceId);
                await cmd.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[NotificationManager] DB error: {ex.Message}");
            }
        }

        // ── TOAST ─────────────────────────────────────────────────────────────
        private static void ShowToast(string title, string message)
        {
            try
            {
                // Run on UI thread — WPF toast via MessageQueue or custom toast window
                System.Windows.Application.Current?.Dispatcher.Invoke(() =>
                {
                    ToastPopupService.Show(title, message);
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[NotificationManager] Toast error: {ex.Message}");
            }
        }

        // ── EMAIL BODY ────────────────────────────────────────────────────────
        private static string BuildEmailBody(string title, string body)
        {
            return $"""
                <!DOCTYPE html>
                <html>
                <body style="font-family:Segoe UI,Arial,sans-serif;background:#f0f4f8;padding:30px;">
                  <div style="max-width:520px;margin:auto;background:white;border-radius:12px;padding:32px;box-shadow:0 2px 12px rgba(0,0,0,.1);">
                    <h2 style="color:#0077B6;margin-top:0;">{title}</h2>
                    <p style="color:#374151;font-size:15px;line-height:1.6;">{body}</p>
                    <hr style="border:none;border-top:1px solid #e5e7eb;margin:24px 0;"/>
                    <p style="color:#9ca3af;font-size:12px;">
                      This is an automated message from <strong>KapwaKuha</strong>.<br/>
                      Do not reply to this email.
                    </p>
                  </div>
                </body>
                </html>
                """;
        }
    }
}