// FILE: View/BeneficiaryClaimTrackerWindow.xaml.cs
using System.Windows;
using KapwaKuha.Services;
using KapwaKuha.ViewModels;

namespace KapwaKuha.View
{
    public partial class BeneficiaryClaimTrackerWindow : Window
    {
        public BeneficiaryClaimTrackerWindow(string beneficiaryId)
        {
            InitializeComponent();
            DataContext = new ClaimTrackerViewModel(beneficiaryId, "Beneficiary");
            Loaded += (s, e) => NavigationService.SetCurrent(this);
        }
    }
}