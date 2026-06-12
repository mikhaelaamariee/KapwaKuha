// FILE: View/AdminLoginWindow.xaml.cs
using KapwaKuha.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace KapwaKuha.View
{
    public partial class AdminLoginWindow : Window
    {
        private bool _showingPassword = false;

        public AdminLoginWindow()
        {
            InitializeComponent();
            DataContext = new AdminLoginViewModel();
        }

        private void PwBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (DataContext is AdminLoginViewModel vm)
            {
                // Keep PasswordBox placeholder in sync
                PlaceholderText.Visibility = PwBox.Password.Length == 0
                    ? Visibility.Visible : Visibility.Collapsed;
            }
            // Keep TextBox in sync if visible
            if (_showingPassword)
                PwTextBox.Text = PwBox.Password;
        }

        private void TogglePwBtn_Click(object sender, RoutedEventArgs e)
        {
            _showingPassword = !_showingPassword;
            if (_showingPassword)
            {
                PwTextBox.Text = PwBox.Password;
                PwBox.Visibility = Visibility.Collapsed;
                PwTextBox.Visibility = Visibility.Visible;
                TogglePwBtn.Content = "🙈";
                TogglePwBtn.ToolTip = "Hide password";
            }
            else
            {
                PwBox.Password = PwTextBox.Text;
                PwTextBox.Visibility = Visibility.Collapsed;
                PwBox.Visibility = Visibility.Visible;
                TogglePwBtn.Content = "👁";
                TogglePwBtn.ToolTip = "Show password";
            }
        }

        private void LoginBtn_Click(object sender, RoutedEventArgs e)
        {
            // Always pull from PwBox (sync'd with PwTextBox in toggle)
            if (_showingPassword)
                PwBox.Password = PwTextBox.Text;

            if (DataContext is AdminLoginViewModel vm)
                vm.LoginCommand.Execute(PwBox);
        }
    }
}