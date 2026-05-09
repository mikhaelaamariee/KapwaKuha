using KapwaKuha.ViewModels;
using KapwaKuha.Services;
using System.Windows;

namespace KapwaKuha.View
{
    public partial class BeneficiaryDashboardWindow : Window
    {
        public BeneficiaryDashboardWindow(string beneficiaryId)
        {
            InitializeComponent();
            DataContext = new BeneficiaryDashboardViewModel(beneficiaryId);
            Loaded += (s, e) => NavigationService.SetCurrent(this);
        }

    
    }
}