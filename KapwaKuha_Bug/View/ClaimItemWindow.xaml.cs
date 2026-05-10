using System;
using System.Windows;
using KapwaKuha.Models;
using KapwaKuha.Services;
using KapwaKuha.ViewModels;

namespace KapwaKuha.View
{
    public partial class ClaimItemWindow : Window
    {
        public ClaimItemWindow(string beneficiaryId, ItemModel item,
            Action? onClaimSuccess = null,
            string? returnToDonorId = null,
            string? returnToDonorName = null)
        {
            InitializeComponent();
            var vm = new ClaimItemViewModel(beneficiaryId, item,
                onClaimSuccess, returnToDonorId, returnToDonorName);
            DataContext = vm;

            NavigationService.SetCurrent(this);

            Loaded += async (s, e) =>
            {
              
                // Safe async load in Loaded event — exceptions don't kill the app here
                try
                {
                    var fullItem = await KapwaDataService.GetItemById(item.Item_ID);
                    if (fullItem == null) return;
                    if (!string.IsNullOrEmpty(fullItem.Item_Name)) vm.Item.Item_Name = fullItem.Item_Name;
                    if (!string.IsNullOrEmpty(fullItem.Item_Description)) vm.Item.Item_Description = fullItem.Item_Description;
                    if (!string.IsNullOrEmpty(fullItem.Item_ImagePath)) vm.Item.Item_ImagePath = fullItem.Item_ImagePath;
                    if (!string.IsNullOrEmpty(fullItem.Category_Name)) vm.Item.Category_Name = fullItem.Category_Name;
                    if (!string.IsNullOrEmpty(fullItem.Donor_Name)) vm.Item.Donor_Name = fullItem.Donor_Name;
                    if (!string.IsNullOrEmpty(fullItem.Item_Condition)) vm.Item.Item_Condition = fullItem.Item_Condition;
                    if (fullItem.Date_Found != default) vm.Item.Date_Found = fullItem.Date_Found;
                    vm.RefreshItemProps();
                }
                catch { /* silently ignore — item already has partial data from chat */ }
            };
        }
    }
}