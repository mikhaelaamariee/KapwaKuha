// FILE: ViewModels/PostItemViewModel.cs
using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using KapwaKuha.Commands;
using KapwaKuha.Models;
using KapwaKuha.Services;
using System.Linq;

namespace KapwaKuha.ViewModels
{
    public class PostItemViewModel : ObservableObject
    {
        private readonly string _donorId;
        private readonly bool _lockDirect;

        private string _itemName = string.Empty;
        private string _selectedCategory = string.Empty;
        private string _selectedCondition = "Good";
        private string _postType = "GeneralPost";
        private string _selectedBeneficiaryId = string.Empty;
        private BeneficiaryRow? _selectedBeneficiary;
        private string _description = string.Empty;
        private string _imagePath = string.Empty;
        private bool _isBusy;
        private string _errorMessage = string.Empty;
        private bool _errorVisible;



        public string ItemName
        {
            get => _itemName;
            set { _itemName = value; OnPropertyChanged(); }
        }
        public string SelectedCategory
        {
            get => _selectedCategory;
            set { _selectedCategory = value; OnPropertyChanged(); }
        }
        public string SelectedCondition
        {
            get => _selectedCondition;
            set { _selectedCondition = value; OnPropertyChanged(); }
        }
        public string PostType
        {
            get => _postType;
            set
            {
                if (_lockDirect) return;   // lock prevents switching
                _postType = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsDirectTarget));
                OnPropertyChanged(nameof(IsGeneralPost));
            }
        }
        public bool IsDirectTarget
        {
            get => _postType == "DirectTarget";
            set { if (value) PostType = "DirectTarget"; }
        }
        public bool IsGeneralPost
        {
            get => _postType == "GeneralPost";
            set { if (value) PostType = "GeneralPost"; }
        }

        // When navigated from HighPriorityNeeds — mode buttons are disabled
        public bool IsModeLocked => _lockDirect;

        public string SelectedBeneficiaryId
        {
            get => _selectedBeneficiaryId;
            set { _selectedBeneficiaryId = value; OnPropertyChanged(); }
        }
        public BeneficiaryRow? SelectedBeneficiary
        {
            get => _selectedBeneficiary;
            set
            {
                _selectedBeneficiary = value;
                OnPropertyChanged();
                SelectedBeneficiaryId = value?.Id ?? string.Empty;
                OnPropertyChanged(nameof(LockedBeneficiaryDisplay)); 
            }
        }
        public string Description
        {
            get => _description;
            set { _description = value; OnPropertyChanged(); }
        }
        public string ImagePath
        {
            get => _imagePath;
            set { _imagePath = value; OnPropertyChanged(); OnPropertyChanged(nameof(HasImage)); }
        }
        public bool HasImage => !string.IsNullOrEmpty(_imagePath);
        public bool IsBusy { get => _isBusy; set { _isBusy = value; OnPropertyChanged(); } }
        public string ErrorMessage { get => _errorMessage; set { _errorMessage = value; OnPropertyChanged(); } }
        public bool ErrorVisible { get => _errorVisible; set { _errorVisible = value; OnPropertyChanged(); } }

        private readonly string _linkedNeedsPostId;

        public bool IsComboLocked => _lockDirect;

        public string DonorLabel => $"Donor: {UserSession.Username}";

        // Shown in read-only TextBox when mode is locked (from HighPriorityNeeds)
        // Updates whenever SelectedBeneficiary changes
        public string LockedBeneficiaryDisplay =>
     _lockDirect && _selectedBeneficiary != null
         ? $"{_selectedBeneficiary.DisplayName}  (ID: {_selectedBeneficiary.Id})"
         : string.Empty;

        public ObservableCollection<string> Categories { get; } = new();
        public ObservableCollection<string> Conditions { get; } = new() { "New", "Good", "Fair", "Poor" };
        public ObservableCollection<BeneficiaryRow> Beneficiaries { get; } = new();

        public ICommand BackCommand { get; }
        public ICommand SaveCommand { get; }
        public ICommand BrowseImageCommand { get; }
        public ICommand SetGeneralPostCommand { get; }
        public ICommand SetDirectTargetCommand { get; }

        public PostItemViewModel(string donorId, string prefillTitle = "",
                 string lockedOrgId = "", bool lockDirect = false,
                 string lockedBeneficiaryId = "",
                 string linkedNeedsPostId = "")
        {
            _donorId = donorId;
            _lockDirect = lockDirect;
            _linkedNeedsPostId = linkedNeedsPostId;
            if (lockDirect) _postType = "DirectTarget";
            if (!string.IsNullOrEmpty(prefillTitle)) ItemName = prefillTitle;

            // If called from HighPriorityNeeds, force DirectTarget immediately
            if (lockDirect) _postType = "DirectTarget";

            BackCommand = new RelayCommand(_ =>
                NavigationService.Navigate(new View.DonorDashboardWindow(_donorId)));

            SetGeneralPostCommand = new RelayCommand(_ => { if (!_lockDirect) PostType = "GeneralPost"; });
            SetDirectTargetCommand = new RelayCommand(_ => PostType = "DirectTarget");

            BrowseImageCommand = new RelayCommand(_ =>
            {
                var dlg = new Microsoft.Win32.OpenFileDialog
                {
                    Filter = "Image files (*.png;*.jpg;*.jpeg;*.bmp)|*.png;*.jpg;*.jpeg;*.bmp",
                    Title = "Select Item Image"
                };
                if (dlg.ShowDialog() == true) ImagePath = dlg.FileName;
            });

            SaveCommand = new AsyncRelayCommand(async _ =>
            {
                ErrorVisible = false;
                if (string.IsNullOrWhiteSpace(ItemName))
                { ErrorMessage = "Item name is required."; ErrorVisible = true; return; }
                if (string.IsNullOrWhiteSpace(SelectedCategory))
                { ErrorMessage = "Please select a category."; ErrorVisible = true; return; }
                if (IsDirectTarget && string.IsNullOrWhiteSpace(SelectedBeneficiaryId))
                { ErrorMessage = "Please select a target beneficiary."; ErrorVisible = true; return; }
                if (string.IsNullOrWhiteSpace(ImagePath))
                { ErrorMessage = "Please attach an image of the item."; ErrorVisible = true; return; }
          
                if (string.IsNullOrWhiteSpace(Description))
                { ErrorMessage = "Caption/description is required."; ErrorVisible = true; return; }

                var confirm = MessageBox.Show(
                    $"Post item?\n\nName: {ItemName}\nCategory: {SelectedCategory}\n" +
                    $"Condition: {SelectedCondition}\nMode: {(IsDirectTarget ? "Direct Target" : "General Post")}",
                    "Confirm Post", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (confirm != MessageBoxResult.Yes) return;

                try
                {
                    IsBusy = true;
                    string itemId = await KapwaDataService.GetNextItemId();
                    string? catId = await KapwaDataService.GetCategoryId(SelectedCategory);

                    var item = new ItemModel
                    {
                        Item_ID = itemId,
                        Item_Name = ItemName.Trim(),
                        Item_Description = Description.Trim(),
                        Item_ImagePath = ImagePath,
                        Item_Condition = SelectedCondition,
                        Item_Status = "Available",
                        Date_Found = DateTime.Now,
                        Donor_ID = _donorId,
                        Category_ID = catId ?? "",
                        PostType = PostType,
                        TargetBeneficiary_ID = IsDirectTarget ? SelectedBeneficiaryId : ""
                    };

                    await KapwaDataService.AddItem(item);

                    await KapwaDataService.CreateNotification(
                        "A001", "Approval",
                        $"📦 New item pending approval: \"{ItemName.Trim()}\" ({SelectedCondition}) submitted by {_donorId}",
                        itemId);

                    string postTypeLabel = IsDirectTarget ? "Direct donation" : "General post";


                    MessageBox.Show(
                        $"📋 Item submitted for admin review!\n\n" +
                        $"Item: {ItemName}\n" +
                        $"Type: {postTypeLabel}\n" +
                        $"ID: {itemId}\n\n" +
                        "It will go live once an admin approves it. Check your Active Listings for status.",
                        "Pending Admin Approval", MessageBoxButton.OK, MessageBoxImage.Information);
                    NavigationService.Navigate(new View.DonorDashboardWindow(_donorId));
                }
                catch { }
                finally { IsBusy = false; }
            });

            if (!string.IsNullOrEmpty(prefillTitle)) ItemName = prefillTitle;

            LoadData(lockedOrgId, lockedBeneficiaryId);  // pass through
        }

        private async void LoadData(string lockedOrgId = "", string lockedBeneficiaryId = "")
        {
            var cats = await KapwaDataService.GetAllCategories();
            Application.Current.Dispatcher.Invoke(() =>
            {
                Categories.Clear();
                foreach (var c in cats) Categories.Add(c);
                if (Categories.Count > 0) SelectedCategory = Categories[0];
            });

            // Load beneficiaries:
            // If locked to a specific org — load only that org's members
            // If a specific beneficiary ID is provided — pre-select it exactly
            var benes = string.IsNullOrEmpty(lockedOrgId)
                ? await KapwaDataService.GetActiveBeneficiaries()
                : (await KapwaDataService.GetBeneficiariesByOrg(lockedOrgId))
                      .Select(b => (b.Id, b.DisplayName)).ToList();

            Application.Current.Dispatcher.Invoke(() =>
            {
                Beneficiaries.Clear();
                foreach (var (id, name) in benes)
                    Beneficiaries.Add(new BeneficiaryRow { Id = id, DisplayName = name });

                // Exact match on the provided ID (fully dynamic — no hardcoding)
                if (!string.IsNullOrEmpty(lockedBeneficiaryId))
                {
                    var match = Beneficiaries.FirstOrDefault(b => b.Id == lockedBeneficiaryId);
                    if (match != null) SelectedBeneficiary = match;
                    else if (Beneficiaries.Count > 0) SelectedBeneficiary = Beneficiaries[0];
                }
                else if (Beneficiaries.Count > 0 && _lockDirect)
                {
                    SelectedBeneficiary = Beneficiaries[0];
                }
            });
        }
    }
}