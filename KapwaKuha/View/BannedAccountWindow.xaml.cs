// FILE: View/BannedAccountWindow.xaml.cs
using System.Windows;

namespace KapwaKuha.View
{
    public partial class BannedAccountWindow : Window
    {
        public string Reason { get; }
        public string StrikesText { get; }
        public string BanStatus { get; }
        public bool IsTotallyBanned { get; }

        public BannedAccountWindow(string reason, int strikes)
        {
            IsTotallyBanned = strikes >= 3;
            Reason = reason;

            if (IsTotallyBanned)
            {
                StrikesText = "⛔ TOTALLY BANNED";
                BanStatus = "Your account has been permanently banned due to repeated policy violations.";
            }
            else
            {
                StrikesText = $"⚠️ Strikes accumulated: {strikes} / 3";
                BanStatus = "You have received a strike on your account for violating community guidelines.";
            }

            InitializeComponent();
            DataContext = this;
        }

        private void Close_Click(object sender, RoutedEventArgs e) => Close();
    }
}