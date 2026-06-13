// FILE: ViewModels/UserProfileViewModel.cs
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using KapwaKuha.Commands;
using KapwaKuha.Models;
using KapwaKuha.Services;
using System.Linq; // Added for .Contains()

namespace KapwaKuha.ViewModels
{
    public class UserProfileViewModel : ObservableObject
    {
        private readonly string _viewerId;
        private readonly string _viewerRole;

        public string TargetId { get; }
        public string TargetRole { get; private set; } = string.Empty;

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
        private string _email = string.Empty;

        private string _indepAddress = string.Empty;
        public string IndepAddress
        {
            get => _indepAddress;
            set { _indepAddress = value; OnPropertyChanged(); }
        }

        public string DisplayName { get => _displayName; set { _displayName = value; OnPropertyChanged(); } }
        public string ProfilePicture { get => _profilePicture; set { _profilePicture = value; OnPropertyChanged(); OnPropertyChanged(nameof(HasPicture)); } }
        public string AccountCreated { get => _accountCreated; set { _accountCreated = value; OnPropertyChanged(); } }
        public string SubInfo { get => _subInfo; set { _subInfo = value; OnPropertyChanged(); } }

        public double AverageRating
        {
            get => _averageRating;
            set
            {
                _averageRating = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(StarDisplay));
                // Update bindings for the new proportional offsets
                OnPropertyChanged(nameof(Star1Offset));
                OnPropertyChanged(nameof(Star2Offset));
                OnPropertyChanged(nameof(Star3Offset));
                OnPropertyChanged(nameof(Star4Offset));
                OnPropertyChanged(nameof(Star5Offset));
            }
        }

        public int TotalDonations { get => _totalDonations; set { _totalDonations = value; OnPropertyChanged(); } }
        public string OrgName { get => _orgName; set { _orgName = value; OnPropertyChanged(); } }
        public string OrgAddress { get => _orgAddress; set { _orgAddress = value; OnPropertyChanged(); } }
        public string OrgContact { get => _orgContact; set { _orgContact = value; OnPropertyChanged(); } }
        public bool IsLoading { get => _isLoading; set { _isLoading = value; OnPropertyChanged(); } }
        public bool IsDonorTarget { get => _isDonorTarget; set { _isDonorTarget = value; OnPropertyChanged(); } }
        public bool IsInstBeneTarget { get => _isInstBeneTarget; set { _isInstBeneTarget = value; OnPropertyChanged(); } }
        public bool IsIndepBeneTarget { get => _isIndepBeneTarget; set { _isIndepBeneTarget = value; OnPropertyChanged(); } }

        public string Email
        {
            get => _email;
            set { _email = value; OnPropertyChanged(); OnPropertyChanged(nameof(HasEmail)); }
        }
        public bool HasEmail => !string.IsNullOrEmpty(_email);
        public bool HasPicture => !string.IsNullOrEmpty(ProfilePicture);

        // ── Star helpers (Realistic Proportional Fill) ─────────────────────────
        public string StarDisplay => $"{AverageRating:F1} / 5.0";

        // Calculates exact fill percentage (0.0 to 1.0)
        private double GetStarFillPercentage(int slot)
        {
            double val = AverageRating - (slot - 1);
            if (val >= 1.0) return 1.0;
            if (val <= 0.0) return 0.0;
            return val;
        }

        // Bind these to your LinearGradientBrush Offsets in XAML
        public double Star1Offset => GetStarFillPercentage(1);
        public double Star2Offset => GetStarFillPercentage(2);
        public double Star3Offset => GetStarFillPercentage(3);
        public double Star4Offset => GetStarFillPercentage(4);
        public double Star5Offset => GetStarFillPercentage(5);

        public ObservableCollection<ItemModel> AvailableItems { get; } = new();
        public ObservableCollection<TransactionRow> ReceivedItems { get; } = new();

        // ── Report form state ─────────────────────────────────────────────
        private string _reportType = "FakeItem";
        private string _reportDescription = string.Empty;
        private bool _reportPanelVisible;
        private string _reportError = string.Empty;
        private bool _reportErrorVisible;

        public string ReportType { get => _reportType; set { _reportType = value; OnPropertyChanged(); } }
        public string ReportDescription { get => _reportDescription; set { _reportDescription = value; OnPropertyChanged(); } }
        public bool ReportPanelVisible { get => _reportPanelVisible; set { _reportPanelVisible = value; OnPropertyChanged(); } }
        public string ReportError { get => _reportError; set { _reportError = value; OnPropertyChanged(); } }
        public bool ReportErrorVisible { get => _reportErrorVisible; set { _reportErrorVisible = value; OnPropertyChanged(); } }

        public bool ViewerIsAdmin => _viewerRole == "Admin";

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
            ToggleReportCommand = new RelayCommand(_ => ReportPanelVisible = !ReportPanelVisible);

            SubmitReportCommand = new AsyncRelayCommand(async _ =>
            {
                // Validate using the UI-bound properties directly — no InputBox needed
                if (string.IsNullOrWhiteSpace(ReportDescription))
                {
                    ReportError = "Please describe the issue before submitting.";
                    ReportErrorVisible = true;
                    return;
                }
                ReportErrorVisible = false;
                ReportError = string.Empty;

                try
                {
                    string reportId = await KapwaDataService.GetNextReportId();
                    await KapwaDataService.FileUserReport(
                        reportId, _viewerId, TargetId,
                        ReportType, ReportDescription, string.Empty);

                    MessageBox.Show("✅ Report submitted. Our team will review it shortly.",
                        "Reported", MessageBoxButton.OK, MessageBoxImage.Information);

                    // Reset and close the panel
                    ReportDescription = string.Empty;
                    ReportType = "FakeItem";
                    ReportPanelVisible = false;
                }
                catch (System.Exception ex)
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
                        Email = donor.Email ?? "";
                    }
                    AverageRating = await KapwaDataService.GetDonorAverageRating(TargetId);
                    TotalDonations = await KapwaDataService.GetDonorTotalDonations(TargetId);

                    var items = await KapwaDataService.GetAvailableItemsByDonor(TargetId);
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        AvailableItems.Clear();
                        foreach (var i in items) AvailableItems.Add(i);
                    });
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
                        Email = bene.Email ?? "";

                    }
                    var received = await KapwaDataService.GetBeneficiaryTransactionHistory(TargetId);
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        ReceivedItems.Clear();
                        foreach (var t in received) ReceivedItems.Add(t);
                    });
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
                        Email = indep.Email ?? "";
                        IndepAddress = indep.Address ?? string.Empty;
                    }
                    var received = await KapwaDataService.GetBeneficiaryTransactionHistory(TargetId);
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        ReceivedItems.Clear();
                        foreach (var t in received) ReceivedItems.Add(t);
                    });
                }
            }
            catch { }
            finally { IsLoading = false; }
        }
    }
}