// FILE: View/BeneficiaryProfileWindow.xaml.cs
using System.Windows;
using KapwaKuha.Services;
using KapwaKuha.ViewModels;

namespace KapwaKuha.View
{
    public partial class BeneficiaryProfileWindow : Window
    {
        public BeneficiaryProfileWindow(string beneficiaryId)
        {
            InitializeComponent();
            DataContext = new BeneficiaryProfileViewModel(beneficiaryId);
            Loaded += (s, e) => NavigationService.SetCurrent(this);
        }
    }
}