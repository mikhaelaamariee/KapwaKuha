using System.Windows;
using KapwaKuha.Services;
using KapwaKuha.ViewModels;

namespace KapwaKuha.View
{
    public partial class BrowseItemsWindow : Window
    {
        // Original constructor — no pre-filter
        public BrowseItemsWindow(string beneficiaryId)
        {
            InitializeComponent();
            DataContext = new BrowseItemsViewModel(beneficiaryId);
            Loaded += (s, e) => NavigationService.SetCurrent(this);
        }

        // New overload — opens with a category pre-selected
        public BrowseItemsWindow(string beneficiaryId, string initialCategory)
        {
            InitializeComponent();
            DataContext = new BrowseItemsViewModel(beneficiaryId, initialCategory);
            Loaded += (s, e) => NavigationService.SetCurrent(this);
        }
    }
}