using System.Windows;
using KapwaKuha.Services;
using KapwaKuha.ViewModels;

namespace KapwaKuha.View
{
    public partial class NeedsWishlistWindow : Window
    {
        public NeedsWishlistWindow(string beneficiaryId)
        {
            InitializeComponent();
            DataContext = new NeedsWishlistViewModel(beneficiaryId);
            Loaded += (s, e) => NavigationService.SetCurrent(this);
        }
    }
}