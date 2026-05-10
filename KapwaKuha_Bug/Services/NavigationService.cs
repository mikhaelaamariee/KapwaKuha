using System;
using System.Windows;
using System.Windows.Threading;

namespace KapwaKuha.Services
{
    public static class NavigationService
    {
        private static Window? _current;

        // Call in each window's Loaded event: NavigationService.SetCurrent(this);
        public static void SetCurrent(Window window) => _current = window;

        public static void Navigate(Window next)
        {
            var previous = _current;

            if (previous != null)
            {
                next.WindowState = previous.WindowState;
                next.WindowStartupLocation = WindowStartupLocation.Manual;
                next.Top = previous.Top;
                next.Left = previous.Left;
                next.Width = previous.Width;
                next.Height = previous.Height;
            }

            Application.Current.MainWindow = next;
            _current = next;
            next.Show();

            if (previous != null)
            {
                // ADD THIS LINE — ensures app doesn't shut down during window swap:
                Application.Current.ShutdownMode = ShutdownMode.OnExplicitShutdown;

                previous.Dispatcher.BeginInvoke(
                    DispatcherPriority.Background,   // Changed from Loaded to Background
                    new Action(() =>
                    {
                        previous.Close();
                        // Restore normal shutdown mode after close completes:
                        Application.Current.ShutdownMode = ShutdownMode.OnLastWindowClose;
                    }));
            }
        }
    }
}