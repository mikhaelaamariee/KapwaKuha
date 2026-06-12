using KapwaKuha.ViewModels;
using KapwaKuha.Services;
using System.Windows;

namespace KapwaKuha.View
{
    public partial class AdminDashboardWindow : Window
    {
        public AdminDashboardWindow(string adminId)
        {
            try
            {
                InitializeComponent();
                DataContext = new AdminDashboardViewModel(adminId);
                Loaded += (s, e) => NavigationService.SetCurrent(this);
            }
           

            catch (Exception ex)
{
                // This catches XAML inflation crashes, missing window resources, and initialization errors!
                MessageBox.Show($"CRITICAL VIEW INFLATION CRASH:\n\n{ex.ToString()}", "Window Init Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ReportImage_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (sender is not System.Windows.Controls.Border border) return;
            string? path = border.Tag as string;
            if (string.IsNullOrEmpty(path) || !System.IO.File.Exists(path)) return;

            var popup = new Window
            {
                Title = "Report Proof Image",
                Width = 640,
                Height = 640,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = this,
                ResizeMode = ResizeMode.CanResize,
                Background = System.Windows.Media.Brushes.Black,
                ShowInTaskbar = false
            };
            var img = new System.Windows.Controls.Image
            {
                Source = new System.Windows.Media.Imaging.BitmapImage(new Uri(path)),
                Stretch = System.Windows.Media.Stretch.Uniform,
                Margin = new Thickness(8)
            };
            popup.MouseLeftButtonDown += (s, _) => popup.Close();
            popup.Content = img;
            popup.ShowDialog();
        }
        // Add inside AdminDashboardWindow class
        private void ItemImage_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (sender is not System.Windows.Controls.Border border) return;
            string? path = border.Tag as string;
            if (string.IsNullOrEmpty(path) || !System.IO.File.Exists(path)) return;

            var popup = new Window
            {
                Title = "Image Preview",
                Width = 640,
                Height = 640,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = this,
                ResizeMode = ResizeMode.CanResize,
                Background = System.Windows.Media.Brushes.Black,
                ShowInTaskbar = false
            };
            var img = new System.Windows.Controls.Image
            {
                Source = new System.Windows.Media.Imaging.BitmapImage(new Uri(path)),
                Stretch = System.Windows.Media.Stretch.Uniform,
                Margin = new Thickness(8)
            };
            // Click anywhere to close
            popup.MouseLeftButtonDown += (s, _) => popup.Close();
            popup.Content = img;
            popup.ShowDialog();
        }
    }
}