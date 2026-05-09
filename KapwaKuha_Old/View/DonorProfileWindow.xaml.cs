// FILE: View/DonorProfileWindow.xaml.cs
using System.Windows;
using KapwaKuha.Services;
using KapwaKuha.ViewModels;

namespace KapwaKuha.View
{
    public partial class DonorProfileWindow : Window
    {
        public DonorProfileWindow(string donorId)
        {
            InitializeComponent();
            DataContext = new DonorProfileViewModel(donorId);
            Loaded += (s, e) => NavigationService.SetCurrent(this);
        }
    }
}