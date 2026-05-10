// FILE: View/EditNeedsPostUrgencyWindow.xaml.cs
using System.Windows;
using KapwaKuha.ViewModels;

namespace KapwaKuha.View
{
    public partial class EditNeedsPostUrgencyWindow : Window
    {
        public EditNeedsPostUrgencyWindow(string beneficiaryId, string orgId)
        {
            InitializeComponent();
            DataContext = new EditNeedsPostUrgencyViewModel(beneficiaryId, orgId);
            Loaded += (s, e) => Services.NavigationService.SetCurrent(this);
        }

       
    }
}