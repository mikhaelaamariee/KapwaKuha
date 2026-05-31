// FILE: ViewModels/AdminDashboardViewModel.cs  (NEW)
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using KapwaKuha.Commands;
using KapwaKuha.Models;
using KapwaKuha.Services;

namespace KapwaKuha.ViewModels
{
    public class AdminDashboardViewModel : ObservableObject
    {
        private readonly string _adminId;

        // ── Identity ─────────────────────────────────────────────────────────
        public string WelcomeText { get; }

        // ── KPI metrics ──────────────────────────────────────────────────────
        private int _totalDonated, _totalClaimed, _activeItems, _fulfilledNeeds;
        private int _activeInstBenes, _activeIndepBenes;
        private int _pendingItems, _pendingBeneficiaries, _openReports;

        public int TotalDonated { get => _totalDonated; set { _totalDonated = value; OnPropertyChanged(); } }
        public int TotalClaimed { get => _totalClaimed; set { _totalClaimed = value; OnPropertyChanged(); } }
        public int ActiveItems { get => _activeItems; set { _activeItems = value; OnPropertyChanged(); } }
        public int FulfilledNeeds { get => _fulfilledNeeds; set { _fulfilledNeeds = value; OnPropertyChanged(); } }
        public int ActiveInstBenes { get => _activeInstBenes; set { _activeInstBenes = value; OnPropertyChanged(); } }
        public int ActiveIndepBenes { get => _activeIndepBenes; set { _activeIndepBenes = value; OnPropertyChanged(); } }
        public int PendingItems { get => _pendingItems; set { _pendingItems = value; OnPropertyChanged(); OnPropertyChanged(nameof(HasPendingItems)); } }
        public int PendingBeneficiaries { get => _pendingBeneficiaries; set { _pendingBeneficiaries = value; OnPropertyChanged(); OnPropertyChanged(nameof(HasPendingBeneficiaries)); } }
        public int OpenReports { get => _openReports; set { _openReports = value; OnPropertyChanged(); OnPropertyChanged(nameof(HasOpenReports)); } }

        public bool HasPendingItems => PendingItems > 0;
        public bool HasPendingBeneficiaries => PendingBeneficiaries > 0;
        public bool HasOpenReports => OpenReports > 0;

        // ── Gatekeeper queues ────────────────────────────────────────────────
        public ObservableCollection<ItemModel> PendingItemsList { get; } = new();
        public ObservableCollection<BeneficiaryModel> PendingBenesList { get; } = new();
        public ObservableCollection<UserReportModel> OpenReportsList { get; } = new();

        private bool _isLoadingItems, _isLoadingBenes, _isLoadingReports;
        public bool IsLoadingItems { get => _isLoadingItems; set { _isLoadingItems = value; OnPropertyChanged(); } }
        public bool IsLoadingBenes { get => _isLoadingBenes; set { _isLoadingBenes = value; OnPropertyChanged(); } }
        public bool IsLoadingReports { get => _isLoadingReports; set { _isLoadingReports = value; OnPropertyChanged(); } }

        // ── Commands ─────────────────────────────────────────────────────────
        public ICommand ApproveItemCommand { get; }
        public ICommand RejectItemCommand { get; }
        public ICommand ApproveBeneficiaryCommand { get; }
        public ICommand RejectBeneficiaryCommand { get; }
        public ICommand RefreshCommand { get; }
        public ICommand LogoutCommand { get; }

