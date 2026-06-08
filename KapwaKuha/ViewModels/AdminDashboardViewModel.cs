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

        public ObservableCollection<NeedsPostModel> PendingNeedsPostsList { get; } = new();
        private int _pendingNeedsPosts;
        public int PendingNeedsPosts
        {
            get => _pendingNeedsPosts;
            set { _pendingNeedsPosts = value; OnPropertyChanged(); OnPropertyChanged(nameof(HasPendingNeedsPosts)); }
        }
        public bool HasPendingNeedsPosts => PendingNeedsPosts > 0;

        private bool _isLoadingNeedsPosts;
        public bool IsLoadingNeedsPosts
        {
            get => _isLoadingNeedsPosts;
            set { _isLoadingNeedsPosts = value; OnPropertyChanged(); }
        }

        public ICommand ApproveNeedsPostCommand { get; }
        public ICommand RejectNeedsPostCommand { get; }

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
        public ICommand ProcessReportCommand { get; }
        public ICommand AdminBanUserCommand { get; }

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
                catch { }
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
                catch { }
            });

            ApproveNeedsPostCommand = new AsyncRelayCommand(async param =>
            {
                if (param is not NeedsPostModel post) return;

                // Admin picks urgency at approval time
                string[] urgencyOptions = { "Low", "Medium", "High" };
                // Default to what bene submitted — admin can confirm or change
                string chosenUrgency = post.Urgency;

                var result = MessageBox.Show(
                    $"Approve needs post \"{post.Title}\" from {post.Org_Name}?\n\n" +
                    $"Submitted urgency: {post.Urgency}\n\n" +
                    "Click YES to approve at this urgency level.\n" +
                    "Click NO to approve but override urgency (you will be prompted).",
                    "Approve Needs Post", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);

                if (result == MessageBoxResult.Cancel) return;

                if (result == MessageBoxResult.No)
                {
                    // Let admin pick urgency override
                 
                    // Simple approach: ask via sequential MessageBoxes
                    var highResult = MessageBox.Show(
                        "Set urgency to HIGH (🔴 Urgent)?\n\nYes = High  |  No = proceed to next option",
                        "Set Urgency", MessageBoxButton.YesNo, MessageBoxImage.Question);
                    if (highResult == MessageBoxResult.Yes) chosenUrgency = "High";
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

                    // Notify the org beneficiaries
                    await KapwaDataService.CreateNotification(
                        post.Org_ID, "Approval",
                        $"✅ Your needs post \"{post.Title}\" has been approved as {chosenUrgency} urgency and is now visible to donors.",
                        post.NeedsPost_ID);

                    await LoadGatekeeperQueuesAsync();
                    await LoadMetricsAsync();
                }
                catch { }
            });

            RejectNeedsPostCommand = new AsyncRelayCommand(async param =>
            {
                if (param is not NeedsPostModel post) return;
                var r = MessageBox.Show(
                    $"Reject needs post \"{post.Title}\"?\nIt will be hidden from donors.",
                    "Confirm Rejection", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (r != MessageBoxResult.Yes) return;
                try
                {
                    await KapwaDataService.RejectNeedsPost(post.NeedsPost_ID);
                    await KapwaDataService.CreateNotification(
                        post.Org_ID, "Approval",
                        $"❌ Your needs post \"{post.Title}\" was not approved. Please revise and resubmit.",
                        post.NeedsPost_ID);
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

                IsLoadingNeedsPosts = true;
                var needsPosts = await KapwaDataService.GetPendingNeedsPosts();
                Application.Current.Dispatcher.Invoke(() =>
                {
                    PendingNeedsPostsList.Clear();
                    foreach (var np in needsPosts) PendingNeedsPostsList.Add(np);
                    PendingNeedsPosts = needsPosts.Count;
                });
                IsLoadingNeedsPosts = false;
            }
            catch { }
            finally
            {
                IsLoadingItems = IsLoadingBenes = IsLoadingReports = false;
            }
        }
    }
}