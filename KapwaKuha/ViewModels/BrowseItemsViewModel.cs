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
        public ICommand MessageDonorCommand { get; }
        public ICommand ViewDonorProfileCommand { get; }   // NEW

        public BrowseItemsViewModel(string beneficiaryId)
            : this(beneficiaryId, "All") { }

        public BrowseItemsViewModel(string beneficiaryId, string initialCategory)
        {
            _beneficiaryId = beneficiaryId;
            _filterCategory = initialCategory;

            BackCommand = new RelayCommand(_ =>
                NavigationService.Navigate(new View.BeneficiaryDashboardWindow(_beneficiaryId)));

            RefreshCommand = new AsyncRelayCommand(async _ => await LoadItemsAsync());

            SelectItemCommand = new RelayCommand(item =>
            {
                if (item is ItemModel selected)
                    NavigationService.Navigate(new View.ClaimItemWindow(_beneficiaryId, selected));
            });

            MessageDonorCommand = new RelayCommand(item =>
            {
                if (item is ItemModel selected)
                    NavigationService.Navigate(
                        new View.ChatWindow(_beneficiaryId, selected.Donor_ID,
                                            selected.Donor_Name, "Beneficiary"));
            });

            // NEW: open UserProfileWindow as a floating modal over the current window
            ViewDonorProfileCommand = new RelayCommand(item =>
            {
                if (item is ItemModel selected)
                {
                    var modal = new View.UserProfileWindow(
                        selected.Donor_ID, _beneficiaryId,
                        UserSession.Role ?? "Beneficiary");
                    modal.Owner = Application.Current.MainWindow;
                    modal.ShowDialog();
                }
            });

            _ = LoadItemsAsync();
        }

        private async System.Threading.Tasks.Task LoadItemsAsync()
        {
            IsBusy = true;
            try
            {
                var all = await KapwaDataService.GetAvailableItems();
                // Fetch ratings in parallel then stamp onto each item
                var ratingTasks = all.Select(async i =>
                {
                    i.DonorAverageRating = await KapwaDataService.GetDonorAverageRating(i.Donor_ID);
                });
                await System.Threading.Tasks.Task.WhenAll(ratingTasks);
                _allItems = all;
                Application.Current.Dispatcher.Invoke(ApplyFilter);
            }
            catch { }
            finally { IsBusy = false; }
        }

        private void ApplyFilter()
        {
            Items.Clear();
            var q = _searchText.Trim().ToLower();
            foreach (var item in _allItems)
            {
                bool catOk = _filterCategory == "All" || item.Category_Name == _filterCategory;
                bool condOk = _filterCondition == "Any" || item.Item_Condition == _filterCondition;
                bool searchOk = string.IsNullOrEmpty(q) ||
                    item.Item_Name.ToLower().Contains(q) ||
                    item.Category_Name.ToLower().Contains(q) ||
                    item.Item_Description.ToLower().Contains(q);
                if (catOk && condOk && searchOk) Items.Add(item);
            }
        }
    }
}