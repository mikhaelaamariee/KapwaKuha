using KapwaKuha.ViewModels;
using KapwaKuha.Services;
using System.Windows;

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
                vm.Password = PwBox.Password;
                vm.ConfirmPass = ConfirmPwBox.Password;
                vm.RegisterCommand.Execute(null);
            }
        }
    }
}