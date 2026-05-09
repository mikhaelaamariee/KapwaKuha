using KapwaKuha.ViewModels;
using KapwaKuha.Services;
using System.Windows;

namespace KapwaKuha.View
{
    public partial class ChooseRoleWindow : Window
    {
        public ChooseRoleWindow()
        {
            InitializeComponent();
            DataContext = new ChooseRoleViewModel();
            Loaded += (s, e) => NavigationService.SetCurrent(this);
        }
    }
}