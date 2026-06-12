using KapwaKuha.ViewModels;
using KapwaKuha.Services;
using System.Windows;
using System.Windows.Controls;

namespace KapwaKuha.View
{
    public partial class IndependentBeneficiaryLoginWindow : Window
    {
        public IndependentBeneficiaryLoginWindow()
        {
            InitializeComponent();
            DataContext = new LoginViewModel("IndependentBeneficiary");
            Loaded += (s, e) => NavigationService.SetCurrent(this);
        }

        private void PwBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            PlaceholderText.Visibility = string.IsNullOrEmpty(PwBox.Password)
                ? Visibility.Visible : Visibility.Collapsed;

            // Keep ViewModel PlainPassword in sync when PwBox is active
            if (DataContext is LoginViewModel vm)
                vm.PlainPassword = PwBox.Password;
        }

        private bool _pwVisible = false;
        private void ShowPwBtn_Click(object sender, RoutedEventArgs e)
        {
            _pwVisible = !_pwVisible;
            if (_pwVisible)
            {
                PwTextBox.Text = PwBox.Password;
                PwTextBox.Visibility = Visibility.Visible;
                PwBox.Visibility = Visibility.Collapsed;
                PlaceholderText.Visibility = Visibility.Collapsed;
                ShowPwIcon.Text = "🙈";
            }
            else
            {
                PwBox.Password = PwTextBox.Text;
                PwBox.Visibility = Visibility.Visible;
                PwTextBox.Visibility = Visibility.Collapsed;
                PlaceholderText.Visibility = string.IsNullOrEmpty(PwBox.Password)
                    ? Visibility.Visible : Visibility.Collapsed;
                ShowPwIcon.Text = "👁";
            }
        }

        private void LoginBtn_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is LoginViewModel vm)
            {
                // Pass whichever control is active
                object param = PwBox.Visibility == Visibility.Visible
                    ? (object)PwBox : PwTextBox.Text;
                vm.LoginCommand.Execute(param);
            }
        }
    }
}