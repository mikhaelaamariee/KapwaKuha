// FILE: ViewModels/BeneficiaryClaimHistoryViewModel.cs
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using KapwaKuha.Commands;
using KapwaKuha.Models;
using KapwaKuha.Services;

namespace KapwaKuha.ViewModels
{
    public class BeneficiaryClaimHistoryViewModel : ObservableObject
    {
        private readonly string _beneficiaryId;
        private List<ClaimModel> _allClaims = new();

        public ObservableCollection<ClaimModel> Claims { get; } = new();

        private string _searchText = string.Empty;
        public string SearchText
        {
            get => _searchText;
            set { _searchText = value; OnPropertyChanged(); ApplyFilter(); }
        }

        private string _filterStatus = "All";
        public string FilterStatus
        {
            get => _filterStatus;
            set { _filterStatus = value; OnPropertyChanged(); ApplyFilter(); }
        }

        private string _filterCategory = "All";
        public string FilterCategory
        {
            get => _filterCategory;
            set { _filterCategory = value; OnPropertyChanged(); ApplyFilter(); }
        }

        private bool _isBusy;
        public bool IsBusy { get => _isBusy; set { _isBusy = value; OnPropertyChanged(); } }

        private string _statusMessage = "Loading...";
        public string StatusMessage
        {
            get => _statusMessage;
            set { _statusMessage = value; OnPropertyChanged(); }
        }

        public bool HasNoClaims => Claims.Count == 0 && !IsBusy;

        public ICommand BackCommand { get; }
        public ICommand RefreshCommand { get; }

        public BeneficiaryClaimHistoryViewModel(string beneficiaryId)
        {
            _beneficiaryId = beneficiaryId;

            BackCommand = new RelayCommand(_ =>
                NavigationService.Navigate(new View.BeneficiaryDashboardWindow(_beneficiaryId)));

            RefreshCommand = new AsyncRelayCommand(async _ => await LoadAsync());

            _ = LoadAsync();
        }

        private async System.Threading.Tasks.Task LoadAsync()
        {
            IsBusy = true;
            OnPropertyChanged(nameof(HasNoClaims));
            try
            {
                var result = await KapwaDataService.GetClaimHistoryByBeneficiary(_beneficiaryId);
                Application.Current.Dispatcher.Invoke(() =>
                {
                    _allClaims = result;
                    ApplyFilter();
                });
            }
            catch { }
            finally
            {
                IsBusy = false;
                OnPropertyChanged(nameof(HasNoClaims));
            }
        }

        private void ApplyFilter()
        {
            Claims.Clear();
            var q = _searchText.Trim().ToLower();
            foreach (var c in _allClaims)
            {
                bool matchSearch = string.IsNullOrEmpty(q) ||
                                   c.Item_Name.ToLower().Contains(q) ||
                                   c.Claim_ID.ToLower().Contains(q) ||
                                   c.Category_Name.ToLower().Contains(q);

                bool matchStatus = _filterStatus == "All" ||
                                   c.Claim_Status == _filterStatus;

                bool matchCat = _filterCategory == "All" ||
                                c.Category_Name == _filterCategory;

                if (matchSearch && matchStatus && matchCat) Claims.Add(c);
            }
            StatusMessage = $"{Claims.Count} claim(s) found.";
            OnPropertyChanged(nameof(HasNoClaims));
        }
    }

    // Design-time ViewModel — gives Visual Studio designer a sample to render
    public class BeneficiaryClaimHistoryDesignViewModel
    {
        public string SearchText { get; } = string.Empty;
        public string FilterStatus { get; } = "All";
        public string FilterCategory { get; } = "All";
        public bool IsBusy { get; } = false;
        public bool HasNoClaims { get; } = false;
        public string StatusMessage { get; } = "2 claim(s) found.";

        public ObservableCollection<ClaimModel> Claims { get; } = new()
        {
            new ClaimModel
            {
                Claim_ID         = "CL001",
                Item_ID          = "ITEM001",
                Item_Name        = "School Supplies for Grade 1 Students",
                Category_Name    = "School Supplies",
                Beneficiary_Name = "Ana Reyes",
                Claim_Status     = "Released",
                Handoff_Type     = "Pickup",
                Claim_Date       = System.DateTime.Now.AddDays(-2)
            },
            new ClaimModel
            {
                Claim_ID         = "CL002",
                Item_ID          = "ITEM002",
                Item_Name        = "Children's Clothing Set",
                Category_Name    = "Clothing",
                Beneficiary_Name = "Ana Reyes",
                Claim_Status     = "Pending",
                Handoff_Type     = "Delivery",
                Claim_Date       = System.DateTime.Now.AddDays(-1)
            }
        };

        public System.Windows.Input.ICommand BackCommand { get; } = new RelayCommand(_ => { });
        public System.Windows.Input.ICommand RefreshCommand { get; } = new RelayCommand(_ => { });
    }
}