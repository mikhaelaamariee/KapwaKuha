// FILE: ViewModels/ActiveListingsViewModel.cs
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

        public ObservableCollection<ItemModel> Items { get; } = new();
        private bool _isBusy;
        public bool IsBusy { get => _isBusy; set { _isBusy = value; OnPropertyChanged(); } }
        public string StatusMessage { get; private set; } = string.Empty;

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
            }
        }
        public bool IsItemSelected => SelectedItem != null;

        // Edit only allowed when item is still Available
        public bool CanEditSelected => SelectedItem?.Item_Status == "Available";

        // Add these properties to ActiveListingsViewModel:

        private string _editName = string.Empty;
        public string EditName
        {
            get => _editName;
            set { _editName = value; OnPropertyChanged(); }
        }

        private string _editDescription = string.Empty;
        public string EditDescription
        {
            get => _editDescription;
            set { _editDescription = value; OnPropertyChanged(); }
        }

        private string _editCondition = "Good";
        public string EditCondition
        {
            get => _editCondition;
            set { _editCondition = value; OnPropertyChanged(); }
        }

        private string _editImagePath = string.Empty;
        public string EditImagePath
        {
            get => _editImagePath;
            set { _editImagePath = value; OnPropertyChanged(); OnPropertyChanged(nameof(HasEditImage)); }
        }

        public bool HasEditImage =>
            !string.IsNullOrEmpty(_editImagePath) && System.IO.File.Exists(_editImagePath);

        private bool _isEditPanelOpen;
        public bool IsEditPanelOpen
        {
            get => _isEditPanelOpen;
            set { _isEditPanelOpen = value; OnPropertyChanged(); }
        }

        public ICommand OpenEditPanelCommand { get; }
        public ICommand BrowseEditImageCommand { get; }
        public ICommand SaveEditCommand { get; }
        public ICommand CancelEditCommand { get; }

        public string[] Conditions { get; } = { "New", "Good", "Fair", "Poor" };

        public ICommand BackCommand { get; }
        public ICommand RefreshCommand { get; }
        public ICommand DeleteItemCommand { get; }
        public ICommand EditPostCommand { get; }

        public ActiveListingsViewModel(string donorId)
        {
            _donorId = donorId;

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
                var r = MessageBox.Show($"Delete '{SelectedItem.Item_Name}'?", "Confirm Delete",
                    MessageBoxButton.YesNo, MessageBoxImage.Warning);
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

            // Replace the EditPostCommand = new AsyncRelayCommand(...) with:
            EditPostCommand = new RelayCommand(_ =>
            {
                if (SelectedItem == null) return;
                if (SelectedItem.Item_Status != "Available")
                {
                    MessageBox.Show("Only Available items can be edited.", "Cannot Edit",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                // Pre-fill edit form
                EditName = SelectedItem.Item_Name;
                EditDescription = SelectedItem.Item_Description;
                EditCondition = SelectedItem.Item_Condition;
                EditImagePath = SelectedItem.Item_ImagePath;
                IsEditPanelOpen = true;
            });

            OpenEditPanelCommand = EditPostCommand;  // alias

            BrowseEditImageCommand = new RelayCommand(_ =>
            {
                var dlg = new Microsoft.Win32.OpenFileDialog
                {
                    Filter = "Image files (*.png;*.jpg;*.jpeg;*.bmp)|*.png;*.jpg;*.jpeg;*.bmp",
                    Title = "Select New Item Image"
                };
                if (dlg.ShowDialog() == true) EditImagePath = dlg.FileName;
            });

            CancelEditCommand = new RelayCommand(_ => IsEditPanelOpen = false);

            SaveEditCommand = new AsyncRelayCommand(async _ =>
            {
                if (SelectedItem == null) return;
                if (string.IsNullOrWhiteSpace(EditName))
                {
                    MessageBox.Show("Item name cannot be empty.", "Validation",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                try
                {
                    IsBusy = true;
                    SelectedItem.Item_Name = EditName.Trim();
                    SelectedItem.Item_Description = EditDescription.Trim();
                    SelectedItem.Item_Condition = EditCondition;
                    SelectedItem.Item_ImagePath = EditImagePath;
                    await KapwaDataService.UpdateItem(SelectedItem);
                    IsEditPanelOpen = false;
                    MessageBox.Show("✅ Item updated successfully!", "Saved",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    await LoadItemsAsync();
                }
                catch { }
                finally { IsBusy = false; }
            });

            _ = LoadItemsAsync();
        }

        private async System.Threading.Tasks.Task LoadItemsAsync()
        {
            IsBusy = true;
            try
            {
                var items = await KapwaDataService.GetItemsByDonor(_donorId);
                Application.Current.Dispatcher.Invoke(() =>
                {
                    Items.Clear();
                    foreach (var i in items) Items.Add(i);
                    StatusMessage = $"{Items.Count} item(s) posted.";
                    OnPropertyChanged(nameof(StatusMessage));
                });
            }
            catch { }
            finally { IsBusy = false; }
        }
    }
}