// ViewModels/NotificationViewModel.cs
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using KapwaKuha.Commands;
using KapwaKuha.Models;
using KapwaKuha.Services;
using Microsoft.Data.SqlClient;

namespace KapwaKuha.ViewModels
{
    public class NotificationViewModel : INotifyPropertyChanged
    {
        private readonly string _userId;

        // ── Collection ────────────────────────────────────────────────────────
        public ObservableCollection<NotificationModel> Notifications { get; } = new();

        // Alias used by dashboard XAML: NotifVM.NotificationsCollection
        public ObservableCollection<NotificationModel> NotificationsCollection => Notifications;

        // ── Unread count ──────────────────────────────────────────────────────
        private int _unreadCount;
        public int UnreadCount
        {
            get => _unreadCount;
            private set
            {
                _unreadCount = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(HasUnread));
            }
        }

        public bool HasUnread => _unreadCount > 0;

        // ── Popup open/close ──────────────────────────────────────────────────
        private bool _isPopupOpen;
        public bool IsPopupOpen
        {
            get => _isPopupOpen;
            set
            {
                _isPopupOpen = value;
                OnPropertyChanged();
                if (value)
                    _ = LoadAsync(); // load fresh notifications when popup opens
            }
        }

        // ── Commands ──────────────────────────────────────────────────────────
        public ICommand TogglePopupCommand { get; }
        public ICommand MarkReadCommand { get; }
        public ICommand MarkAllReadCommand { get; }
        public ICommand MarkAllAsReadCommand { get; } // alias used in XAML
        public ICommand RefreshCommand { get; }


        // ── Constructor ───────────────────────────────────────────────────────
        public NotificationViewModel(string userId)
        {
            _userId = userId;

            TogglePopupCommand = new AsyncRelayCommand(async () =>
            {
                IsPopupOpen = !IsPopupOpen;
                if (IsPopupOpen)
                    await LoadAsync();
            });

       

            MarkReadCommand = new AsyncRelayCommand<string>(MarkReadAsync);
            MarkAllReadCommand = new AsyncRelayCommand(MarkAllReadAsync);
            MarkAllAsReadCommand = new AsyncRelayCommand(MarkAllReadAsync); // same handler
            RefreshCommand = new AsyncRelayCommand(LoadAsync);
        }

        // ── Load ──────────────────────────────────────────────────────────────
        public async Task LoadAsync()
        {
            try
            {
                var dt = await RunSpQueryAsync("sp_GetUserNotifications", cmd =>
                    cmd.Parameters.AddWithValue("@UserId", _userId));

                Notifications.Clear();
                foreach (DataRow row in dt.Rows)
                {
                    Notifications.Add(new NotificationModel
                    {
                        Notif_ID = row["Notif_ID"]?.ToString() ?? "",
                        Recipient_ID = row["Recipient_ID"]?.ToString() ?? "",
                        TargetRole = row["TargetRole"]?.ToString() ?? "",
                        Title = row["Title"]?.ToString() ?? "",
                        Notif_Type = row["Notif_Type"]?.ToString() ?? "",
                        Message = row["Message"]?.ToString() ?? "",
                        IsRead = Convert.ToBoolean(row["IsRead"]),
                        SentAt = Convert.ToDateTime(row["SentAt"]),
                        Reference_ID = row["Reference_ID"]?.ToString() ?? ""
                    });
                }

                await LoadUnreadCountAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[NotifVM] Load error: {ex.Message}");
            }
        }

        // Alias for dashboards that call LoadNotificationsAsync()
        public Task LoadNotificationsAsync() => LoadAsync();

        // ── Mark single read ──────────────────────────────────────────────────
        private async Task MarkReadAsync(string? notifId)
        {
            if (string.IsNullOrWhiteSpace(notifId)) return;
            try
            {
                await RunSpNonQueryAsync("sp_MarkNotificationRead", cmd =>
                    cmd.Parameters.AddWithValue("@NotifId", notifId));

                var item = Notifications.FirstOrDefault(n => n.Notif_ID == notifId);
                if (item != null) item.IsRead = true;
                await LoadUnreadCountAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[NotifVM] MarkRead error: {ex.Message}");
            }
        }

        // ── Mark all read ─────────────────────────────────────────────────────
        private async Task MarkAllReadAsync()
        {
            try
            {
                await RunSpNonQueryAsync("sp_MarkAllNotificationsRead", cmd =>
                    cmd.Parameters.AddWithValue("@UserId", _userId));

                foreach (var n in Notifications) n.IsRead = true;
                UnreadCount = 0;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[NotifVM] MarkAllRead error: {ex.Message}");
            }
        }

        // ── Unread count ──────────────────────────────────────────────────────
        private async Task LoadUnreadCountAsync()
        {
            try
            {
                var dt = await RunSpQueryAsync("sp_GetUnreadNotificationCount", cmd =>
                    cmd.Parameters.AddWithValue("@UserId", _userId));

                if (dt.Rows.Count > 0)
                    UnreadCount = Convert.ToInt32(dt.Rows[0]["UnreadCount"]);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[NotifVM] UnreadCount error: {ex.Message}");
            }
        }

        // ── DB helpers ────────────────────────────────────────────────────────
        private static async Task<DataTable> RunSpQueryAsync(string sp, Action<SqlCommand> paramBuilder)
        {
            var dt = new DataTable();
            await using var conn = new SqlConnection(KapwaDataService.GetConnectionString());
            await conn.OpenAsync();
            await using var cmd = new SqlCommand(sp, conn) { CommandType = System.Data.CommandType.StoredProcedure };
            paramBuilder(cmd);
            using var reader = await cmd.ExecuteReaderAsync();
            dt.Load(reader);
            return dt;
        }

        private static async Task RunSpNonQueryAsync(string sp, Action<SqlCommand> paramBuilder)
        {
            await using var conn = new SqlConnection(KapwaDataService.GetConnectionString());
            await conn.OpenAsync();
            await using var cmd = new SqlCommand(sp, conn) { CommandType = System.Data.CommandType.StoredProcedure };
            paramBuilder(cmd);
            await cmd.ExecuteNonQueryAsync();
        }

        // ── INotifyPropertyChanged ────────────────────────────────────────────
        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}