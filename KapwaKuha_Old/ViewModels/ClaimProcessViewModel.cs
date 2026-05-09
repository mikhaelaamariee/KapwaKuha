using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using KapwaKuha.Commands;
using KapwaKuha.Models;
using KapwaKuha.Services;

namespace KapwaKuha.ViewModels
{
    // Parallel to ProcessReturnViewModel in CarRentals.
    // "Found Items" list  ≈  "Active Rentals" list
    // SelectedItem        ≈  SelectedRental
    // ProcessClaimCommand ≈  ReturnCommand
    public class ClaimProcessViewModel : ObservableObject
    {
        private readonly string _adminId;
        public string AdminLabel { get; }

        // ── Found Items list ──────────────────────────────────────────────────
        public ObservableCollection<ItemModel> FoundItems { get; } = new();

        private ItemModel? _selectedItem;
        public ItemModel? SelectedItem
        {
            get => _selectedItem;
            set
            {
                _selectedItem = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsItemSelected));
                OnPropertyChanged(nameof(StorageDaysDisplay));
            }
        }

        public bool IsItemSelected => SelectedItem != null;
        public string StorageDaysDisplay => SelectedItem?.StorageDaysDisplay
                                             ?? "← Select a Found item";

        // ── Beneficiary ComboBox ──────────────────────────────────────────────
        public ObservableCollection<BeneficiaryRow> Beneficiaries { get; } = new();

        private BeneficiaryRow? _selectedBeneficiary;
        public BeneficiaryRow? SelectedBeneficiary
        {
            get => _selectedBeneficiary;
            set { _selectedBeneficiary = value; OnPropertyChanged(); }
        }

        // ── Verification Notes ────────────────────────────────────────────────
        private string _notes = string.Empty;
        public string VerificationNotes
        {
            get => _notes;
            set { _notes = value; OnPropertyChanged(); }
        }

        // ── Status / Busy ─────────────────────────────────────────────────────
        private string _statusMsg = string.Empty;
        public string StatusMessage
        {
            get => _statusMsg;
            set { _statusMsg = value; OnPropertyChanged(); }
        }

        private bool _isBusy;
        public bool IsBusy
        {
            get => _isBusy;
            set { _isBusy = value; OnPropertyChanged(); }
        }

        // ── Commands ──────────────────────────────────────────────────────────
        public ICommand BackCommand { get; }
        public ICommand RefreshCommand { get; }
        public ICommand ProcessClaimCommand { get; }

        public ClaimProcessViewModel(string adminId)
        {
            _adminId = adminId;
            AdminLabel = $"Agent: {adminId}";

            BackCommand = new RelayCommand(_ =>
                NavigationService.Navigate(new View.DonorDashboardWindow(_adminId)));

            RefreshCommand = new RelayCommand(_ => LoadFoundItemsAsync());

            ProcessClaimCommand = new AsyncRelayCommand(async _ =>
            {
                if (SelectedItem == null)
                { MessageBox.Show("Select a Found item.", "No Item", MessageBoxButton.OK, MessageBoxImage.Warning); return; }

                if (SelectedBeneficiary == null)
                { MessageBox.Show("Select a Beneficiary.", "No Beneficiary", MessageBoxButton.OK, MessageBoxImage.Warning); return; }

                if (string.IsNullOrWhiteSpace(VerificationNotes))
                { MessageBox.Show("Enter verification notes.", "Notes Required", MessageBoxButton.OK, MessageBoxImage.Warning); return; }

                var confirm = MessageBox.Show(
                    $"Item : {SelectedItem.Item_Name}\n" +
                    $"To   : {SelectedBeneficiary.DisplayName}\n" +
                    $"Storage: {SelectedItem.StorageDaysDisplay}\n\n" +
                    $"Release this item?",
                    "Confirm Release", MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (confirm != MessageBoxResult.Yes) return;

                try
                {
                    IsBusy = true;

                    string claimId = await KapwaDataService.GetNextClaimId();

                    var claim = new ClaimModel
                    {
                        Claim_ID = claimId,
                        Item_ID = SelectedItem.Item_ID,
                        Item_Name = SelectedItem.Item_Name,
                        Beneficiary_ID = SelectedBeneficiary.Id,
                        Beneficiary_Name = SelectedBeneficiary.DisplayName,
                        Claim_Date = DateTime.Now,
                        Claim_Status = "Released",
                        Verification_Notes = VerificationNotes.Trim()
                    };

                    await KapwaDataService.SaveClaim(claim);
                    await KapwaDataService.UpdateItemStatus(SelectedItem.Item_ID, "Claimed");
                    KapwaDataService.GenerateClaimReport(claim);

                    FoundItems.Remove(SelectedItem);
                    SelectedItem = null;
                    SelectedBeneficiary = null;
                    VerificationNotes = string.Empty;

                    MessageBox.Show($"✅ Released!  Claim ID: {claimId}",
                        "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    StatusMessage = $"Last processed: {claimId}  {DateTime.Now:HH:mm}";
                }
                catch (Exception ex)
                { MessageBox.Show("Process failed: " + ex.Message); }
                finally
                { IsBusy = false; }
            });

            LoadFoundItemsAsync();
            LoadBeneficiariesAsync();
        }

        private async void LoadFoundItemsAsync()
        {
            IsBusy = true;
            try
            {
                var items = await KapwaDataService.GetFoundItems();
                Application.Current.Dispatcher.Invoke(() =>
                {
                    FoundItems.Clear();
                    foreach (var i in items) FoundItems.Add(i);
                    StatusMessage = $"{FoundItems.Count} item(s) awaiting claim.";
                });
            }
            catch (Exception ex) { StatusMessage = "Load failed."; MessageBox.Show(ex.Message); }
            finally { IsBusy = false; }
        }

        private async void LoadBeneficiariesAsync()
        {
            var rows = await KapwaDataService.GetActiveBeneficiaries();
            Application.Current.Dispatcher.Invoke(() =>
            {
                Beneficiaries.Clear();
                foreach (var (id, name) in rows)
                    Beneficiaries.Add(new BeneficiaryRow { Id = id, DisplayName = name });
            });
        }
    }

    public class BeneficiaryRow
    {
        public string Id { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
    }
}