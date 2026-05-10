using System.Windows;
using KapwaKuha.Services;
using KapwaKuha.ViewModels;

namespace KapwaKuha.View
{
    public partial class ChatListWindow : Window
    {
        public ChatListWindow(string myId, string role)
        {
            InitializeComponent();
            DataContext = new ChatListViewModel(myId, role);
            Loaded += (s, e) => NavigationService.SetCurrent(this);
        }
    }
}