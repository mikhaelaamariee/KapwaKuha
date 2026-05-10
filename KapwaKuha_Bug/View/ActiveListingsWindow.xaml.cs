// FILE: View/ActiveListingsWindow.xaml.cs
using System.Windows;
using System.Windows.Controls;
using KapwaKuha.Services;
using KapwaKuha.ViewModels;

namespace KapwaKuha.View
{
    public partial class ActiveListingsWindow : Window
    {
        // ── Standard navigation: no pinpoint ─────────────────────────────────
        public ActiveListingsWindow(string donorId)
            : this(donorId, string.Empty) { }

        // ── REQUIREMENT 5: Pinpoint navigation ───────────────────────────────
        /// <summary>
        /// Opens Active Listings and scrolls to + highlights the item
        /// identified by <paramref name="pinpointItemId"/>.
        /// </summary>
        public ActiveListingsWindow(string donorId, string pinpointItemId)
        {
            InitializeComponent();
            var vm = new ActiveListingsViewModel(donorId, pinpointItemId);
            DataContext = vm;

            Loaded += (s, e) =>
            {
                NavigationService.SetCurrent(this);

                // If a pinpoint ID was supplied, wait for the ListBox to render
                // then bring the selected item into view.
                if (!string.IsNullOrEmpty(pinpointItemId))
                {
                    // Small dispatcher delay ensures the ListBox has laid out its items
                    Dispatcher.BeginInvoke(
                        System.Windows.Threading.DispatcherPriority.Loaded,
                        new System.Action(() => ScrollToPinpoint(vm, pinpointItemId)));
                }
            };
        }

        /// <summary>
        /// Finds the named ListBox in the visual tree and calls
        /// ScrollIntoView for the selected item.
        /// </summary>
        private void ScrollToPinpoint(ActiveListingsViewModel vm, string itemId)
        {
            // Walk the visual tree to find the ListBox that shows Items
            var listBox = FindItemsListBox(this);
            if (listBox == null) return;

            // Find the matching item in the ListBox's Items source
            foreach (var obj in listBox.Items)
            {
                if (obj is KapwaKuha.Models.ItemModel item && item.Item_ID == itemId)
                {
                    listBox.ScrollIntoView(item);

                    // Highlight it (SelectedItem is already set by VM, but force UI selection too)
                    listBox.SelectedItem = item;
                    break;
                }
            }
        }

        /// <summary>Recursively finds the first ListBox in the visual tree.</summary>
        private static ListBox? FindItemsListBox(System.Windows.DependencyObject parent)
        {
            for (int i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = System.Windows.Media.VisualTreeHelper.GetChild(parent, i);
                if (child is ListBox lb) return lb;
                var result = FindItemsListBox(child);
                if (result != null) return result;
            }
            return null;
        }
    }
}