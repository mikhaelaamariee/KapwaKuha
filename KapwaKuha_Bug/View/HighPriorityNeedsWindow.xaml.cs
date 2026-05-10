using System.Windows;
using KapwaKuha.Services;
using KapwaKuha.ViewModels;

namespace KapwaKuha.View
{
    public partial class HighPriorityNeedsWindow : Window
    {
        public HighPriorityNeedsWindow(string donorId)
        {
            InitializeComponent();
            DataContext = new HighPriorityNeedsViewModel(donorId);
            Loaded += (s, e) => NavigationService.SetCurrent(this);
        }
    }
}