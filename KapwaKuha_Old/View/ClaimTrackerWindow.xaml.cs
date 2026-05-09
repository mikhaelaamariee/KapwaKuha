// FILE: View/ClaimTrackerWindow.xaml.cs
using System.Windows;
using KapwaKuha.ViewModels;

namespace KapwaKuha.View
{
    public partial class ClaimTrackerWindow : Window
    {
        public ClaimTrackerWindow(string userId, string role)
        {
            InitializeComponent();
            DataContext = new ClaimTrackerViewModel(userId, role);
            Loaded += (s, e) => Services.NavigationService.SetCurrent(this);
        }
    }
}