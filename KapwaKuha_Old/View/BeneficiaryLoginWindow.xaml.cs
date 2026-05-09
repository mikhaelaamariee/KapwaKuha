using KapwaKuha.ViewModels;
using KapwaKuha.Services;
using System.Windows;
using System.Windows.Controls; 

namespace KapwaKuha.View
{
    public partial class BeneficiaryLoginWindow : Window
    {
        public BeneficiaryLoginWindow()
        {
            InitializeComponent();
            DataContext = new LoginViewModel("Beneficiary");
            Loaded += (s, e) => NavigationService.SetCurrent(this);
        }
        private void PwBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            // If the password box has text, hide the placeholder. Otherwise, show it.
            var placeholder = this.FindName("PlaceholderText") as UIElement;
            var pwBox = sender as PasswordBox;
            if (placeholder != null && pwBox != null)
            {
                if (string.IsNullOrEmpty(pwBox.Password))
                {
                    placeholder.Visibility = Visibility.Visible;
                }
                else
                {
                    placeholder.Visibility = Visibility.Collapsed;
                }
            }
        }
    }
}