using KapwaKuha.ViewModels;
using KapwaKuha.Services;
using System.Windows;

namespace KapwaKuha.View
{
    public partial class BeneficiaryTypeSelectWindow : Window
    {
        public BeneficiaryTypeSelectWindow()
        {
            InitializeComponent();
            DataContext = new BeneficiaryTypeSelectViewModel();
            Loaded += (s, e) => NavigationService.SetCurrent(this);
        }
    }
}