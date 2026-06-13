// FILE: ViewModels/NeedsWishlistViewModel.cs
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using KapwaKuha.Commands;
using KapwaKuha.Models;
using KapwaKuha.Services;

namespace KapwaKuha.ViewModels
{
    public class NeedsWishlistViewModel : ObservableObject
    {
        private readonly string _beneficiaryId;
        private string _orgId = string.Empty;
        private bool _isIndependent = false;

        private string _title = string.Empty;
        private string _description = string.Empty;
        private string _urgency = "Medium";
        private string _imagePath = string.Empty;
        private bool _isBusy;
        private string _errorMessage = string.Empty;
        private bool _errorVisible;

        private bool _isIndependentBene = false;
        public bool CanPostNeeds => !_isIndependentBene;

        private NeedsPostModel? _selectedPost;

        public string Title
        {
            get => _title;
            set { _title = value; OnPropertyChanged(); }
        }
        public string Description
        {
            get => _description;
            set { _description = value; OnPropertyChanged(); }
        }
        public string Urgency
        {
            get => _urgency;
            set
            {
                _urgency = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsLow));
                OnPropertyChanged(nameof(IsMedium));
                OnPropertyChanged(nameof(IsHigh));
            }
        }
        public string ImagePath
        {
            get => _imagePath;
            set { _imagePath = value; OnPropertyChanged(); OnPropertyChanged(nameof(HasImage)); }
        }

        public bool IsEditing => _selectedPost != null;
        public bool IsCreating => _selectedPost == null;
        public bool HasImage => !string.IsNullOrEmpty(_imagePath);

        public bool IsLow { get => _urgency == "Low"; set { if (value) Urgency = "Low"; } }
        public bool IsMedium { get => _urgency == "Medium"; set { if (value) Urgency = "Medium"; } }
        public bool IsHigh { get => _urgency == "High"; set { if (value) Urgency = "High"; } }

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

        public ObservableCollection<NeedsPostModel> MyPosts { get; } = new();

        public NeedsPostModel? SelectedPost
        {
            get => _selectedPost;
            set
            {
                _selectedPost = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsEditing));
                OnPropertyChanged(nameof(IsCreating));
                OnPropertyChanged(nameof(CanEditSelected));
                if (value != null)
                {
                    Title = value.Title;
                    Description = value.Description;
                    Urgency = value.Urgency;
                    ImagePath = value.ImagePath ?? string.Empty;
                }
            }
        }

        public bool CanEditSelected => _selectedPost == null || _selectedPost.Admin_Approval_Status != "Approved";

        public ICommand BackCommand { get; }
        public ICommand PostNeedCommand { get; }
        public ICommand BrowseImageCommand { get; }
        public ICommand SetLowCommand { get; }
        public ICommand SetMediumCommand { get; }
        public ICommand SetHighCommand { get; }
        public ICommand SelectPostCommand { get; }
        public ICommand ClearSelectionCommand { get; }
        public ICommand UpdateNeedCommand { get; }
        public ICommand DeleteNeedCommand { get; }

        public NeedsWishlistViewModel(string beneficiaryId)
        {
            _beneficiaryId = beneficiaryId;

            BackCommand = new RelayCommand(_ =>
                NavigationService.Navigate(new View.BeneficiaryDashboardWindow(_beneficiaryId)));

            SetLowCommand = new RelayCommand(_ => Urgency = "Low");
            SetMediumCommand = new RelayCommand(_ => Urgency = "Medium");
            SetHighCommand = new RelayCommand(_ => Urgency = "High");

            BrowseImageCommand = new RelayCommand(_ =>
            {
                var dlg = new Microsoft.Win32.OpenFileDialog
                {
                    Filter = "Image files (*.png;*.jpg;*.jpeg;*.bmp)|*.png;*.jpg;*.jpeg;*.bmp",
                    Title = "Select Need Photo"
                };
                if (dlg.ShowDialog() == true) ImagePath = dlg.FileName;
            });

            PostNeedCommand = new AsyncRelayCommand(async _ =>
            {
                ErrorVisible = false;
                if (string.IsNullOrWhiteSpace(Title))
                { ErrorMessage = "Title is required."; ErrorVisible = true; return; }
                if (string.IsNullOrWhiteSpace(Description))
                { ErrorMessage = "Description is required."; ErrorVisible = true; return; }

                // For institutional bene, orgId must be resolved.
                // For independent, we use the beneficiaryId itself as Org_ID
                // (or a special "INDEP" prefix — make sure DB allows this).
                // Here we use _orgId which for indep is set to beneficiaryId in LoadOrgIdAsync.
                if (string.IsNullOrEmpty(_orgId))
                {
                    ErrorMessage = "Could not resolve your account. Please try again.";
                    ErrorVisible = true;
                    return;
                }

                try
                {
                    IsBusy = true;
                    string postId = await KapwaDataService.GetNextNeedsPostId();
                    var post = new NeedsPostModel
                    {
                        NeedsPost_ID = postId,
                        Org_ID = _orgId,
                        Title = Title.Trim(),
                        Description = Description.Trim(),
                        Urgency = Urgency,
                        ImagePath = ImagePath,
                        Status = "Open",
                        Admin_Approval_Status = "Pending"
                    };
                    await KapwaDataService.PostNeedsRequest(post);

                    await LoadMyPostsAsync();

                    Title = Description = ImagePath = string.Empty;
                    Urgency = "Medium";

                    MessageBox.Show(
                        "✅ Your needs post has been submitted!\n\nIt will appear publicly once an admin approves it.",
                        "Submitted for Approval", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch { }
                finally { IsBusy = false; }
            });

            SelectPostCommand = new RelayCommand(post =>
            {
                if (post is NeedsPostModel p) SelectedPost = p;
            });

            ClearSelectionCommand = new RelayCommand(_ =>
            {
                SelectedPost = null;
                Title = string.Empty;
                Description = string.Empty;
                Urgency = "Medium";
                ImagePath = string.Empty;
                ErrorVisible = false;
            });

            UpdateNeedCommand = new AsyncRelayCommand(async _ =>
            {
                if (SelectedPost == null) return;
                ErrorVisible = false;

                if (string.IsNullOrWhiteSpace(Title))
                { ErrorMessage = "Title is required."; ErrorVisible = true; return; }
                if (string.IsNullOrWhiteSpace(Description))
                { ErrorMessage = "Description is required."; ErrorVisible = true; return; }

                if (SelectedPost.Admin_Approval_Status == "Approved")
                {
                    var confirm = MessageBox.Show(
                        $"Editing \"{SelectedPost.Title}\" will send it back for admin review.\n\nYour post will be hidden from donors until re-approved. Continue?",
                        "Resubmit for Approval", MessageBoxButton.YesNo, MessageBoxImage.Question);
                    if (confirm != MessageBoxResult.Yes) return;

                    try
                    {
                        IsBusy = true;
                        var pendingEdit = new NeedsPostModel
                        {
                            NeedsPost_ID = SelectedPost.NeedsPost_ID,
                            Title = Title.Trim(),
                            Description = Description.Trim(),
                            Urgency = Urgency,
                            ImagePath = ImagePath
                        };
                        await KapwaDataService.SubmitNeedsPostEditForReview(pendingEdit);
                        await LoadMyPostsAsync();
                        MessageBox.Show(
                            "✅ Your edits have been submitted for admin review.\n" +
                            "Your post will reappear with the new details once approved.",
                            "Submitted", MessageBoxButton.OK, MessageBoxImage.Information);
                        ClearSelectionCommand.Execute(null);
                    }
                    catch { }
                    finally { IsBusy = false; }
                    return;
                }

                try
                {
                    IsBusy = true;
                    SelectedPost.Title = Title.Trim();
                    SelectedPost.Description = Description.Trim();
                    SelectedPost.Urgency = Urgency;
                    SelectedPost.ImagePath = ImagePath;
                    SelectedPost.Admin_Approval_Status = "Pending";

                    await KapwaDataService.UpdateNeedsPost(SelectedPost);

                    var idx = MyPosts.IndexOf(SelectedPost);
                    if (idx >= 0) { MyPosts.RemoveAt(idx); MyPosts.Insert(idx, SelectedPost); }

                    MessageBox.Show(
                        "📋 Need post updated and sent back for admin review.\n\n" +
                        "It will be hidden from donors until an admin re-approves it.",
                        "Re-submitted for Approval", MessageBoxButton.OK, MessageBoxImage.Information);

                    ClearSelectionCommand.Execute(null);
                }
                catch { }
                finally { IsBusy = false; }
            });

            DeleteNeedCommand = new AsyncRelayCommand(async _ =>
            {
                if (SelectedPost == null) return;
                var r = MessageBox.Show(
                    $"Delete \"{SelectedPost.Title}\"?\n\nThis cannot be undone.",
                    "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (r != MessageBoxResult.Yes) return;
                try
                {
                    IsBusy = true;
                    await KapwaDataService.DeleteNeedsPost(SelectedPost.NeedsPost_ID);
                    MyPosts.Remove(SelectedPost);
                    ClearSelectionCommand.Execute(null);
                    MessageBox.Show("Post deleted.", "Deleted",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch { }
                finally { IsBusy = false; }
            });

            _ = LoadOrgIdAsync();
        }

        private async System.Threading.Tasks.Task LoadOrgIdAsync()
        {
            try
            {
                // Try institutional bene first
                var bene = await KapwaDataService.GetBeneficiaryById(_beneficiaryId);
                if (bene != null)
                {
                    _orgId = bene.Organization_ID;
                    _isIndependent = false;
                }
                else
                {
                    // Independent — resolve or auto-create their personal org via the SP;
                    // store the resolved ORG### back so repeated posts reuse the same row
                    _orgId = await KapwaDataService.GetOrCreateIndepBeneOrg(_beneficiaryId);
                    _isIndependent = true;
                }
                await LoadMyPostsAsync();
            }
            catch { }
        }

        private async System.Threading.Tasks.Task LoadMyPostsAsync()
        {
            if (string.IsNullOrEmpty(_orgId)) return;
            try
            {
                List<NeedsPostModel> posts;
                if (_isIndependent)
                    // For independent bene, org_id was stored as beneficiaryId
                    posts = await KapwaDataService.GetNeedsPostsByOrg(_orgId);
                else
                    posts = await KapwaDataService.GetNeedsPostsByOrg(_orgId);

                Application.Current.Dispatcher.Invoke(() =>
                {
                    MyPosts.Clear();
                    foreach (var p in posts) MyPosts.Add(p);
                });
            }
            catch { }
        }
    }
}