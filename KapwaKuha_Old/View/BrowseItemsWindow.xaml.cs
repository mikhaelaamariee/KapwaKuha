using System.Windows;
using KapwaKuha.Services;
using KapwaKuha.ViewModels;

namespace KapwaKuha.View
{
    public partial class BrowseItemsWindow : Window
    {
        public BrowseItemsWindow(string beneficiaryId)
        {
            InitializeComponent();
            DataContext = new BrowseItemsViewModel(beneficiaryId);
            Loaded += (s, e) => NavigationService.SetCurrent(this);
        }
    }
}