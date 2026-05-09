// FILE: ViewModels/HighPriorityNeedsViewModel.cs
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using KapwaKuha.Commands;
using KapwaKuha.Models;
using KapwaKuha.Services;

namespace KapwaKuha.ViewModels
{
    public class HighPriorityNeedsViewModel : ObservableObject
    {
        private readonly string _donorId;

        public ObservableCollection<NeedsPostModel> NeedsPosts { get; } = new();
        private bool _isBusy;
        public bool IsBusy { get => _isBusy; set { _isBusy = value; OnPropertyChanged(); } }

        // Add to the class:
        private System.Collections.Generic.List<NeedsPostModel> _allPosts = new();

        private string _filterUrgency = "All";
        public string FilterUrgency
        {
            get => _filterUrgency;
            set { _filterUrgency = value; OnPropertyChanged(); ApplyFilter(); }
        }

        private string _searchText = string.Empty;
        public string SearchText
        {
            get => _searchText;
            set { _searchText = value; OnPropertyChanged(); ApplyFilter(); }
        }


        private void ApplyFilter()
        {
            NeedsPosts.Clear();
            var q = _searchText.Trim().ToLower();
            foreach (var p in _allPosts)
            {
                bool matchUrgency = _filterUrgency == "All" || p.Urgency == _filterUrgency;
                bool matchSearch = string.IsNullOrEmpty(q) ||
                                    p.Title.ToLower().Contains(q) ||
                                    p.Description.ToLower().Contains(q) ||
                                    p.Org_Name.ToLower().Contains(q);
                if (matchUrgency && matchSearch) NeedsPosts.Add(p);
            }
        }

        public ICommand BackCommand { get; }
        public ICommand RefreshCommand { get; }
        public ICommand DonateToNeedCommand { get; }

        public HighPriorityNeedsViewModel(string donorId)
        {
            _donorId = donorId;

            BackCommand = new RelayCommand(_ =>
                NavigationService.Navigate(new View.DonorDashboardWindow(_donorId)));

            RefreshCommand = new AsyncRelayCommand(async _ => await LoadPostsAsync());

            DonateToNeedCommand = new RelayCommand(post =>
            {
                if (post is not NeedsPostModel selected) return;

                // Guard: must have a resolved beneficiary to target
                if (string.IsNullOrEmpty(selected.RequesterBeneficiaryId))
                {
                    MessageBox.Show(
                        $"No active beneficiary found in \"{selected.Org_Name}\".\n\n" +
                        "Please ensure the organization has at least one active member registered.",
                        "Cannot Fulfill",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Navigate to PostItem:
                //   - Pre-fill title from the need
                //   - Lock to DirectTarget mode
                //   - Pass the DYNAMIC Org_ID so the VM can filter beneficiaries
                //   - Also pass the specific RequesterBeneficiaryId so it pre-selects correctly
                NavigationService.Navigate(
                    new View.PostItemWindow(
                        _donorId,
                        prefillTitle: selected.Title,
                        lockedOrgId: selected.Org_ID,
                        lockDirect: true,
                        lockedBeneficiaryId: selected.RequesterBeneficiaryId));  // ← dynamic
            });

            _ = LoadPostsAsync();
        }

        // REPLACE LoadPostsAsync entirely:
        private async System.Threading.Tasks.Task LoadPostsAsync()
        {
            IsBusy = true;
            try
            {
                var posts = await KapwaDataService.GetOpenNeedsPosts();
                Application.Current.Dispatcher.Invoke(() =>
                {
                    _allPosts = posts;
                    ApplyFilter();   // ← uses filter instead of direct add
                });
            }
            catch { }
            finally { IsBusy = false; }
        }
    }
}