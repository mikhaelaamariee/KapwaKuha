using KapwaKuha.ViewModels;
using KapwaKuha.Services;
using System.Windows;
using System.Windows.Controls;

namespace KapwaKuha.View
{
    public partial class IndependentBeneficiarySignUpWindow : Window
    {
        public IndependentBeneficiarySignUpWindow()
        {
            InitializeComponent();
            DataContext = new IndependentBeneficiarySignUpViewModel();
            Loaded += (s, e) => NavigationService.SetCurrent(this);
        }

        private void RegisterBtn_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is IndependentBeneficiarySignUpViewModel vm)
            {
                vm.Password = PwBox.Visibility == Visibility.Visible
                    ? PwBox.Password : PwTextBox.Text;
                vm.ConfirmPass = ConfirmPwBox.Visibility == Visibility.Visible
                    ? ConfirmPwBox.Password : ConfirmPwTextBox.Text;
                vm.RegisterCommand.Execute(null);
            }
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
                ShowPwIcon.Text = "🙈";
            }
            else
            {
                PwBox.Password = PwTextBox.Text;
                PwBox.Visibility = Visibility.Visible;
                PwTextBox.Visibility = Visibility.Collapsed;
                ShowPwIcon.Text = "👁";
            }
        }

        private bool _confirmPwVisible = false;
        private void ShowConfirmPwBtn_Click(object sender, RoutedEventArgs e)
        {
            _confirmPwVisible = !_confirmPwVisible;
            if (_confirmPwVisible)
            {
                ConfirmPwTextBox.Text = ConfirmPwBox.Password;
                ConfirmPwTextBox.Visibility = Visibility.Visible;
                ConfirmPwBox.Visibility = Visibility.Collapsed;
                ShowConfirmPwIcon.Text = "🙈";
            }
            else
            {
                ConfirmPwBox.Password = ConfirmPwTextBox.Text;
                ConfirmPwBox.Visibility = Visibility.Visible;
                ConfirmPwTextBox.Visibility = Visibility.Collapsed;
                ShowConfirmPwIcon.Text = "👁";
            }
        }
    }
}