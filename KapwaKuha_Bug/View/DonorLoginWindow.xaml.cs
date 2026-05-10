using KapwaKuha.ViewModels;
using KapwaKuha.Services;
using System.Windows;

namespace KapwaKuha.View
{
    public partial class DonorLoginWindow : Window
    {
        public DonorLoginWindow()
        {
            InitializeComponent();
            DataContext = new LoginViewModel("Donor");
            Loaded += (s, e) => NavigationService.SetCurrent(this);
        }
        private void PwBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            // If the password box has text, hide the placeholder. Otherwise, show it.
            if (string.IsNullOrEmpty(PwBox.Password))
            {
                PlaceholderText.Visibility = Visibility.Visible;
            }
            else
            {
                PlaceholderText.Visibility = Visibility.Collapsed;
            }
        }
    }
}