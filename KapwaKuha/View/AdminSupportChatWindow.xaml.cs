// FILE: View/AdminSupportChatWindow.xaml.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using KapwaKuha.Models;
using KapwaKuha.Services;

namespace KapwaKuha.View
{
    public partial class AdminSupportChatWindow : Window
    {
        private readonly string _userId;
        private readonly string _role;
        private readonly bool _adminMode;

        // ── Auto-reply rules: keyword → response ─────────────────────────────
        private static readonly (string[] Keywords, string Reply)[] _autoReplies = new[]
        {
            (new[] { "help", "assist", "support" },
             "👋 Hi! I'm the KapwaKuha support bot. I can help you with donations, claims, and account issues. " +
             "Please describe your concern and our admin team will get back to you shortly."),

            (new[] { "claim", "pickup", "collect" },
             "📦 For claim-related concerns, please check your **Claim Tracker** in your dashboard. " +
             "If an item shows 'Pending Release', your donor has been notified. " +
             "Still stuck? Describe the issue and an admin will assist."),

            (new[] { "donation", "donate", "item", "post" },
             "🎁 For donation or item posting questions, make sure your item has been approved by an admin before it appears publicly. " +
             "Check the status badge on your Active Listings."),

            (new[] { "account", "login", "password", "ban", "suspended" },
             "🔐 For account issues like login problems or a suspended account, please provide your username " +
             "and a brief description. An admin will review your case within 24 hours."),

            (new[] { "approval", "pending", "review", "waiting" },
             "⏳ Items and needs posts are reviewed by our admin team. Approval typically takes less than 24 hours. " +
             "If it's been longer, please share your post title and we'll look into it."),

            (new[] { "rating", "star", "feedback", "review" },
             "⭐ Donor ratings are given by beneficiaries after a successful handoff is marked as Released. " +
             "You can view your rating on your profile."),

            (new[] { "organization", "org", "institution", "register" },
             "🏫 To register as an Institutional Beneficiary, your account must use a valid organizational email. " +
             "Make sure your details match your organization's official records."),

            (new[] { "report", "scam", "fraud", "fake" },
             "🚩 To report a user, open their profile and click **Report User**. " +
             "Provide a detailed description and any proof. Admins will review within 48 hours."),

            (new[] { "thank", "thanks", "salamat", "ty" },
             "😊 You're welcome! Is there anything else I can help you with?"),
        };

        public AdminSupportChatWindow(string userId, string role, bool adminMode = false)
        {
            InitializeComponent();
            _userId = userId;
            _role = role;
            _adminMode = adminMode;
            _ = LoadMessages();
        }

        private async Task LoadMessages()
        {
            var msgs = await KapwaDataService.GetAdminSupportMessages(_userId);
            Application.Current.Dispatcher.Invoke(() =>
            {
                MessagesList.ItemsSource = null;
                var items = new List<SupportChatItem>();
                foreach (var m in msgs)
                {
                    bool fromMe = _adminMode ? (m.SenderId == "A001") : m.IsFromUser;
                    items.Add(new SupportChatItem
                    {
                        Text = m.Text,
                        TimeLabel = m.Time,
                        IsFromUser = fromMe,
                        HAlignment = fromMe ? HorizontalAlignment.Right : HorizontalAlignment.Left,
                        SenderLabel = fromMe ? "You" : (m.SenderId == "A001" ? "KapwaKuha Support" : m.SenderId),
                    });
                }
                MessagesList.ItemsSource = items;
                ScrollView.ScrollToEnd();
            });
        }

        private async void SendBtn_Click(object sender, RoutedEventArgs e) => await SendMessage();
        private async void InputBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter) await SendMessage();
        }

        private async Task SendMessage()
        {
            string text = InputBox.Text.Trim();
            if (string.IsNullOrEmpty(text)) return;
            InputBox.Text = string.Empty;

            string senderId = _adminMode ? "A001" : _userId;
            string receiverId = _adminMode ? _userId : "A001";

            // Save the user/admin message
            await KapwaDataService.SaveChatMessage(senderId, receiverId, text);
            System.Diagnostics.Debug.WriteLine($"Saved message from {senderId} to {receiverId}: {text}");

            // Auto-reply only when a user (not admin) sends
            if (!_adminMode)
            {
                string? autoReply = GetAutoReply(text);
                if (autoReply != null)
                {
                    System.Diagnostics.Debug.WriteLine($"Auto-reply triggered: {autoReply}");

                    // Small delay so reply feels natural
                    await Task.Delay(1000);

                    // Save and await the auto-reply before loading messages
                    await KapwaDataService.SaveChatMessage("A001", _userId, autoReply);
                    System.Diagnostics.Debug.WriteLine($"Saved auto-reply from A001 to {_userId}");
                }
            }

            // Load and display all messages
            await LoadMessages();
        }

        /// <summary>
        /// Checks the user's message for known keywords and returns an automated reply, or null if none match.
        /// </summary>
        private static string? GetAutoReply(string userMessage)
        {
            string lower = userMessage.ToLowerInvariant();
            foreach (var (keywords, reply) in _autoReplies)
            {
                if (keywords.Any(k => lower.Contains(k)))
                    return reply;
            }
            return null;
        }

        private void BackBtn_Click(object sender, RoutedEventArgs e) => Close();
    }

    public class SupportChatItem
    {
        public string Text { get; set; } = string.Empty;
        public string TimeLabel { get; set; } = string.Empty;
        public bool IsFromUser { get; set; }
        public HorizontalAlignment HAlignment { get; set; }
        public string SenderLabel { get; set; } = string.Empty;
    }
}