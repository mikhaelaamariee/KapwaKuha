// FILE: View/DonorClaimTrackerWindow.xaml.cs
using System.Windows;
using KapwaKuha.Services;
using KapwaKuha.ViewModels;

namespace KapwaKuha.View
{
    public partial class DonorClaimTrackerWindow : Window
    {
        public DonorClaimTrackerWindow(string donorId)
        {
            InitializeComponent();
            DataContext = new ClaimTrackerViewModel(donorId, "Donor");
            Loaded += (s, e) => NavigationService.SetCurrent(this);
        }
    }
}