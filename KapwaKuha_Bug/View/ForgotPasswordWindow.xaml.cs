using System.Windows;
using KapwaKuha.Services;
using KapwaKuha.ViewModels;

namespace KapwaKuha.View
{
    public partial class ForgotPasswordWindow : Window
    {
        public ForgotPasswordWindow(string role)
        {
            InitializeComponent();
            DataContext = new ForgotPasswordViewModel(role);
            Loaded += (s, e) => NavigationService.SetCurrent(this);
        }

        private void ResetBtn_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is ForgotPasswordViewModel vm)
            {
                vm.NewPassword = NewPwBox.Password;
                vm.ConfirmPassword = ConfirmPwBox.Password;
                vm.ResetPasswordCommand.Execute(null);
            }
        }
    }
}