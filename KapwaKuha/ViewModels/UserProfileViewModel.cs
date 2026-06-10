// FILE: ViewModels/UserProfileViewModel.cs
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using KapwaKuha.Commands;
using KapwaKuha.Models;
using KapwaKuha.Services;

namespace KapwaKuha.ViewModels
{
    public class UserProfileViewModel : ObservableObject
    {
        // ── Identity of the VIEWER (reporter) ────────────────────────────────
        private readonly string _viewerId;
        private readonly string _viewerRole;

        // ── Identity of the TARGET (person being viewed) ─────────────────────
        public string TargetId { get; }
        public string TargetRole { get; private set; } = string.Empty;

        // ── Display properties ────────────────────────────────────────────────
        private string _displayName = "Loading…";
        private string _profilePicture = string.Empty;
        private string _accountCreated = string.Empty;
        private string _subInfo = string.Empty;
        private double _averageRating = 0.0;
        private int _totalDonations = 0;
        private string _orgName = string.Empty;
        private string _orgAddress = string.Empty;
        private string _orgContact = string.Empty;
        private bool _isLoading = true;
        private bool _isDonorTarget;
        private bool _isInstBeneTarget;
        private bool _isIndepBeneTarget;

        public string DisplayName { get => _displayName; set { _displayName = value; OnPropertyChanged(); } }
        public string ProfilePicture { get => _profilePicture; set { _profilePicture = value; OnPropertyChanged(); OnPropertyChanged(nameof(HasPicture)); } }
        public string AccountCreated { get => _accountCreated; set { _accountCreated = value; OnPropertyChanged(); } }
        public string SubInfo { get => _subInfo; set { _subInfo = value; OnPropertyChanged(); } }
        public double AverageRating { get => _averageRating; set { _averageRating = value; OnPropertyChanged(); OnPropertyChanged(nameof(StarDisplay)); OnPropertyChanged(nameof(StarFill1)); OnPropertyChanged(nameof(StarFill2)); OnPropertyChanged(nameof(StarFill3)); OnPropertyChanged(nameof(StarFill4)); OnPropertyChanged(nameof(StarFill5)); } }
        public int TotalDonations { get => _totalDonations; set { _totalDonations = value; OnPropertyChanged(); } }
        public string OrgName { get => _orgName; set { _orgName = value; OnPropertyChanged(); } }
        public string OrgAddress { get => _orgAddress; set { _orgAddress = value; OnPropertyChanged(); } }
        public string OrgContact { get => _orgContact; set { _orgContact = value; OnPropertyChanged(); } }
        public bool IsLoading { get => _isLoading; set { _isLoading = value; OnPropertyChanged(); } }
        public bool IsDonorTarget { get => _isDonorTarget; set { _isDonorTarget = value; OnPropertyChanged(); } }
        public bool IsInstBeneTarget { get => _isInstBeneTarget; set { _isInstBeneTarget = value; OnPropertyChanged(); } }
        public bool IsIndepBeneTarget { get => _isIndepBeneTarget; set { _isIndepBeneTarget = value; OnPropertyChanged(); } }

        public bool HasPicture => !string.IsNullOrEmpty(ProfilePicture);

        // Star display helpers
        public string StarDisplay => $"{AverageRating:F1} / 5.0";
        public string StarFill1 => AverageRating >= 1 ? "#FFD700" : "#E0E0E0";
        public string StarFill2 => AverageRating >= 2 ? "#FFD700" : "#E0E0E0";
        public string StarFill3 => AverageRating >= 3 ? "#FFD700" : "#E0E0E0";
        public string StarFill4 => AverageRating >= 4 ? "#FFD700" : "#E0E0E0";
        public string StarFill5 => AverageRating >= 5 ? "#FFD700" : "#E0E0E0";

        // Donor's available items list
        public ObservableCollection<ItemModel> AvailableItems { get; } = new();

        public ObservableCollection<TransactionRow> ReceivedItems { get; } = new();

        // ── Report form state ─────────────────────────────────────────────────
        private string _reportType = "FakeItem";
        private string _reportDescription = string.Empty;
        private bool _reportPanelVisible;
        private string _reportError = string.Empty;
        private bool _reportErrorVisible;


        private string _email = string.Empty;
        public string Email
        {
            get => _email;
            set { _email = value; OnPropertyChanged(); OnPropertyChanged(nameof(HasEmail)); }
        }
        public bool HasEmail => !string.IsNullOrEmpty(Email);
        public string ReportType { get => _reportType; set { _reportType = value; OnPropertyChanged(); } }
        public string ReportDescription { get => _reportDescription; set { _reportDescription = value; OnPropertyChanged(); } }
        public bool ReportPanelVisible { get => _reportPanelVisible; set { _reportPanelVisible = value; OnPropertyChanged(); } }
        public string ReportError { get => _reportError; set { _reportError = value; OnPropertyChanged(); } }
        public bool ReportErrorVisible { get => _reportErrorVisible; set { _reportErrorVisible = value; OnPropertyChanged(); } }

        // Whether the viewer is Admin (enables Ban button)
        public bool ViewerIsAdmin => _viewerRole == "Admin";

        // ── Commands ──────────────────────────────────────────────────────────
        public ICommand CloseCommand { get; }
        public ICommand ToggleReportCommand { get; }
        public ICommand SubmitReportCommand { get; }
        public ICommand BanUserCommand { get; }
        public System.Action? OnCloseRequested { get; set; }

