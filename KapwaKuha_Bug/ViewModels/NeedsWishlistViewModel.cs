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
        private string _orgId = string.Empty;   // ← real Organization_ID loaded on init

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

        // True = editing an existing post; False = creating new
        public bool IsEditing => _selectedPost != null;
        public bool IsCreating => _selectedPost == null;
        public bool HasImage => !string.IsNullOrEmpty(_imagePath);

        public bool IsLow { get => _urgency == "Low"; set { if (value) Urgency = "Low"; } }
        public bool IsMedium { get => _urgency == "Medium"; set { if (value) Urgency = "Medium"; } }
        public bool IsHigh { get => _urgency == "High"; set { if (value) Urgency = "High"; } }

        public bool IsBusy { get => _isBusy; set { _isBusy = value; OnPropertyChanged(); } }
        public string ErrorMessage { get => _errorMessage; set { _errorMessage = value; OnPropertyChanged(); } }
        public bool ErrorVisible { get => _errorVisible; set { _errorVisible = value; OnPropertyChanged(); } }

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
                // Pre-fill form when a post is selected for edit
                if (value != null)
                {
                    Title = value.Title;
                    Description = value.Description;
                    Urgency = value.Urgency;
                    ImagePath = value.ImagePath ?? string.Empty;
                }
            }
        }


        public ICommand BackCommand { get; }
        public ICommand PostNeedCommand { get; }
        public ICommand BrowseImageCommand { get; }
        public ICommand SetLowCommand { get; }
        public ICommand SetMediumCommand { get; }
        public ICommand SetHighCommand { get; }
        public ICommand SelectPostCommand { get; }   // sets SelectedPost from the list
        public ICommand ClearSelectionCommand { get; }  // clears edit, back to new post form
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

                // Guard: org must be loaded
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
                        Org_ID = _orgId,        // ← use real org ID, not beneficiary ID
                        Title = Title.Trim(),
                        Description = Description.Trim(),
                        Urgency = Urgency,
                        ImagePath = ImagePath,
                        Status = "Open"
                    };
                    await KapwaDataService.PostNeedsRequest(post);
                    MyPosts.Insert(0, post);
                    MessageBox.Show($"✅ Need posted! ID: {postId}",
                        "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    Title = Description = ImagePath = string.Empty;
                    Urgency = "Medium";
                }
                catch { }
                finally { IsBusy = false; }
            }); SelectPostCommand = new RelayCommand(post =>
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

                try
                {
                    IsBusy = true;
                    SelectedPost.Title = Title.Trim();
                    SelectedPost.Description = Description.Trim();
                    SelectedPost.Urgency = Urgency;
                    SelectedPost.ImagePath = ImagePath;

                    await KapwaDataService.UpdateNeedsPost(SelectedPost);

                    // Force UI refresh of the list item
                    var idx = MyPosts.IndexOf(SelectedPost);
                    if (idx >= 0)
                    {
                        MyPosts.RemoveAt(idx);
                        MyPosts.Insert(idx, SelectedPost);
                    }

                    MessageBox.Show("✅ Need post updated!",
                        "Updated", MessageBoxButton.OK, MessageBoxImage.Information);
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

            // Load the beneficiary's own posts on startup
            _ = LoadMyPosts();



            // Load the beneficiary's real Organization_ID on startup
            _ = LoadOrgId();
        }

        private async System.Threading.Tasks.Task LoadOrgId()
        {
            try
            {
                var bene = await KapwaDataService.GetBeneficiaryById(_beneficiaryId);
                if (bene != null)
                    _orgId = bene.Organization_ID;
            }
            catch { }
        }
        private async System.Threading.Tasks.Task LoadMyPosts()
        {
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