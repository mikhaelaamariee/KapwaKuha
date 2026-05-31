using KapwaKuha.ViewModels;
using KapwaKuha.Services;
using System.Windows;

namespace KapwaKuha.View
{
    public partial class AdminLoginWindow : Window
    {
        public AdminLoginWindow()
        {
            InitializeComponent();
            DataContext = new AdminLoginViewModel();
            Loaded += (s, e) => NavigationService.SetCurrent(this);
        }

        private void PwBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            PlaceholderText.Visibility =
                string.IsNullOrEmpty(PwBox.Password)
                ? Visibility.Visible
                : Visibility.Collapsed;
        }

        private void LoginBtn_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is AdminLoginViewModel vm)
                vm.LoginCommand.Execute(PwBox);
        }
    }
}