        public UserProfileViewModel(string targetId, string viewerId, string viewerRole)
        {
            TargetId = targetId;
            _viewerId = viewerId;
            _viewerRole = viewerRole;

            CloseCommand = new RelayCommand(_ => OnCloseRequested?.Invoke());

            ToggleReportCommand = new RelayCommand(_ =>
                ReportPanelVisible = !ReportPanelVisible);

            SubmitReportCommand = new AsyncRelayCommand(async _ =>
            {
                ReportErrorVisible = false;
                if (string.IsNullOrWhiteSpace(ReportDescription))
                {
                    ReportError = "Please describe the issue.";
                    ReportErrorVisible = true;
                    return;
                }
                try
                {
                    string reportId = await KapwaDataService.GetNextReportId();
                    var report = new UserReportModel
                    {
                        Report_ID = reportId,
                        Reporter_ID = _viewerId,
                        Reported_ID = TargetId,
                        Report_Type = ReportType,
                        Description = ReportDescription,
                        Status = "Open"
                    };
                    await KapwaDataService.FileUserReport(report);
                    MessageBox.Show(
                        $"✅ Report submitted (ID: {reportId}).\nOur admin team will review it shortly.",
                        "Report Filed", MessageBoxButton.OK, MessageBoxImage.Information);
                    ReportPanelVisible = false;
                    ReportDescription = string.Empty;
                    ReportErrorVisible = false;
                }
                catch (Exception ex)
                {
                    ReportError = "Failed to submit: " + ex.Message;
                    ReportErrorVisible = true;
                }
            });

            BanUserCommand = new AsyncRelayCommand(async _ =>
            {
                if (!ViewerIsAdmin) return;
                var r = MessageBox.Show(
                    $"Ban user {TargetId}?\nThis will blacklist them and deactivate their account immediately.",
                    "Confirm Ban", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (r != MessageBoxResult.Yes) return;
                try
                {
                    await KapwaDataService.AdminBanUser(TargetId);
                    await KapwaDataService.CreateNotification(
                        TargetId, "AccountAlert",
                        "⚠️ Your account has been banned due to policy violations.",
                        TargetId);
                    MessageBox.Show("User banned successfully.", "Done",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    OnCloseRequested?.Invoke();
                }
                catch { }
            });

            _ = LoadProfileAsync();
        }

        private async System.Threading.Tasks.Task LoadProfileAsync()
        {
            IsLoading = true;
            try
            {
                // Resolve role from DB if not known
                TargetRole = await KapwaDataService.GetUserRoleById(TargetId);

                if (TargetRole == "Donor")
                {
                    IsDonorTarget = true;
                    var donor = await KapwaDataService.GetDonorById(TargetId);
                    if (donor != null)
                    {
                        DisplayName = donor.Donor_FullName;
                        ProfilePicture = donor.ProfilePicturePath ?? string.Empty;
                        SubInfo = $"@{donor.Donor_Username}  ·  {donor.Donor_ContactNumber}";
                        AccountCreated = "Active Donor";
                    }
                    AverageRating = await KapwaDataService.GetDonorAverageRating(TargetId);
                    TotalDonations = await KapwaDataService.GetDonorTotalDonations(TargetId);

                    var items = await KapwaDataService.GetAvailableItemsByDonor(TargetId);
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        AvailableItems.Clear();
                        foreach (var i in items) AvailableItems.Add(i);
                    });

                    // In Donor branch, after loading donor:
                    Email = donor.Email ?? "";
                }
                else if (TargetRole == "InstitutionalBeneficiary")
                {
                    IsInstBeneTarget = true;
                    var bene = await KapwaDataService.GetBeneficiaryById(TargetId);
                    if (bene != null)
                    {
                        DisplayName = bene.Beneficiary_FullName;
                        ProfilePicture = bene.ProfilePicturePath ?? string.Empty;
                        SubInfo = $"@{bene.Beneficiary_Username}  ·  {bene.Beneficiary_Sex}";
                        AccountCreated = "Institutional Beneficiary";
                        OrgName = bene.Organization_Name ?? string.Empty;
                        OrgAddress = bene.Organization_Address ?? string.Empty;
                        OrgContact = bene.Organization_Contact ?? string.Empty;
                    }

                    var received = await KapwaDataService.GetBeneficiaryTransactionHistory(TargetId);
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        ReceivedItems.Clear();
                        foreach (var t in received) ReceivedItems.Add(t);
                    });
                    Email = bene.Email ?? "";
                }
                else if (TargetRole == "IndependentBeneficiary")
                {
                    IsIndepBeneTarget = true;
                    var indep = await KapwaDataService.GetIndependentBeneficiaryById(TargetId);
                    if (indep != null)
                    {
                        DisplayName = indep.FullName;
                        ProfilePicture = indep.ProfilePicturePath ?? string.Empty;
                        SubInfo = $"@{indep.Username}  ·  {indep.Sex}  ·  {indep.Address}";
                        AccountCreated = "Independent Beneficiary";
                    }
                    var received = await KapwaDataService.GetBeneficiaryTransactionHistory(TargetId);
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        ReceivedItems.Clear();
                        foreach (var t in received) ReceivedItems.Add(t);
                    });
                    Email = indep.Email ?? "";
                }


            }
            catch { }
            finally { IsLoading = false; }
        }
    }
}