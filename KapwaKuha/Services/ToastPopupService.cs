// Services/ToastPopupService.cs
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace KapwaKuha.Services
{
    /// <summary>
    /// Lightweight WPF in-app toast — no UWP / Windows.UI.Notifications dependency needed.
    /// Call from any thread; it marshals to the UI thread automatically.
    /// </summary>
    public static class ToastPopupService
    {
        public static void Show(string title, string message, int durationSeconds = 4)
        {
            var app = Application.Current;
            if (app == null) return;

            app.Dispatcher.Invoke(() =>
            {
                // Find host window
                Window? host = null;
                foreach (Window w in app.Windows)
                {
                    if (w.IsActive) { host = w; break; }
                }
                host ??= app.MainWindow;
                if (host == null) return;

                // Build toast border
                var toast = new Border
                {
                    Background = new SolidColorBrush(Color.FromRgb(10, 37, 64)),
                    CornerRadius = new CornerRadius(12),
                    Padding = new Thickness(18, 12, 18, 12),
                    Margin = new Thickness(0, 0, 16, 16),
                    MaxWidth = 320,
                    HorizontalAlignment = HorizontalAlignment.Right,
                    VerticalAlignment = VerticalAlignment.Bottom,
                    Opacity = 0,
                    RenderTransformOrigin = new Point(1, 1),
                    RenderTransform = new TranslateTransform(0, 40)
                };

                var panel = new StackPanel();
                panel.Children.Add(new TextBlock
                {
                    Text = title,
                    Foreground = Brushes.White,
                    FontWeight = FontWeights.Bold,
                    FontSize = 13,
                    TextWrapping = TextWrapping.Wrap
                });
                panel.Children.Add(new TextBlock
                {
                    Text = message,
                    Foreground = new SolidColorBrush(Color.FromRgb(186, 230, 253)),
                    FontSize = 12,
                    TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(0, 4, 0, 0)
                });
                toast.Child = panel;

                // Overlay on the window using AdornerLayer via a Grid overlay
                // Inject into window content via a helper overlay
                var overlay = EnsureOverlay(host);
                overlay.Children.Add(toast);

                // Animate in
                var slideIn = new DoubleAnimation(40, 0, TimeSpan.FromMilliseconds(300)) { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } };
                var fadeIn = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(300));
                toast.RenderTransform.BeginAnimation(TranslateTransform.YProperty, slideIn);
                toast.BeginAnimation(UIElement.OpacityProperty, fadeIn);

                // Auto-dismiss
                var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(durationSeconds) };
                timer.Tick += (_, _) =>
                {
                    timer.Stop();
                    var fadeOut = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(300));
                    var slideOut = new DoubleAnimation(0, 40, TimeSpan.FromMilliseconds(300));
                    fadeOut.Completed += (_, _) => overlay.Children.Remove(toast);
                    toast.BeginAnimation(UIElement.OpacityProperty, fadeOut);
                    toast.RenderTransform.BeginAnimation(TranslateTransform.YProperty, slideOut);
                };
                timer.Start();
            });
        }

        private static Grid EnsureOverlay(Window host)
        {
            const string OverlayTag = "KapwaToastOverlay";

            // Check if overlay already exists
            if (host.Content is Grid rootGrid)
            {
                foreach (UIElement child in rootGrid.Children)
                {
                    if (child is Grid g && g.Tag?.ToString() == OverlayTag)
                        return g;
                }
                var overlay = MakeOverlayGrid(OverlayTag);
                rootGrid.Children.Add(overlay);
                return overlay;
            }

            // Wrap existing content
            var originalContent = host.Content as UIElement;
            var wrapper = new Grid();
            if (originalContent != null)
            {
                host.Content = null;
                wrapper.Children.Add(originalContent);
            }
            var ol = MakeOverlayGrid(OverlayTag);
            wrapper.Children.Add(ol);
            host.Content = wrapper;
            return ol;
        }

        private static Grid MakeOverlayGrid(string tag) => new Grid
        {
            Tag = tag,
            IsHitTestVisible = false,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch
        };
    }
}