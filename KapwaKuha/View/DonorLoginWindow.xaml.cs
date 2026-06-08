using KapwaKuha.ViewModels;
using KapwaKuha.Services;
using System.Windows;
using System.Windows.Controls;

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
            PlaceholderText.Visibility = string.IsNullOrEmpty(PwBox.Password)
                ? Visibility.Visible : Visibility.Collapsed;
        }

        private void ShowPwCheck_Checked(object sender, RoutedEventArgs e)
        {
            PwTextBox.Text = PwBox.Password;
            PwTextBox.Visibility = Visibility.Visible;
            PwBox.Visibility = Visibility.Collapsed;
            PlaceholderText.Visibility = Visibility.Collapsed;
        }

        private void ShowPwCheck_Unchecked(object sender, RoutedEventArgs e)
        {
            PwBox.Password = PwTextBox.Text;
            PwBox.Visibility = Visibility.Visible;
            PwTextBox.Visibility = Visibility.Collapsed;
            PlaceholderText.Visibility = string.IsNullOrEmpty(PwBox.Password)
                ? Visibility.Visible : Visibility.Collapsed;
        }

        private void LoginBtn_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is LoginViewModel vm)
            {
                object param = PwBox.Visibility == Visibility.Visible
                    ? (object)PwBox : PwTextBox.Text;
                vm.LoginCommand.Execute(param);
            }
        }
    }
}