        public AdminDashboardViewModel(string adminId)
        {
            _adminId = adminId;
            WelcomeText = $"Admin Panel — {UserSession.FullName}";

            ApproveItemCommand = new AsyncRelayCommand(async param =>
            {
                if (param is not ItemModel item) return;
                var r = MessageBox.Show(
                    $"Approve item \"{item.Item_Name}\"?\nThis will make it visible to beneficiaries.",
                    "Confirm Approval", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (r != MessageBoxResult.Yes) return;
                try
                {
                    await KapwaDataService.ApproveItem(item.Item_ID);
                    // Notify donor
                    await KapwaDataService.CreateNotification(
                        item.Donor_ID, "Approval",
                        $"✅ Your item \"{item.Item_Name}\" has been approved and is now visible.",
                        item.Item_ID);
                    await LoadGatekeeperQueuesAsync();
                    await LoadMetricsAsync();
                }
                catch { }
            });

            RejectItemCommand = new AsyncRelayCommand(async param =>
            {
                if (param is not ItemModel item) return;
                var r = MessageBox.Show(
                    $"Reject item \"{item.Item_Name}\"?",
                    "Confirm Rejection", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (r != MessageBoxResult.Yes) return;
                try
                {
                    await KapwaDataService.RejectItem(item.Item_ID);
                    await KapwaDataService.CreateNotification(
                        item.Donor_ID, "Approval",
                        $"❌ Your item \"{item.Item_Name}\" was rejected. Please review and resubmit.",
                        item.Item_ID);
                    await LoadGatekeeperQueuesAsync();
                }
                catch { }
            });

            ApproveBeneficiaryCommand = new AsyncRelayCommand(async param =>
            {
                if (param is not BeneficiaryModel bene) return;
                var r = MessageBox.Show(
                    $"Approve beneficiary \"{bene.Beneficiary_FullName}\" from {bene.Organization_Name}?",
                    "Confirm Approval", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (r != MessageBoxResult.Yes) return;
                try
                {
                    await KapwaDataService.ApproveBeneficiary(bene.Beneficiary_ID);
                    await KapwaDataService.CreateNotification(
                        bene.Beneficiary_ID, "Approval",
                        "✅ Your account has been approved! You can now log in to KapwaKuha.",
                        bene.Beneficiary_ID);
                    await LoadGatekeeperQueuesAsync();
                    await LoadMetricsAsync();
                }
                catch { }
            });

            RejectBeneficiaryCommand = new AsyncRelayCommand(async param =>
            {
                if (param is not BeneficiaryModel bene) return;
                var r = MessageBox.Show(
                    $"Reject beneficiary \"{bene.Beneficiary_FullName}\"?",
                    "Confirm Rejection", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (r != MessageBoxResult.Yes) return;
                try
                {
                    await KapwaDataService.RejectBeneficiary(bene.Beneficiary_ID);
                    await LoadGatekeeperQueuesAsync();
                }
                catch { }
            });

            RefreshCommand = new AsyncRelayCommand(async _ =>
            {
                await LoadMetricsAsync();
                await LoadGatekeeperQueuesAsync();
            });

            LogoutCommand = new RelayCommand(_ =>
            {
                var r = MessageBox.Show("Log out of Admin Panel?", "Confirm Logout",
                    MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (r == MessageBoxResult.Yes)
                {
                    UserSession.Clear();
                    NavigationService.Navigate(new View.ChooseRoleWindow());
                }
            });

            _ = LoadMetricsAsync();
            _ = LoadGatekeeperQueuesAsync();
        }

        private async System.Threading.Tasks.Task LoadMetricsAsync()
        {
            try
            {
                var m = await KapwaDataService.GetAdminImpactMetrics();
                Application.Current.Dispatcher.Invoke(() =>
                {
                    TotalDonated = m.TotalDonated;
                    TotalClaimed = m.TotalClaimed;
                    ActiveItems = m.ActiveItems;
                    FulfilledNeeds = m.FulfilledNeeds;
                    ActiveInstBenes = m.ActiveInstBenes;
                    ActiveIndepBenes = m.ActiveIndepBenes;
                    PendingItems = m.PendingItems;
                    PendingBeneficiaries = m.PendingBeneficiaries;
                    OpenReports = m.OpenReports;
                });
            }
            catch { }
        }

        private async System.Threading.Tasks.Task LoadGatekeeperQueuesAsync()
        {
            IsLoadingItems = IsLoadingBenes = IsLoadingReports = true;
            try
            {
                var items = await KapwaDataService.GetPendingItems();
                var benes = await KapwaDataService.GetPendingBeneficiaries();
                var reports = await KapwaDataService.GetOpenReports();

                Application.Current.Dispatcher.Invoke(() =>
                {
                    PendingItemsList.Clear();
                    foreach (var i in items) PendingItemsList.Add(i);

                    PendingBenesList.Clear();
                    foreach (var b in benes) PendingBenesList.Add(b);

                    OpenReportsList.Clear();
                    foreach (var r in reports) OpenReportsList.Add(r);
                });
            }
            catch { }
            finally
            {
                IsLoadingItems = IsLoadingBenes = IsLoadingReports = false;
            }
        }
    }
}