// FILE: View/PostItemWindow.xaml.cs
using System.Windows;
using KapwaKuha.Services;
using KapwaKuha.ViewModels;

namespace KapwaKuha.View
{
    public partial class PostItemWindow : Window
    {
        public PostItemWindow(string donorId, string prefillTitle = "",
                      string lockedOrgId = "", bool lockDirect = false,
                      string lockedBeneficiaryId = "")
        {
            InitializeComponent();
            DataContext = new PostItemViewModel(donorId, prefillTitle,
                                                lockedOrgId, lockDirect, lockedBeneficiaryId);
            Loaded += (s, e) => NavigationService.SetCurrent(this);
        }
    }
}