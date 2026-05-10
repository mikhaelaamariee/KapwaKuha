// FILE: ClaimItemViewModel.cs
using System;
using System.Windows;
using System.Windows.Input;
using KapwaKuha.Commands;
using KapwaKuha.Models;
using KapwaKuha.Services;

namespace KapwaKuha.ViewModels
{
    public class ClaimItemViewModel : ObservableObject
    {
        private readonly string _beneficiaryId;
        private readonly string? _returnToDonorId;
        private readonly string? _returnToDonorName;
        private readonly Action? _onClaimSuccess;

        private ItemModel _item;
        public ItemModel Item
        {
            get => _item;
            set
            {
                _item = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ItemTitle));
                OnPropertyChanged(nameof(ItemCategory));
                OnPropertyChanged(nameof(ItemCondition));
                OnPropertyChanged(nameof(ItemDonorName));
                OnPropertyChanged(nameof(ItemImagePath));
                OnPropertyChanged(nameof(HasImage));
            }
        }

        // Exposed flattened props so XAML doesn't need nested bindings
        public string ItemTitle => Item?.Item_Name ?? "Unknown Item";
        public string ItemCategory => Item?.Category_Name ?? "";
        public string ItemCondition => Item?.Item_Condition ?? "";
        public string ItemDonorName => string.IsNullOrEmpty(Item?.Donor_Name)
                                        ? Item?.Donor_ID ?? ""
                                        : Item.Donor_Name;
        public string ItemImagePath => Item?.Item_ImagePath ?? "";
        public bool HasImage => !string.IsNullOrWhiteSpace(Item?.Item_ImagePath);

        private string _handoffType = "Pickup";
        private string _location = string.Empty;
        private string _eventName = string.Empty;
        private bool _isBusy;
        private string _errorMessage = string.Empty;
        private bool _errorVisible;

        public string HandoffType
        {
            get => _handoffType;
            set
            {
                _handoffType = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsPickup));
                OnPropertyChanged(nameof(IsDelivery));
                OnPropertyChanged(nameof(IsDonationDrive));
                OnPropertyChanged(nameof(ShowEventName));
            }
        }

        public bool IsPickup { get => _handoffType == "Pickup"; set { if (value) HandoffType = "Pickup"; } }
        public bool IsDelivery { get => _handoffType == "Delivery"; set { if (value) HandoffType = "Delivery"; } }
        public bool IsDonationDrive { get => _handoffType == "Donation Drive"; set { if (value) HandoffType = "Donation Drive"; } }

        public Visibility ShowEventName =>
            IsDonationDrive ? Visibility.Visible : Visibility.Collapsed;

        public string Location
        {
            get => _location;
            set { _location = value; OnPropertyChanged(); }
        }
        public string EventName
        {
            get => _eventName;
            set { _eventName = value; OnPropertyChanged(); }
        }
        public bool IsBusy
        {
            get => _isBusy;
            set { _isBusy = value; OnPropertyChanged(); }
        }
        public string ErrorMessage
        {
            get => _errorMessage;
            set { _errorMessage = value; OnPropertyChanged(); }
        }
        public bool ErrorVisible
        {
            get => _errorVisible;
            set { _errorVisible = value; OnPropertyChanged(); }
        }

        public ICommand BackCommand { get; }
        public ICommand ConfirmClaimCommand { get; }
        public ICommand SetPickupCommand { get; }
        public ICommand SetDeliveryCommand { get; }
        public ICommand SetDonationDriveCommand { get; }

        // Add this method to ClaimItemViewModel
        public void RefreshItemProps()
        {
            OnPropertyChanged(nameof(ItemTitle));
            OnPropertyChanged(nameof(ItemCategory));
            OnPropertyChanged(nameof(ItemCondition));
            OnPropertyChanged(nameof(ItemDonorName));
            OnPropertyChanged(nameof(ItemImagePath));
            OnPropertyChanged(nameof(HasImage));
            OnPropertyChanged(nameof(Item));
        }

        public ClaimItemViewModel(string beneficiaryId, ItemModel item,
      Action? onClaimSuccess = null,
      string? returnToDonorId = null,
      string? returnToDonorName = null)
        {
            _beneficiaryId = beneficiaryId;
            _item = item;
            _onClaimSuccess = onClaimSuccess;
            _returnToDonorId = returnToDonorId;
            _returnToDonorName = returnToDonorName;

            // Back: go to chat if came from chat, otherwise BrowseItems
            BackCommand = new RelayCommand(_ =>
            {
                if (!string.IsNullOrEmpty(_returnToDonorId))
                    NavigationService.Navigate(
                        new View.ChatWindow(_beneficiaryId, _returnToDonorId,
                                            _returnToDonorName ?? "", "Beneficiary"));
                else
                    NavigationService.Navigate(new View.BrowseItemsWindow(_beneficiaryId));
            });

            SetPickupCommand = new RelayCommand(_ => HandoffType = "Pickup");
            SetDeliveryCommand = new RelayCommand(_ => HandoffType = "Delivery");
            SetDonationDriveCommand = new RelayCommand(_ => HandoffType = "Donation Drive");

            ConfirmClaimCommand = new AsyncRelayCommand(async _ =>
            {
                ErrorVisible = false;

                if (string.IsNullOrWhiteSpace(Location))
                {
                    ErrorMessage = "Please enter a pickup/delivery location.";
                    ErrorVisible = true;
                    return;
                }
                if (IsDonationDrive && string.IsNullOrWhiteSpace(EventName))
                {
                    ErrorMessage = "Please enter the event name for a Donation Drive.";
                    ErrorVisible = true;
                    return;
                }

                var confirm = MessageBox.Show(
                    $"Claim this item?\n\nItem: {ItemTitle}\n" +
                    $"Category: {ItemCategory}\n" +
                    $"Condition: {ItemCondition}\n" +
                    $"Handoff: {HandoffType}\nLocation: {Location}" +
                    (IsDonationDrive ? $"\nEvent: {EventName}" : ""),
                    "Confirm Claim", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (confirm != MessageBoxResult.Yes) return;

                try
                {
                    IsBusy = true;

                    string claimId = await KapwaDataService.GetNextClaimId();
                    var claim = new ClaimModel
                    {
                        Claim_ID = claimId,
                        Item_ID = Item.Item_ID,
                        Item_Name = ItemTitle,
                        Item_ImagePath = ItemImagePath,
                        Beneficiary_ID = _beneficiaryId,
                        Beneficiary_Name = UserSession.FullName,
                        Claim_Date = DateTime.Now,
                        Claim_Status = "Pending",
                        Handoff_Type = HandoffType,
                        Verification_Notes = $"Location: {Location}" +
                                             (IsDonationDrive ? $" | Event: {EventName}" : "")
                    };

                    var (success, error) = await KapwaDataService.SaveClaim(claim);
                    if (!success)
                    {
                        ErrorMessage = error;
                        ErrorVisible = true;
                        return;
                    }

                    KapwaDataService.GenerateClaimReport(claim);

                    MessageBox.Show($"✅ Claimed! Your Claim ID: {claimId}",
                        "Success", MessageBoxButton.OK, MessageBoxImage.Information);

                    // Notify donor in chat
                    try
                    {
                        string chatMsg =
                            $"✅ I have accepted the donation! Claim ID: {claimId}.\n" +
                            $"Please confirm the handoff details.\n" +
                            $"Handoff: {HandoffType}" +
                            (string.IsNullOrEmpty(Location) ? "." : $" at {Location}.") +
                            (IsDonationDrive && !string.IsNullOrEmpty(EventName)
                                ? $" (Event: {EventName})" : "");

                        await KapwaDataService.SaveChatMessage(
                            _beneficiaryId, Item.Donor_ID, chatMsg);
                    }
                    catch { /* chat is optional, don't block */ }

                    _onClaimSuccess?.Invoke();

                    // Return to chat if came from there, otherwise beneficiary dashboard
                    if (!string.IsNullOrEmpty(_returnToDonorId))
                        NavigationService.Navigate(
                            new View.ChatWindow(_beneficiaryId, _returnToDonorId,
                                                _returnToDonorName ?? "", "Beneficiary"));
                    else
                        NavigationService.Navigate(
                            new View.BeneficiaryDashboardWindow(_beneficiaryId));
                }
                catch (Exception ex)
                {
                    ErrorMessage = "An unexpected error occurred: " + ex.Message;
                    ErrorVisible = true;
                }
                finally
                {
                    IsBusy = false;
                }
            });
        }
    }
}