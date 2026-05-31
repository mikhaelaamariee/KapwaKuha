using KapwaKuha.ViewModels;
using KapwaKuha.Services;
using System.Windows;

namespace KapwaKuha.View
{
    public partial class AdminDashboardWindow : Window
    {
        public AdminDashboardWindow(string adminId)
        {
            InitializeComponent();
            DataContext = new AdminDashboardViewModel(adminId);
            Loaded += (s, e) => NavigationService.SetCurrent(this);
        }
    }
}