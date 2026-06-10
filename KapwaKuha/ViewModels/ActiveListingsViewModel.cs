// FILE: ViewModels/ActiveListingsViewModel.cs
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using KapwaKuha.Commands;
using KapwaKuha.Models;
using KapwaKuha.Services;

namespace KapwaKuha.ViewModels
{
    public class ActiveListingsViewModel : ObservableObject
    {
        private readonly string _donorId;
        private List<ItemModel> _allItems = new();

        public ObservableCollection<ItemModel> Items { get; } = new();

        private bool _isBusy;
        public bool IsBusy { get => _isBusy; set { _isBusy = value; OnPropertyChanged(); } }

        private string _statusMessage = string.Empty;
        public string StatusMessage
        {
            get => _statusMessage;
            private set { _statusMessage = value; OnPropertyChanged(); }
        }

        // ── Search + filter ──────────────────────────────────────────────────
        private string _searchText = string.Empty;
        public string SearchText
        {
            get => _searchText;
            set { _searchText = value; OnPropertyChanged(); ApplyFilter(); }
        }

        private string _filterStatus = "All";
        public string FilterStatus
        {
            get => _filterStatus;
            set { _filterStatus = value; OnPropertyChanged(); ApplyFilter(); }
        }

        // ── Selection ────────────────────────────────────────────────────────
        private ItemModel? _selectedItem;
        public ItemModel? SelectedItem
        {
            get => _selectedItem;
            set
            {
                _selectedItem = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsItemSelected));
                OnPropertyChanged(nameof(CanEditSelected));
                OnPropertyChanged(nameof(CanDeleteSelected));
            }
        }
        public bool IsItemSelected => SelectedItem != null;

        // Edit allowed: Pending OR Rejected (not Approved, not Claimed/Reserved)
        public bool CanEditSelected => SelectedItem?.CanDonorEdit == true;

        // Delete allowed: Available status (any approval state)
        public bool CanDeleteSelected => SelectedItem?.Item_Status == "Available";

        // ── Edit panel fields ────────────────────────────────────────────────
        private string _editName = string.Empty;
        private string _editDescription = string.Empty;
        private string _editCondition = "Good";
        private string _editImagePath = string.Empty;
        private bool _isEditPanelOpen;

        public string EditName
        {
            get => _editName;
            set { _editName = value; OnPropertyChanged(); }
        }
        public string EditDescription
        {
            get => _editDescription;
            set { _editDescription = value; OnPropertyChanged(); }
        }
        public string EditCondition
        {
            get => _editCondition;
            set { _editCondition = value; OnPropertyChanged(); }
        }
        public string EditImagePath
        {
            get => _editImagePath;
            set { _editImagePath = value; OnPropertyChanged(); OnPropertyChanged(nameof(HasEditImage)); }
        }
        public bool HasEditImage => !string.IsNullOrEmpty(_editImagePath)
                                    && System.IO.File.Exists(_editImagePath);
        public bool IsEditPanelOpen
        {
            get => _isEditPanelOpen;
            set { _isEditPanelOpen = value; OnPropertyChanged(); }
        }

        public string PinpointItemId { get; }

        // ── Commands ─────────────────────────────────────────────────────────
        public ICommand BackCommand { get; }
        public ICommand RefreshCommand { get; }
        public ICommand DeleteItemCommand { get; }
        public ICommand OpenEditPanelCommand { get; }
        public ICommand BrowseEditImageCommand { get; }
        public ICommand SaveEditCommand { get; }
        public ICommand CancelEditCommand { get; }
        public ICommand EditPostCommand { get; }

        public ObservableCollection<string> Conditions { get; } = new() { "New", "Good", "Fair", "Poor" };

        public ActiveListingsViewModel(string donorId) : this(donorId, string.Empty) { }

        public ActiveListingsViewModel(string donorId, string pinpointItemId)
        {
            _donorId = donorId;
            PinpointItemId = pinpointItemId;

            BackCommand = new RelayCommand(_ =>
                NavigationService.Navigate(new View.DonorDashboardWindow(_donorId)));

            RefreshCommand = new AsyncRelayCommand(async _ => await LoadItemsAsync());

            DeleteItemCommand = new AsyncRelayCommand(async _ =>
            {
                if (SelectedItem == null) return;
                if (SelectedItem.Item_Status != "Available")
                {
                    MessageBox.Show("Only Available items can be deleted.", "Cannot Delete",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                var r = MessageBox.Show($"Delete '{SelectedItem.Item_Name}'?",
                    "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (r != MessageBoxResult.Yes) return;
                try
                {
                    IsBusy = true;
                    await KapwaDataService.DeleteItem(SelectedItem.Item_ID);
                    Items.Remove(SelectedItem);
                    SelectedItem = null;
                    MessageBox.Show("Item deleted.", "Done",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch { }
                finally { IsBusy = false; }
            });

            OpenEditPanelCommand = new RelayCommand(_ =>
            {
                if (SelectedItem == null) return;
                EditName = SelectedItem.Item_Name;
                EditDescription = SelectedItem.Item_Description;
                EditCondition = SelectedItem.Item_Condition;
                EditImagePath = SelectedItem.Item_ImagePath;
                IsEditPanelOpen = true;
            });

            CancelEditCommand = new RelayCommand(_ => IsEditPanelOpen = false);

            BrowseEditImageCommand = new RelayCommand(_ =>
            {
                var dlg = new Microsoft.Win32.OpenFileDialog
                {
                    Filter = "Images (*.png;*.jpg;*.jpeg;*.bmp)|*.png;*.jpg;*.jpeg;*.bmp",
                    Title = "Select Item Image"
                };
                if (dlg.ShowDialog() == true) EditImagePath = dlg.FileName;
            });

            SaveEditCommand = new AsyncRelayCommand(async _ =>
            {
                if (SelectedItem == null) return;
                if (string.IsNullOrWhiteSpace(EditName))
                {
                    MessageBox.Show("Name cannot be empty.", "Validation",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                try
                {
                    IsBusy = true;
                    SelectedItem.Item_Name = EditName;
                    SelectedItem.Item_Description = EditDescription;
                    SelectedItem.Item_Condition = EditCondition;
                    SelectedItem.Item_ImagePath = EditImagePath;

                    await KapwaDataService.UpdateItem(SelectedItem);
                    IsEditPanelOpen = false;

                    MessageBox.Show(
                        "✅ Your item has been updated and resubmitted for admin review.\n\nIt will reappear in listings once re-approved.",
                        "Resubmitted for Approval", MessageBoxButton.OK, MessageBoxImage.Information);

                    await LoadItemsAsync();
                }
                catch { }
                finally { IsBusy = false; }
            });

            EditPostCommand = OpenEditPanelCommand;

            _ = LoadItemsAsync();
        }

        // ── Data helpers ──────────────────────────────────────────────────────
        private async System.Threading.Tasks.Task LoadItemsAsync()
        {
            IsBusy = true;
            try
            {
                var items = await KapwaDataService.GetItemsByDonor(_donorId);
                Application.Current.Dispatcher.Invoke(() =>
                {
                    _allItems = items;
                    ApplyFilter();

                    if (!string.IsNullOrEmpty(PinpointItemId))
                    {
                        foreach (var item in Items)
                        {
                            if (item.Item_ID == PinpointItemId)
                            {
                                SelectedItem = item;
                                break;
                            }
                        }
                    }
                });
            }
            catch { }
            finally { IsBusy = false; }
        }

        private void ApplyFilter()
        {
            Items.Clear();
            var q = _searchText.Trim().ToLower();
            foreach (var i in _allItems)
            {
                bool matchSearch = string.IsNullOrEmpty(q) ||
                                   i.Item_Name.ToLower().Contains(q) ||
                                   i.Item_Description.ToLower().Contains(q) ||
                                   i.Category_Name.ToLower().Contains(q);

                bool matchStatus = _filterStatus == "All" ||
                                   i.Item_Status == _filterStatus;

                if (matchSearch && matchStatus) Items.Add(i);
            }
            StatusMessage = $"{Items.Count} item(s) shown.";
        }
    }
}