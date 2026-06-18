// Services/EmailService.cs
using System;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;

namespace KapwaKuha.Services
{
    /// <summary>
    /// Sends HTML emails via Gmail SMTP (App Password required).
    /// Configure SmtpHost / credentials to match your provider.
    /// </summary>
    public static class EmailService
    {
        // ── Configure these ───────────────────────────────────────────────────
        private const string SmtpHost = "smtp.gmail.com";
        private const int SmtpPort = 587;
        private const string SenderEmail = "kapwakuha.notify@gmail.com";   // your Gmail
        private const string SenderName = "KapwaKuha";
        // Store the App Password in an environment variable or config, NOT hardcoded:
        private static readonly string AppPassword =
            Environment.GetEnvironmentVariable("KAPWA_SMTP_PASS") ?? "";

        public static async Task SendAsync(string toEmail, string subject, string body)
        {
            if (string.IsNullOrWhiteSpace(AppPassword))
            {
                System.Diagnostics.Debug.WriteLine("[EmailService] KAPWA_SMTP_PASS not set — skipping email.");
                return;
            }

            try
            {
                using var client = new SmtpClient(SmtpHost, SmtpPort)
                {
                    Credentials = new NetworkCredential(SenderEmail, AppPassword),
                    EnableSsl = true,
                    DeliveryMethod = SmtpDeliveryMethod.Network,
                    Timeout = 10_000
                };

                using var mail = new MailMessage
                {
                    From = new MailAddress(SenderEmail, SenderName),
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = true
                };
                mail.To.Add(toEmail);

                await client.SendMailAsync(mail);
                System.Diagnostics.Debug.WriteLine($"[EmailService] Sent to {toEmail}: {subject}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[EmailService] Error: {ex.Message}");
                // Don't rethrow — notification failure shouldn't crash the app
            }
        }
    }
}