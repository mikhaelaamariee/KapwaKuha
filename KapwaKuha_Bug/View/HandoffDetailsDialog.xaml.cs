// FILE: View/HandoffDetailsDialog.xaml.cs
using System;
using System.Windows;

namespace KapwaKuha.View
{
    public partial class HandoffDetailsDialog : Window
    {
        public string HandoffType { get; private set; } = "Pickup";
        public string Location { get; private set; } = "";
        public string EventName { get; private set; } = "";
        public DateTime? EventDate { get; private set; }

        public HandoffDetailsDialog(string itemName, string nextStatus, string currentHandoff)
        {
            InitializeComponent();
            TitleText.Text = $"Advance \"{itemName}\" → {nextStatus}";
            HandoffCombo.SelectedIndex = currentHandoff switch
            {
                "Delivery" => 1,
                "Donation Drive" => 2,
                _ => 0
            };
        }

        private void OK_Click(object sender, RoutedEventArgs e)
        {
            HandoffType = HandoffCombo.SelectedIndex switch
            {
                1 => "Delivery",
                2 => "Donation Drive",
                _ => "Pickup"
            };
            Location = LocationBox.Text.Trim();
            EventName = EventNameBox.Text.Trim();
            if (DateTime.TryParse(EventDateBox.Text.Trim(), out var dt))
                EventDate = dt;
            DialogResult = true;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;
    }
}