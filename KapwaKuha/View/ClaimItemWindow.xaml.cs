using System; // Required for 'Action?' to be recognized
using System.Windows;
using KapwaKuha.Models;
using KapwaKuha.Services;
using KapwaKuha.ViewModels;

namespace KapwaKuha.View 
{
    public partial class ClaimItemWindow : Window
    {
        public ClaimItemWindow(string beneficiaryId, ItemModel item, Action? onClaimSuccess = null)
        {
            InitializeComponent();

            // Keeping your exact ViewModel initialization to prevent new errors
            var vm = new ClaimItemViewModel(beneficiaryId, item);
            DataContext = vm;

            Loaded += (s, e) => NavigationService.SetCurrent(this);

            if (string.IsNullOrEmpty(item.Item_Name) || item.Item_Name == item.Item_ID)
            {
                _ = System.Threading.Tasks.Task.Run(async () =>
                {
                    var fullItem = await KapwaDataService.GetItemById(item.Item_ID);
                    if (fullItem != null)
                    {
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            // If your ItemModel implements INotifyPropertyChanged, this will work perfectly.
                            // Otherwise, you may need to replace the object entirely: vm.Item = fullItem;
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