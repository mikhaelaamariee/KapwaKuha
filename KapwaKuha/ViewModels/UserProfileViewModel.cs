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
                OnPropertyChanged(nameof(StarFill1));
                OnPropertyChanged(nameof(StarFill2));
                OnPropertyChanged(nameof(StarFill3));
                OnPropertyChanged(nameof(StarFill4));
                OnPropertyChanged(nameof(StarFill5));
                OnPropertyChanged(nameof(HalfStarVisible));
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

        // ── Star helpers (with half-star support) ─────────────────────────
        public string StarDisplay => $"{AverageRating:F1} / 5.0";

        // Returns full/half/empty for each star slot
        // 0=empty, 1=half, 2=full
        private int StarValue(int slot)
        {
            double val = AverageRating - (slot - 1);
            if (val >= 1.0) return 2;       // full
            if (val >= 0.3) return 1;       // half (≥0.3 shows half)
            return 0;                       // empty
        }

        public string StarFill1 => StarValue(1) == 2 ? "#FFD700" : "#E0E0E0";
        public string StarFill2 => StarValue(2) == 2 ? "#FFD700" : "#E0E0E0";
        public string StarFill3 => StarValue(3) == 2 ? "#FFD700" : "#E0E0E0";
        public string StarFill4 => StarValue(4) == 2 ? "#FFD700" : "#E0E0E0";
        public string StarFill5 => StarValue(5) == 2 ? "#FFD700" : "#E0E0E0";

        // Half overlay visibility per slot
        public bool HalfStar1 => StarValue(1) == 1;
        public bool HalfStar2 => StarValue(2) == 1;
        public bool HalfStar3 => StarValue(3) == 1;
        public bool HalfStar4 => StarValue(4) == 1;
        public bool HalfStar5 => StarValue(5) == 1;

        // Kept for XAML that only checks "any half visible"
        public bool HalfStarVisible => HalfStar1 || HalfStar2 || HalfStar3 || HalfStar4 || HalfStar5;

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
                string reportType = Microsoft.VisualBasic.Interaction.InputBox(
                    "Report type (FakeItem / Fraud / Spam / Inappropriate):",
                    "Report User", "Spam");
                if (string.IsNullOrWhiteSpace(reportType)) return;
                if (!new[] { "FakeItem", "Fraud", "Spam", "Inappropriate" }.Contains(reportType))
                { MessageBox.Show("Invalid report type."); return; }

                string description = Microsoft.VisualBasic.Interaction.InputBox(
                    "Describe the issue (required):", "Report Details", "");
                if (string.IsNullOrWhiteSpace(description)) return;

                string proofPath = string.Empty;
                var attachResult = MessageBox.Show(
                    "Attach a proof image?\n\nClick YES to attach.",
                    "Attach Proof", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (attachResult == MessageBoxResult.Yes)
                {
                    var dlg = new Microsoft.Win32.OpenFileDialog
                    {
                        Filter = "Images (*.png;*.jpg;*.jpeg;*.bmp)|*.png;*.jpg;*.jpeg;*.bmp",
                        Title = "Select Proof Image"
                    };
                    if (dlg.ShowDialog() == true) proofPath = dlg.FileName;
                }

                var confirm = MessageBox.Show(
                    $"Report this user for {reportType}?\n\nDescription: {description}",
                    "Confirm Report", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (confirm != MessageBoxResult.Yes) return;

                try
                {
                    string reportId = await KapwaDataService.GetNextReportId();
                    await KapwaDataService.FileUserReport(
                        reportId, _viewerId, TargetId,
                        reportType, description, proofPath);
                    MessageBox.Show("✅ Report submitted. Our team will review it shortly.",
                        "Reported", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Failed to submit report: " + ex.Message);
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
                        SubInfo = $"{(string.IsNullOrEmpty(donor.Email) ? "@" + donor.Donor_Username : donor.Email)}  ·  {donor.Donor_ContactNumber}";

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
                        SubInfo = $"{(string.IsNullOrEmpty(bene.Email) ? "@" + bene.Beneficiary_Username : bene.Email)}  ·  {bene.Beneficiary_Sex}";
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
                        SubInfo = $"{(string.IsNullOrEmpty(indep.Email) ? "@" + indep.Username : indep.Email)}  ·  {indep.Sex}  ·  {indep.Address}";
                        AccountCreated = "Independent Beneficiary";
                        Email = indep.Email ?? "";
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