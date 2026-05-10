// FILE: ClaimItemViewModel.cs
// Window: ClaimItemWindow.xaml
// Handles the claim flow: HandoffType selection + confirmation
// Parallel to ProcessReturnViewModel in CarRentals
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
        public ItemModel Item { get; }

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

        public bool IsPickup
        {
            get => _handoffType == "Pickup";
            set { if (value) HandoffType = "Pickup"; }
        }
        public bool IsDelivery
        {
            get => _handoffType == "Delivery";
            set { if (value) HandoffType = "Delivery"; }
        }
        public bool IsDonationDrive
        {
            get => _handoffType == "Donation Drive";
            set { if (value) HandoffType = "Donation Drive"; }
        }

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

        private readonly Action? _onClaimSuccess;

        public ICommand BackCommand { get; }
        public ICommand ConfirmClaimCommand { get; }
        public ICommand SetPickupCommand { get; }
        public ICommand SetDeliveryCommand { get; }
        public ICommand SetDonationDriveCommand { get; }

        public ClaimItemViewModel(string beneficiaryId, ItemModel item,
                           Action? onClaimSuccess = null)
        {
            _beneficiaryId = beneficiaryId;
            Item = item;
            _onClaimSuccess = onClaimSuccess;

            BackCommand = new RelayCommand(_ =>
                NavigationService.Navigate(new View.BrowseItemsWindow(_beneficiaryId)));

            SetPickupCommand = new RelayCommand(_ => HandoffType = "Pickup");
            SetDeliveryCommand = new RelayCommand(_ => HandoffType = "Delivery");
            SetDonationDriveCommand = new RelayCommand(_ => HandoffType = "Donation Drive");

            ConfirmClaimCommand = new AsyncRelayCommand(async _ =>
            {
                ErrorVisible = false;

                // ── Local validation ──────────────────────────────────────────
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
                    $"Claim this item?\n\nItem: {Item.Item_Name}\n" +
                    $"Handoff: {HandoffType}\nLocation: {Location}" +
                    (IsDonationDrive ? $"\nEvent: {EventName}" : ""),
                    "Confirm Claim", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (confirm != MessageBoxResult.Yes) return;
                try
                {
                    IsBusy = true;
                    ErrorVisible = false; // Reset any previous errors

                    string claimId = await KapwaDataService.GetNextClaimId();
                    var claim = new ClaimModel
                    {
                        Claim_ID = claimId,
                        Item_ID = Item.Item_ID,
                        Item_Name = Item.Item_Name,
                        Item_ImagePath = Item.Item_ImagePath,
                        Beneficiary_ID = _beneficiaryId,
                        Beneficiary_Name = UserSession.FullName,
                        Claim_Date = DateTime.Now,
                        Claim_Status = "Pending", // Status is set here!
                        Handoff_Type = HandoffType,
                        Verification_Notes = $"Location: {Location}" +
                                             (IsDonationDrive ? $" | Event: {EventName}" : "")
                    };

                    // ── SaveClaim now returns (bool Success, string Error) ─────
                    var (success, error) = await KapwaDataService.SaveClaim(claim);

                    if (!success)
                    {
                        // Show the trigger's friendly error — no crash, no generic message
                        ErrorMessage = error;
                        ErrorVisible = true;
                        return;
                    }

                    KapwaDataService.GenerateClaimReport(claim);

                    // Save proof of receipt to DB — use the generated report file path
                    string reportPath = System.IO.Path.Combine(
                        AppDomain.CurrentDomain.BaseDirectory,
                        "KapwaKuhaData", "ClaimReports", $"Claim_{claimId}.txt");
                    await KapwaDataService.SaveProofOfReceipt(claimId, reportPath);

                    MessageBox.Show($"✅ Claimed! Your Claim ID: {claimId}",
                        "Success", MessageBoxButton.OK, MessageBoxImage.Information);

                    // Auto-confirm back to donor in chat
                    try
                    {
                        string chatMessage = $"⏳ I have confirmed my claim! (Claim ID: {claimId}).\n" +
                                             $"I am now waiting to receive the item via {HandoffType}" +
                                             (string.IsNullOrEmpty(Location) ? "." : $" at {Location}.");

                        if (IsDonationDrive && !string.IsNullOrEmpty(EventName))
                        {
                            chatMessage += $" (Event: {EventName})";
                        }

                        await KapwaDataService.SaveChatMessage(_beneficiaryId, Item.Donor_ID, chatMessage);
                    }
                    catch { /* chat message optional — don't block the claim */ }


                    // Call the chat callback so buttons disappear only after successful claim
                    _onClaimSuccess?.Invoke();

                    // Navigate to dashboard ONLY ONCE (removed the duplicate line)
                    NavigationService.Navigate(new View.BeneficiaryDashboardWindow(_beneficiaryId));
                }
                catch (Exception ex)
                {
                    // Show unexpected errors instead of silently swallowing them
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