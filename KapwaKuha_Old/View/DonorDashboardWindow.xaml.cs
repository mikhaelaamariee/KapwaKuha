using KapwaKuha.ViewModels;
using KapwaKuha.Services;
using System.Windows;

namespace KapwaKuha.View
{
    public partial class DonorDashboardWindow : Window
    {
        public DonorDashboardWindow(string donorId)
        {
            InitializeComponent();
            DataContext = new DonorDashboardViewModel(donorId);
            Loaded += (s, e) => NavigationService.SetCurrent(this);
        }
    }
}