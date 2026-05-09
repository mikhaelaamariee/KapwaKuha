// FILE: ItemsViewModel.cs
// Window: ItemsWindow.xaml
// Admin view — all items in the system (parallel to AdminCarsViewModel in CarRentals)
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using KapwaKuha.Commands;
using KapwaKuha.Models;
using KapwaKuha.Services;

namespace KapwaKuha.ViewModels
{
    public class ItemsViewModel : ObservableObject
    {
        private readonly string _userId;

        public ObservableCollection<ItemModel> AllItems { get; } = new();

        private ItemModel? _selectedItem;
        public ItemModel? SelectedItem
        {
            get => _selectedItem;
            set { _selectedItem = value; OnPropertyChanged(); OnPropertyChanged(nameof(IsItemSelected)); }
        }

        public bool IsItemSelected => SelectedItem != null;

        private string _statusMessage = string.Empty;
        public string StatusMessage
        {
            get => _statusMessage;
            set { _statusMessage = value; OnPropertyChanged(); }
        }

        private bool _isBusy;
        public bool IsBusy
        {
            get => _isBusy;
            set { _isBusy = value; OnPropertyChanged(); }
        }
        private string _searchText = string.Empty;
        public string SearchText
        {
            get => _searchText;
            set { _searchText = value; OnPropertyChanged(); OnPropertyChanged(nameof(FilteredItems)); }
        }

        private string _selectedStatusFilter = "All";
        public string SelectedStatusFilter
        {
            get => _selectedStatusFilter;
            set { _selectedStatusFilter = value; OnPropertyChanged(); OnPropertyChanged(nameof(FilteredItems)); }
        }

        private string _selectedCategoryFilter = "All";
        public string SelectedCategoryFilter
        {
            get => _selectedCategoryFilter;
            set { _selectedCategoryFilter = value; OnPropertyChanged(); OnPropertyChanged(nameof(FilteredItems)); }
        }

        public List<string> StatusFilters => new() { "All", "Available", "Claimed", "Reserved" };
        public List<string> CategoryFilters => new() { "All", "Clothing", "Food", "Electronics", "Medicine", "School Supplies" };
        public List<string> StatusOptions => new() { "Available", "Claimed", "Reserved" };

        private string _newStatus = "Available";
        public string NewStatus
        {
            get => _newStatus;
            set { _newStatus = value; OnPropertyChanged(); }
        }

        public IEnumerable<ItemModel> FilteredItems => AllItems.Where(i =>
            (SelectedStatusFilter == "All" || i.Item_Status == SelectedStatusFilter) &&
            (SelectedCategoryFilter == "All" || i.Category_Name == SelectedCategoryFilter) &&
            (string.IsNullOrWhiteSpace(SearchText) || i.Item_Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase)));
        public ICommand BackCommand { get; }
        public ICommand RefreshCommand { get; }
        public ICommand DeleteCommand { get; }

        public ItemsViewModel(string userId)
        {
            _userId = userId;

            BackCommand = new RelayCommand(_ =>
                NavigationService.Navigate(new View.DonorDashboardWindow(_userId)));

            RefreshCommand = new AsyncRelayCommand(async _ => await LoadItemsAsync());

            DeleteCommand = new AsyncRelayCommand(async _ =>
            {
                if (SelectedItem == null) return;

                var confirm = MessageBox.Show(
                    $"Remove item \"{SelectedItem.Item_Name}\"?\nThis cannot be undone.",
                    "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (confirm != MessageBoxResult.Yes) return;

                try
                {
                    IsBusy = true;
                    await KapwaDataService.DeleteItem(SelectedItem.Item_ID);
                    AllItems.Remove(SelectedItem);
                    SelectedItem = null;
                    StatusMessage = $"Item removed. {AllItems.Count} item(s) remaining.";
                }
                catch (System.Exception ex)
                {
                    MessageBox.Show("Delete failed: " + ex.Message);
                }
                finally { IsBusy = false; }
            });

            _ = LoadItemsAsync();
        }

        private async System.Threading.Tasks.Task LoadItemsAsync()
        {
            IsBusy = true;
            try
            {
                var items = await KapwaDataService.GetAllItems();
                Application.Current.Dispatcher.Invoke(() =>
                {
                    AllItems.Clear();
                    foreach (var i in items) AllItems.Add(i);
                    StatusMessage = $"{AllItems.Count} total item(s).";
                });
            }
            catch (System.Exception ex) { StatusMessage = "Load failed."; MessageBox.Show(ex.Message); }
            finally { IsBusy = false; }
        }
        public class ItemsDesignViewModel : ObservableObject
        {
            public bool IsItemSelected => true;
            public string SearchText { get; set; } = "";
            public string SelectedStatusFilter { get; set; } = "All";
            public string SelectedCategoryFilter { get; set; } = "All";
            public string NewStatus { get; set; } = "Available";
            public List<string> StatusFilters => new() { "All", "Available", "Claimed", "Reserved" };
            public List<string> CategoryFilters => new() { "All", "Clothing", "Food", "Electronics" };
            public List<string> StatusOptions => new() { "Available", "Claimed", "Reserved" };
            public ICommand BackCommand { get; } = new RelayCommand(_ => { });
            public ICommand RefreshCommand { get; } = new RelayCommand(_ => { });
            public ICommand UpdateStatusCommand { get; } = new RelayCommand(_ => { });
            public ItemModel SelectedItem { get; } = new ItemModel
            {
                Item_ID = "ITEM001",
                Item_Name = "Assorted Clothing",
                Item_Status = "Available",
                Category_Name = "Clothing",
                Date_Found = DateTime.Now.AddDays(-3),
                Donor_Name = "Juan Dela Cruz"
            };
            public ObservableCollection<ItemModel> FilteredItems { get; } = new()
    {
        new ItemModel { Item_ID="ITEM001", Item_Name="Assorted Clothing",   Item_Status="Available", Category_Name="Clothing",  Donor_Name="Juan Dela Cruz",   Date_Found=DateTime.Now.AddDays(-3) },
        new ItemModel { Item_ID="ITEM002", Item_Name="Canned Goods",        Item_Status="Claimed",   Category_Name="Food",      Donor_Name="Maria Santos",     Date_Found=DateTime.Now.AddDays(-7) },
        new ItemModel { Item_ID="ITEM003", Item_Name="Casio Calculator",    Item_Status="Available", Category_Name="Electronics",Donor_Name="Roberto Reyes",   Date_Found=DateTime.Now.AddDays(-1) },
        new ItemModel { Item_ID="ITEM004", Item_Name="School Backpack",     Item_Status="Reserved",  Category_Name="School Supplies",Donor_Name="Juan Dela Cruz",Date_Found=DateTime.Now },
    };
        }
    }
}