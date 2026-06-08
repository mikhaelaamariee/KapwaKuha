// FILE: View/UserProfileWindow.xaml.cs
using KapwaKuha.ViewModels;
using System.Windows;

namespace KapwaKuha.View
{
    public partial class UserProfileWindow : Window
    {
        public UserProfileWindow(string targetId, string viewerId, string viewerRole)
        {
            InitializeComponent();
            var vm = new UserProfileViewModel(targetId, viewerId, viewerRole);
            vm.OnCloseRequested = () => Close();
            DataContext = vm;
        }
    }
}