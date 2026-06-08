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

        private string _title = string.Empty;
        private string _description = string.Empty;
        private string _urgency = "Medium";
        private string _imagePath = string.Empty;
        private bool _isBusy;
        private string _errorMessage = string.Empty;
        private bool _errorVisible;

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
                    // NOTE: urgency is NOT pre-filled for edit —
                    // admin controls final urgency; bene may only edit title/desc/image
                    Urgency = value.Urgency;
                    ImagePath = value.ImagePath ?? string.Empty;
                }
            }
        }

        // Only allow edit if post is NOT approved (live posts can't be edited without re-review)
        public bool CanEditSelected => _selectedPost != null && _selectedPost.Admin_Approval_Status != "Approved";

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

            // ── NEW POST ──────────────────────────────────────────────────────
            PostNeedCommand = new AsyncRelayCommand(async _ =>
            {
                ErrorVisible = false;
                if (string.IsNullOrWhiteSpace(Title))
                { ErrorMessage = "Title is required."; ErrorVisible = true; return; }
                if (string.IsNullOrWhiteSpace(Description))
                { ErrorMessage = "Description is required."; ErrorVisible = true; return; }
                if (string.IsNullOrEmpty(_orgId))
                {
                    ErrorMessage = "Your organization could not be found. Please try again.";
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
                    MyPosts.Insert(0, post);

                    // Correct message — goes to admin, not live yet
                    MessageBox.Show(
                        $"📋 Needs post submitted for admin review!\n\nID: {postId}\n\n" +
                        "An admin will review and confirm the urgency level before it goes live.",
                        "Pending Approval", MessageBoxButton.OK, MessageBoxImage.Information);

                    Title = Description = ImagePath = string.Empty;
                    Urgency = "Medium";
                }
                catch { }
                finally { IsBusy = false; }
            });

            // ── SELECT POST FROM LIST ─────────────────────────────────────────
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

            // ── UPDATE EXISTING POST ──────────────────────────────────────────
            UpdateNeedCommand = new AsyncRelayCommand(async _ =>
            {
                if (SelectedPost == null) return;
                ErrorVisible = false;

                // Gate: cannot edit a live (Approved) post
                if (SelectedPost.Admin_Approval_Status == "Approved")
                {
                    MessageBox.Show(
                        "This post is currently live and cannot be edited.\n\n" +
                        "If you need to make changes, please contact an admin or delete and repost.",
                        "Cannot Edit Approved Post", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (string.IsNullOrWhiteSpace(Title))
                { ErrorMessage = "Title is required."; ErrorVisible = true; return; }
                if (string.IsNullOrWhiteSpace(Description))
                { ErrorMessage = "Description is required."; ErrorVisible = true; return; }

                try
                {
                    IsBusy = true;
                    SelectedPost.Title = Title.Trim();
                    SelectedPost.Description = Description.Trim();
                    SelectedPost.Urgency = Urgency;
                    SelectedPost.ImagePath = ImagePath;
                    // Reset approval to Pending — admin must re-review
                    SelectedPost.Admin_Approval_Status = "Pending";

                    await KapwaDataService.UpdateNeedsPost(SelectedPost);

                    // Refresh in list
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

            // ── DELETE ────────────────────────────────────────────────────────
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

            _ = LoadMyPostsAsync();
            _ = LoadOrgIdAsync();
        }

        private async System.Threading.Tasks.Task LoadOrgIdAsync()
        {
            try
            {
                var bene = await KapwaDataService.GetBeneficiaryById(_beneficiaryId);
                if (bene != null)
                {
                    _orgId = bene.Organization_ID;
                    // Reload posts now that we have the real orgId
                    await LoadMyPostsAsync();
                }
            }
            catch { }
        }

        private async System.Threading.Tasks.Task LoadMyPostsAsync()
        {
            if (string.IsNullOrEmpty(_orgId)) return;
            try
            {
                var posts = await KapwaDataService.GetNeedsPostsByOrg(_orgId);
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