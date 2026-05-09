// FILE: View/BeneficiaryClaimHistoryWindow.xaml.cs
using System.Windows;
using KapwaKuha.ViewModels;

namespace KapwaKuha.View
{
    public partial class BeneficiaryClaimHistoryWindow : Window
    {
        public BeneficiaryClaimHistoryWindow(string beneficiaryId)
        {
            InitializeComponent();
            DataContext = new BeneficiaryClaimHistoryViewModel(beneficiaryId);
            Loaded += (s, e) => Services.NavigationService.SetCurrent(this);
        }
    }
}