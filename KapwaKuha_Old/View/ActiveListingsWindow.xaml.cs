using System.Windows;
using KapwaKuha.Services;
using KapwaKuha.ViewModels;

namespace KapwaKuha.View
{
    public partial class ActiveListingsWindow : Window
    {
        public ActiveListingsWindow(string donorId)
        {
            InitializeComponent();
            DataContext = new ActiveListingsViewModel(donorId);
            Loaded += (s, e) => NavigationService.SetCurrent(this);
        }
    }
}