using KapwaKuha.Models;
using KapwaKuha.ViewModels;
using KapwaKuha.Services;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace KapwaKuha.View
{
    public partial class NotificationsWindow : Window
    {
        public NotificationsWindow(string userId)
        {
            InitializeComponent();
            DataContext = new NotificationViewModel(userId);
            Loaded += (s, e) => NavigationService.SetCurrent(this);
        }

        private void NotifItem_Clicked(object sender, MouseButtonEventArgs e)
        {
            if (sender is ListViewItem item &&
                item.DataContext is NotificationModel notif &&
                DataContext is NotificationViewModel vm)
            {
                vm.MarkReadCommand.Execute(notif);
            }
        }
    }
}