using System.Windows;
using KapwaKuha.Services;
using KapwaKuha.ViewModels;

namespace KapwaKuha.View
{
    public partial class SignUpWindow : Window
    {
        public SignUpWindow(string role)
        {
            InitializeComponent();
            DataContext = new SignUpViewModel(role);
            Loaded += (s, e) => NavigationService.SetCurrent(this);
        }

        // PasswordBox cannot bind directly — push to ViewModel before executing command
        private void RegisterBtn_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is SignUpViewModel vm)
            {
                vm.Password = PwBox.Password;
                vm.ConfirmPass = ConfirmPwBox.Password;

                // We deleted the OrgCombo and SelectedOrgId logic here because 
                // the TextBox in XAML automatically handles SelectedOrgName!

                vm.RegisterCommand.Execute(null);
            }
        }
    }
}