// FILE: ViewModels/NotificationViewModel.cs  (NEW)
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using KapwaKuha.Commands;
using KapwaKuha.Models;
using KapwaKuha.Services;

namespace KapwaKuha.ViewModels
{
    public class NotificationViewModel : ObservableObject
    {
        private readonly string _userId;

        public ObservableCollection<NotificationModel> Notifications { get; } = new();

        private int _unreadCount;
        public int UnreadCount
        {
            get => _unreadCount;
            set { _unreadCount = value; OnPropertyChanged(); OnPropertyChanged(nameof(HasUnread)); }
        }
        public bool HasUnread => UnreadCount > 0;

        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            set { _isLoading = value; OnPropertyChanged(); }
        }

        private bool _hasNoNotifications = true;
        public bool HasNoNotifications
        {
            get => _hasNoNotifications;
            set { _hasNoNotifications = value; OnPropertyChanged(); }
        }

        public ICommand MarkAllReadCommand { get; }
        public ICommand MarkReadCommand { get; }
        public ICommand RefreshCommand { get; }

        public NotificationViewModel(string userId)
        {
            _userId = userId;

            MarkAllReadCommand = new AsyncRelayCommand(async _ =>
            {
                await KapwaDataService.MarkAllNotificationsRead(_userId);
                foreach (var n in Notifications) n.IsRead = true;
                UnreadCount = 0;
            });

            MarkReadCommand = new AsyncRelayCommand(async param =>
            {
                if (param is NotificationModel notif && !notif.IsRead)
                {
                    await KapwaDataService.MarkNotificationRead(notif.Notif_ID);
                    notif.IsRead = true;
                    UnreadCount = System.Math.Max(0, UnreadCount - 1);
                }
            });

            RefreshCommand = new AsyncRelayCommand(async _ => await LoadAsync());

            LoadAsync();
        }

        public async System.Threading.Tasks.Task LoadAsync()
        {
            IsLoading = true;
            try
            {
                var notifs = await KapwaDataService.GetNotificationsForUser(_userId);
                Application.Current.Dispatcher.Invoke(() =>
                {
                    Notifications.Clear();
                    foreach (var n in notifs) Notifications.Add(n);
                    UnreadCount = Notifications.Count(n => !n.IsRead);
                    HasNoNotifications = !Notifications.Any();
                });
            }
            catch { }
            finally { IsLoading = false; }
        }
    }
}