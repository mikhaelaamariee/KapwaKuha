using System.Windows;
using KapwaKuha.Models;
using KapwaKuha.Services;
using KapwaKuha.ViewModels;

namespace KapwaKuha.View
{
    public partial class ClaimItemWindow : Window
    {
        public ClaimItemWindow(string beneficiaryId, ItemModel item,
                               Action? onClaimSuccess = null)
        {
            InitializeComponent();
            DataContext = new ClaimItemViewModel(beneficiaryId, item);
            Loaded += (s, e) => NavigationService.SetCurrent(this);

            // In ClaimItemWindow.xaml.cs constructor, after InitializeComponent():
            // If item was passed with only ID (from chat accept), load full details
            if (string.IsNullOrEmpty(item.Item_Name) || item.Item_Name == item.Item_ID)
            {
                // Fire and forget — loads full item data asynchronously
                _ = System.Threading.Tasks.Task.Run(async () =>
                {
                    var fullItem = await KapwaDataService.GetItemById(item.Item_ID);
                    if (fullItem != null && DataContext is ClaimItemViewModel vm)
                    {
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            // Update the Item property — VM exposes it as public
                            // so XAML bindings update automatically
                            if (vm.Item is ItemModel i)
                            {
                                i.Item_Name = fullItem.Item_Name;
                                i.Item_Description = fullItem.Item_Description;
                                i.Item_ImagePath = fullItem.Item_ImagePath;
                                i.Category_Name = fullItem.Category_Name;
                                i.Donor_Name = fullItem.Donor_Name;
                            }
                        });
                    }
                });
            }
        }
    }
}