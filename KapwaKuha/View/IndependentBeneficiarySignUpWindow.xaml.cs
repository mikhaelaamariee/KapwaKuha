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

        private void ShowPwCheck_Checked(object sender, RoutedEventArgs e)
        {
            PwTextBox.Text = PwBox.Password;
            PwTextBox.Visibility = Visibility.Visible;
            PwBox.Visibility = Visibility.Collapsed;
        }

        private void ShowPwCheck_Unchecked(object sender, RoutedEventArgs e)
        {
            PwBox.Password = PwTextBox.Text;
            PwBox.Visibility = Visibility.Visible;
            PwTextBox.Visibility = Visibility.Collapsed;
        }

        private void ShowConfirmPwCheck_Checked(object sender, RoutedEventArgs e)
        {
            ConfirmPwTextBox.Text = ConfirmPwBox.Password;
            ConfirmPwTextBox.Visibility = Visibility.Visible;
            ConfirmPwBox.Visibility = Visibility.Collapsed;
        }

        private void ShowConfirmPwCheck_Unchecked(object sender, RoutedEventArgs e)
        {
            ConfirmPwBox.Password = ConfirmPwTextBox.Text;
            ConfirmPwBox.Visibility = Visibility.Visible;
            ConfirmPwTextBox.Visibility = Visibility.Collapsed;
        }
    }
}