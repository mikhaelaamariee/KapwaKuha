using System.Windows;
using KapwaKuha.Services;

namespace KapwaKuha.View
{
    public partial class ItemsWindow : Window
    {
        public ItemsWindow(string userId)
        {
            InitializeComponent();
            DataContext = new KapwaKuha.ViewModels.ItemsViewModel(userId);
            Loaded += (s, e) => NavigationService.SetCurrent(this);
        }
    }
}