// FILE: ViewModels/BrowseItemsViewModel.cs
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using KapwaKuha.Commands;
using KapwaKuha.Models;
using KapwaKuha.Services;

namespace KapwaKuha.ViewModels
{
    public class BrowseItemsViewModel : ObservableObject
    {
        private readonly string _beneficiaryId;
        private System.Collections.Generic.List<ItemModel> _allItems = new();

        public ObservableCollection<ItemModel> Items { get; } = new();

        private string _searchText = string.Empty;
        public string SearchText
        {
            get => _searchText;
            set { _searchText = value; OnPropertyChanged(); ApplyFilter(); }
        }

        private string _filterCategory = "All";
        public string FilterCategory
        {
            get => _filterCategory;
            set
            {
                _filterCategory = value; OnPropertyChanged(); ApplyFilter();
                OnPropertyChanged(nameof(IsCatAll));
                OnPropertyChanged(nameof(IsCatClothing));
                OnPropertyChanged(nameof(IsCatFood));
                OnPropertyChanged(nameof(IsCatElectronics));
                OnPropertyChanged(nameof(IsCatMedicine));
                OnPropertyChanged(nameof(IsCatSchool));
            }
        }

        private string _filterCondition = "Any";
        public string FilterCondition
        {
            get => _filterCondition;
            set { _filterCondition = value; OnPropertyChanged(); ApplyFilter(); }
        }

        // Radio button bindings for sidebar
        public bool IsCatAll { get => _filterCategory == "All"; set { if (value) FilterCategory = "All"; } }
        public bool IsCatClothing { get => _filterCategory == "Clothing"; set { if (value) FilterCategory = "Clothing"; } }
        public bool IsCatFood { get => _filterCategory == "Food"; set { if (value) FilterCategory = "Food"; } }
        public bool IsCatElectronics { get => _filterCategory == "Electronics"; set { if (value) FilterCategory = "Electronics"; } }
        public bool IsCatMedicine { get => _filterCategory == "Medicine"; set { if (value) FilterCategory = "Medicine"; } }
        public bool IsCatSchool { get => _filterCategory == "School Supplies"; set { if (value) FilterCategory = "School Supplies"; } }

        private bool _isBusy;
        public bool IsBusy { get => _isBusy; set { _isBusy = value; OnPropertyChanged(); } }

        public ICommand BackCommand { get; }
        public ICommand RefreshCommand { get; }
        public ICommand SelectItemCommand { get; }

        public BrowseItemsViewModel(string beneficiaryId)
        {
            _beneficiaryId = beneficiaryId;

            BackCommand = new RelayCommand(_ =>
                NavigationService.Navigate(new View.BeneficiaryDashboardWindow(_beneficiaryId)));

            RefreshCommand = new AsyncRelayCommand(async _ => await LoadItemsAsync());

            SelectItemCommand = new RelayCommand(item =>
            {
                if (item is ItemModel selected)
                    NavigationService.Navigate(new View.ClaimItemWindow(_beneficiaryId, selected));
            });

            _ = LoadItemsAsync();
        }

        private async System.Threading.Tasks.Task LoadItemsAsync()
        {
            // Ensure thread safety for IsBusy
            Application.Current?.Dispatcher.Invoke(() => IsBusy = true);
            try
            {
                var items = await KapwaDataService.GetAvailableItems();
                Application.Current?.Dispatcher.Invoke(() =>
                {
                    // Null check fallback in case DB service returns null
                    _allItems = items ?? new System.Collections.Generic.List<ItemModel>();
                    ApplyFilter();
                });
            }
            catch { /* Keep this empty to swallow background DB errors, or log them */ }
            finally
            {
                // Must be back on the UI thread to update the bound property
                Application.Current?.Dispatcher.Invoke(() => IsBusy = false);
            }
        }

        private void ApplyFilter()
        {
            Items.Clear();

            // Prevent crash if the database returned a null list
            if (_allItems == null) return;

            // Safely trim and lower the search text
            var q = _searchText?.Trim().ToLower() ?? string.Empty;

            foreach (var i in _allItems)
            {
                // Use ?. to safely check for nulls before calling ToLower()
                bool matchSearch = string.IsNullOrEmpty(q) ||
                                   (i.Item_Name?.ToLower().Contains(q) ?? false) ||
                                   (i.Item_Description?.ToLower().Contains(q) ?? false) ||
                                   (i.Donor_Name?.ToLower().Contains(q) ?? false);

                bool matchCat = _filterCategory == "All" || i.Category_Name == _filterCategory;
                bool matchCond = _filterCondition == "Any" || i.Item_Condition == _filterCondition;

                if (matchSearch && matchCat && matchCond)
                {
                    Items.Add(i);
                }
            }
        }
    }
}