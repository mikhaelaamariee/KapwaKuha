// FILE: ViewModels/AdminDashboardViewModel.cs
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

        public int PendingItems
        {
            get => _pendingItems;
            set { _pendingItems = value; OnPropertyChanged(); OnPropertyChanged(nameof(HasPendingItems)); }
        }
        public int PendingBeneficiaries
        {
            get => _pendingBeneficiaries;
            set { _pendingBeneficiaries = value; OnPropertyChanged(); OnPropertyChanged(nameof(HasPendingBeneficiaries)); }
        }
        public int OpenReports
        {
            get => _openReports;
            set { _openReports = value; OnPropertyChanged(); OnPropertyChanged(nameof(HasOpenReports)); }
        }

        public bool HasPendingItems => PendingItems > 0;
        public bool HasPendingBeneficiaries => PendingBeneficiaries > 0;
        public bool HasOpenReports => OpenReports > 0;

        // ── Gatekeeper queues ────────────────────────────────────────────────
        public ObservableCollection<ItemModel> PendingItemsList { get; } = new();
        public ObservableCollection<BeneficiaryModel> PendingBenesList { get; } = new();
        public ObservableCollection<DonorModel> PendingDonorsList { get; } = new();
        public ObservableCollection<NeedsPostModel> PendingNeedsPostsList { get; } = new();
        public ObservableCollection<UserReportModel> OpenReportsList { get; } = new();

        private int _pendingDonors;
        public int PendingDonors
        {
            get => _pendingDonors;
            set { _pendingDonors = value; OnPropertyChanged(); OnPropertyChanged(nameof(HasPendingDonors)); }
        }
        public bool HasPendingDonors => PendingDonors > 0;

        private int _pendingNeedsPosts;
        public int PendingNeedsPosts
        {
            get => _pendingNeedsPosts;
            set { _pendingNeedsPosts = value; OnPropertyChanged(); OnPropertyChanged(nameof(HasPendingNeedsPosts)); }
        }
        public bool HasPendingNeedsPosts => PendingNeedsPosts > 0;

        private bool _isLoadingItems, _isLoadingBenes, _isLoadingReports, _isLoadingNeedsPosts;
        public bool IsLoadingItems { get => _isLoadingItems; set { _isLoadingItems = value; OnPropertyChanged(); } }
        public bool IsLoadingBenes { get => _isLoadingBenes; set { _isLoadingBenes = value; OnPropertyChanged(); } }
        public bool IsLoadingReports { get => _isLoadingReports; set { _isLoadingReports = value; OnPropertyChanged(); } }
        public bool IsLoadingNeedsPosts { get => _isLoadingNeedsPosts; set { _isLoadingNeedsPosts = value; OnPropertyChanged(); } }

        // ── Error state ───────────────────────────────────────────────────────
        private string _loadError = string.Empty;
        public string LoadError
        {
            get => _loadError;
            set { _loadError = value; OnPropertyChanged(); OnPropertyChanged(nameof(HasLoadError)); }
        }
        public bool HasLoadError => !string.IsNullOrEmpty(LoadError);

        // ── Commands ─────────────────────────────────────────────────────────
        public ICommand ApproveItemCommand { get; }
        public ICommand RejectItemCommand { get; }
        public ICommand ApproveBeneficiaryCommand { get; }
        public ICommand RejectBeneficiaryCommand { get; }
        public ICommand ApproveDonorCommand { get; }
        public ICommand RejectDonorCommand { get; }
        public ICommand ApproveNeedsPostCommand { get; }
        public ICommand RejectNeedsPostCommand { get; }
        public ICommand ProcessReportCommand { get; }
        public ICommand AdminBanUserCommand { get; }
        public ICommand RefreshCommand { get; }
        public ICommand LogoutCommand { get; }

        public AdminDashboardViewModel(string adminId)
        {
            _adminId = adminId;
            WelcomeText = $"Admin Panel — {UserSession.FullName}";

            // ── Items ─────────────────────────────────────────────────────────
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
                    await KapwaDataService.CreateNotification(
                        item.Donor_ID, "Approval",
                        $"✅ Your item \"{item.Item_Name}\" has been approved and is now visible.",
                        item.Item_ID);
                    await LoadGatekeeperQueuesAsync();
                    await LoadMetricsAsync();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to approve item: {ex.Message}", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            });

            RejectItemCommand = new AsyncRelayCommand(async param =>
            {
                if (param is not ItemModel item) return;

                // Ask admin for a rejection reason
                string reason = Microsoft.VisualBasic.Interaction.InputBox(
                    $"Enter rejection reason for \"{item.Item_Name}\":\n(The donor will see this and can edit + resubmit.)",
                    "Rejection Reason",
                    "Please revise your item post and resubmit.");

                if (reason == null) return; // cancelled
                if (string.IsNullOrWhiteSpace(reason))
                    reason = "Your item was rejected. Please edit and resubmit.";

                var r = MessageBox.Show(
                    $"Reject \"{item.Item_Name}\"?\n\nReason: {reason}",
                    "Confirm Rejection", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (r != MessageBoxResult.Yes) return;

                try
                {
                    await KapwaDataService.RejectItem(item.Item_ID, reason);
                    await KapwaDataService.CreateNotification(
                        item.Donor_ID, "Approval",
                        $"❌ Your item \"{item.Item_Name}\" was rejected.\n\nReason: {reason}\n\nPlease edit your listing and resubmit for re-review.",
                        item.Item_ID);
                    await LoadGatekeeperQueuesAsync();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to reject item: {ex.Message}", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            });

            // ── Beneficiaries ─────────────────────────────────────────────────
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
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to approve beneficiary: {ex.Message}", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
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
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to reject beneficiary: {ex.Message}", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            });

            // ── Donors ────────────────────────────────────────────────────────
            ApproveDonorCommand = new AsyncRelayCommand(async param =>
            {
                if (param is not DonorModel donor) return;
                var r = MessageBox.Show(
                    $"Approve donor account for \"{donor.Donor_FullName}\" (@{donor.Donor_Username})?",
                    "Approve Donor", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (r != MessageBoxResult.Yes) return;
                try
                {
                    await KapwaDataService.ApproveDonor(donor.Donor_ID);
                    await KapwaDataService.CreateNotification(
                        donor.Donor_ID, "Approval",
                        "✅ Your donor account has been approved! You can now post items.",
                        donor.Donor_ID);
                    await LoadGatekeeperQueuesAsync();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to approve donor: {ex.Message}", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            });

            RejectDonorCommand = new AsyncRelayCommand(async param =>
            {
                if (param is not DonorModel donor) return;
                var r = MessageBox.Show(
                    $"Reject donor account for \"{donor.Donor_FullName}\"?",
                    "Reject Donor", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (r != MessageBoxResult.Yes) return;
                try
                {
                    await KapwaDataService.RejectDonor(donor.Donor_ID);
                    await LoadGatekeeperQueuesAsync();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to reject donor: {ex.Message}", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            });

            // ── Needs Posts ───────────────────────────────────────────────────
            ApproveNeedsPostCommand = new AsyncRelayCommand(async param =>
            {
                if (param is not NeedsPostModel post) return;

                string chosenUrgency = post.Urgency ?? "Medium";

                var result = MessageBox.Show(
                    $"Approve needs post \"{post.Title}\" from {post.Org_Name}?\n\n" +
                    $"Submitted urgency: {post.Urgency}\n\n" +
                    "Click YES to approve at this urgency level.\n" +
                    "Click NO to approve but override urgency (you will be prompted).",
                    "Approve Needs Post", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);

                if (result == MessageBoxResult.Cancel) return;

                if (result == MessageBoxResult.No)
                {
                    var highResult = MessageBox.Show(
                        "Set urgency to HIGH (🔴 Urgent)?\n\nYes = High  |  No = proceed to next option",
                        "Set Urgency", MessageBoxButton.YesNo, MessageBoxImage.Question);
                    if (highResult == MessageBoxResult.Yes)
                        chosenUrgency = "High";
                    else
                    {
                        var medResult = MessageBox.Show(
                            "Set urgency to MEDIUM (🟡 Moderate)?\n\nYes = Medium  |  No = Low",
                            "Set Urgency", MessageBoxButton.YesNo, MessageBoxImage.Question);
                        chosenUrgency = medResult == MessageBoxResult.Yes ? "Medium" : "Low";
                    }
                }

                try
                {
                    await KapwaDataService.ApproveNeedsPost(post.NeedsPost_ID, chosenUrgency);
                    await KapwaDataService.CreateNotification(
                        post.Org_ID, "Approval",
                        $"✅ Your needs post \"{post.Title}\" has been approved as {chosenUrgency} urgency and is now visible to donors.",
                        post.NeedsPost_ID);
                    await LoadGatekeeperQueuesAsync();
                    await LoadMetricsAsync();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to approve needs post: {ex.Message}", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            });

            RejectNeedsPostCommand = new AsyncRelayCommand(async param =>
            {
                if (param is not NeedsPostModel post) return;

                // Prompt admin for a rejection reason
                string reason = Microsoft.VisualBasic.Interaction.InputBox(
                    $"Enter a rejection reason for \"{post.Title}\":\n(This will be shown to the beneficiary so they know what to fix.)",
                    "Rejection Reason",
                    "Please revise your post and resubmit.");

                // Cancelled — don't reject
                if (reason == null) return;
                if (string.IsNullOrWhiteSpace(reason))
                    reason = "Your post was rejected. Please edit and resubmit.";

                var r = MessageBox.Show(
                    $"Reject \"{post.Title}\"?\n\nReason: {reason}\n\nThe beneficiary will see this reason and can edit & resubmit.",
                    "Confirm Rejection", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (r != MessageBoxResult.Yes) return;

                try
                {
                    await KapwaDataService.RejectNeedsPost(post.NeedsPost_ID, reason);
                    await KapwaDataService.CreateNotification(
                        post.Org_ID, "Approval",
                        $"❌ Your needs post \"{post.Title}\" was not approved.\n\nReason: {reason}\n\nPlease edit your post and resubmit for re-review.",
                        post.NeedsPost_ID);
                    await LoadGatekeeperQueuesAsync();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to reject needs post: {ex.Message}", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            });

            // ── Reports ───────────────────────────────────────────────────────
            ProcessReportCommand = new AsyncRelayCommand(async param =>
            {
                if (param is not UserReportModel report) return;
                var result = MessageBox.Show(
                    $"Apply a STRIKE to {report.Reported_Name} for report \"{report.Report_Type}\"?\n\n" +
                    "3 strikes = automatic blacklist.",
                    "Apply Strike", MessageBoxButton.YesNoCancel, MessageBoxImage.Warning);
                if (result == MessageBoxResult.Cancel) return;

                string action = result == MessageBoxResult.Yes ? "Strike" : "None";
                try
                {
                    await KapwaDataService.ProcessUserReport(
                        report.Report_ID, report.Reported_ID,
                        "Reviewed", "Reviewed by Admin.", action);
                    if (action == "Strike")
                        await KapwaDataService.CreateNotification(
                            report.Reported_ID, "AccountAlert",
                            "⚠️ A strike has been applied to your account for a policy violation.",
                            report.Report_ID);
                    await LoadGatekeeperQueuesAsync();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to process report: {ex.Message}", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            });

            AdminBanUserCommand = new AsyncRelayCommand(async param =>
            {
                if (param is not UserReportModel report) return;
                var r = MessageBox.Show(
                    $"Permanently BAN user {report.Reported_ID} ({report.Reported_Name})?\n\n" +
                    "This will blacklist and deactivate their account immediately.",
                    "Confirm Ban", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (r != MessageBoxResult.Yes) return;
                try
                {
                    await KapwaDataService.AdminBanUser(report.Reported_ID);
                    await KapwaDataService.CreateNotification(
                        report.Reported_ID, "AccountAlert",
                        "⚠️ Your account has been permanently banned due to repeated violations.",
                        report.Report_ID);
                    await LoadGatekeeperQueuesAsync();
                    await LoadMetricsAsync();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to ban user: {ex.Message}", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            });

            // ── General ───────────────────────────────────────────────────────
            RefreshCommand = new AsyncRelayCommand(async _ =>
            {
                LoadError = string.Empty;
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

            // ── Safe deferred load — waits for window to fully render first ───
            Application.Current.Dispatcher.BeginInvoke(
                System.Windows.Threading.DispatcherPriority.Loaded,
                new Action(async () =>
                {
                    await LoadMetricsAsync();
                    await LoadGatekeeperQueuesAsync();
                }));
        }

        // ── Safe metrics load ─────────────────────────────────────────────────
        private async Task LoadMetricsAsync()
        {
            try
            {
                var m = await KapwaDataService.GetAdminImpactMetrics();
                if (Application.Current == null) return;
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
            catch (Exception ex)
            {
                SafeDispatch(() => LoadError = $"Metrics load failed: {ex.Message}");
            }
        }

        private async Task LoadGatekeeperQueuesAsync()
        {
            SafeDispatch(() =>
                IsLoadingItems = IsLoadingBenes = IsLoadingReports = IsLoadingNeedsPosts = true);
            try
            {
                var items = await KapwaDataService.GetPendingItems() ?? new();
                var benes = await KapwaDataService.GetPendingBeneficiaries() ?? new();
                var donors = await KapwaDataService.GetPendingDonors() ?? new();
                var needsPosts = await KapwaDataService.GetPendingNeedsPosts() ?? new();
                var reports = await KapwaDataService.GetOpenReports() ?? new();

                if (Application.Current == null) return;
                Application.Current.Dispatcher.Invoke(() =>
                {
                    PendingItemsList.Clear();
                    foreach (var i in items) PendingItemsList.Add(i);

                    PendingBenesList.Clear();
                    foreach (var b in benes) PendingBenesList.Add(b);

                    PendingDonorsList.Clear();
                    foreach (var d in donors) PendingDonorsList.Add(d);

                    PendingNeedsPostsList.Clear();
                    foreach (var n in needsPosts) PendingNeedsPostsList.Add(n);

                    OpenReportsList.Clear();
                    foreach (var r in reports) OpenReportsList.Add(r);

                    PendingItems = items.Count;
                    PendingDonors = donors.Count;
                    PendingNeedsPosts = needsPosts.Count;
                });
            }
            catch (Exception ex)
            {
                SafeDispatch(() => LoadError = $"Queue load failed: {ex.Message}");
            }
            finally
            {
                SafeDispatch(() =>
                    IsLoadingItems = IsLoadingBenes = IsLoadingReports = IsLoadingNeedsPosts = false);
            }
        }

        // ── UI-thread helper ──────────────────────────────────────────────────
        private static void SafeDispatch(Action action)
        {
            try
            {
                if (Application.Current == null) return;
                if (Application.Current.Dispatcher.CheckAccess())
                    action();
                else
                    Application.Current.Dispatcher.Invoke(action);
            }
            catch { /* swallow dispatch errors during shutdown */ }
        }
    }
}