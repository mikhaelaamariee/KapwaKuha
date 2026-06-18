// Services/ToastService.cs
// Pure-WPF toast — no UWP/Microsoft.Toolkit.Uwp.Notifications required.
// NotificationManager calls ToastPopupService.Show() directly, so this file
// is now a thin alias kept for any legacy callers.
namespace KapwaKuha.Services
{
    public static class ToastService
    {
        public static void Show(string title, string message)
        {
            try
            {
                System.Windows.Application.Current?.Dispatcher.Invoke(() =>
                    ToastPopupService.Show(title, message));
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ToastService] Failed: {ex.Message}");
            }
        }
    }
}