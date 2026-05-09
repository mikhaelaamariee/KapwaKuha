using System.Windows;
using KapwaKuha.Services;
using KapwaKuha.ViewModels;

namespace KapwaKuha.View
{
    public partial class MyImpactWindow : Window
    {
        public MyImpactWindow(string donorId)
        {
            InitializeComponent();
            DataContext = new MyImpactViewModel(donorId);
            Loaded += (s, e) => NavigationService.SetCurrent(this);
        }
    }
}