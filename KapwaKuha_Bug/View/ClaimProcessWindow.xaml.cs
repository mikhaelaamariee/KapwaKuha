using KapwaKuha.ViewModels;
using System.Windows;
using KapwaKuha.Services;

namespace KapwaKuha.View   
{
    public partial class ClaimProcessWindow : Window
    {
        public ClaimProcessWindow(string adminId)
        {
            InitializeComponent();
            DataContext = new ClaimProcessViewModel(adminId);
            Loaded += (s, e) => NavigationService.SetCurrent(this);
        }

     
    }
}