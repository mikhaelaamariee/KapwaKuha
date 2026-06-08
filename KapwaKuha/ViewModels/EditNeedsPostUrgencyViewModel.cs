using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using KapwaKuha.Commands;
using KapwaKuha.Models;
using KapwaKuha.Services;

namespace KapwaKuha.ViewModels
{
    public class EditNeedsPostUrgencyViewModel : ObservableObject
    {
        private readonly string _beneficiaryId;
        private readonly string _orgId;
        private string? _pendingSelectId;

        public ObservableCollection<NeedsPostModel> MyPosts { get; } = new();

        private NeedsPostModel? _selectedPost;
        public NeedsPostModel? SelectedPost
        {
            get => _selectedPost;
            set
            {
                _selectedPost = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(HasSelection));

                if (value != null)
                {
                    SelectedUrgency = value.Urgency;
                    EditTitle = value.Title;
                    EditDescription = value.Description ?? string.Empty;
                    EditImagePath = value.ImagePath ?? string.Empty;
                }
                else
                {
                    EditTitle = string.Empty;
                    EditDescription = string.Empty;
                    EditImagePath = string.Empty;
                }
            }
        }
        public bool HasSelection => _selectedPost != null;

        private string _selectedUrgency = "Medium";
        public string SelectedUrgency
        {
            get => _selectedUrgency;
            set { _selectedUrgency = value; OnPropertyChanged(); }
        }

        private bool _isBusy;
        public bool IsBusy { get => _isBusy; set { _isBusy = value; OnPropertyChanged(); } }

        private string _editTitle = string.Empty;
        public string EditTitle
        {
            get => _editTitle;
            set { _editTitle = value; OnPropertyChanged(); }
        }

        private string _editDescription = string.Empty;
        public string EditDescription
        {
            get => _editDescription;
            set { _editDescription = value; OnPropertyChanged(); }
        }

        private string _editImagePath = string.Empty;
        public string EditImagePath
        {
            get => _editImagePath;
            set { _editImagePath = value; OnPropertyChanged(); OnPropertyChanged(nameof(HasEditImage)); }
        }

        public bool HasEditImage =>
            !string.IsNullOrEmpty(_editImagePath) && System.IO.File.Exists(_editImagePath);

        public ICommand BrowseImageCommand { get; }
        public ICommand BackCommand { get; }
        public ICommand RefreshCommand { get; }
        public ICommand SaveUrgencyCommand { get; }
        public ICommand DeletePostCommand { get; }

        public EditNeedsPostUrgencyViewModel(string beneficiaryId, string orgId)
        {
            _beneficiaryId = beneficiaryId;
            _orgId = orgId;

            BackCommand = new RelayCommand(_ =>
                NavigationService.Navigate(new View.BeneficiaryDashboardWindow(_beneficiaryId)));

            RefreshCommand = new AsyncRelayCommand(async _ => await LoadPostsAsync());

            SaveUrgencyCommand = new AsyncRelayCommand(async _ =>
            {
                if (SelectedPost == null) return;
                var confirm = MessageBox.Show(
                    $"Submit your edits to \"{SelectedPost.Title}\" for admin review?\n\nYour post will be hidden until re-approved.",
                    "Confirm Edit Submission", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (confirm != MessageBoxResult.Yes) return;
                try
                {
                    IsBusy = true;
                    SelectedPost.Urgency = SelectedUrgency;
                    SelectedPost.Title = EditTitle.Trim();
                    SelectedPost.Description = EditDescription.Trim();
                    SelectedPost.ImagePath = EditImagePath;
                    // Submit edit for admin review — sets Pending, stores snapshot
                    await KapwaDataService.SubmitNeedsPostEditForReview(SelectedPost);
                    MessageBox.Show(
                        "✅ Your edits have been submitted for admin review.\nYour post will be re-activated once approved.",
                        "Submitted", MessageBoxButton.OK, MessageBoxImage.Information);
                    await LoadPostsAsync();
                }
                catch { }
                finally { IsBusy = false; }
            });

            DeletePostCommand = new AsyncRelayCommand(async _ =>
            {
                if (SelectedPost == null) return;
                var confirm = MessageBox.Show(
                    $"Delete \"{SelectedPost.Title}\"? This cannot be undone.",
                    "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (confirm != MessageBoxResult.Yes) return;
                try
                {
                    IsBusy = true;
                    await KapwaDataService.DeleteNeedsPost(SelectedPost.NeedsPost_ID);
                    MyPosts.Remove(SelectedPost);
                    SelectedPost = null;
                    MessageBox.Show("Post deleted.", "Deleted",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch { }
                finally { IsBusy = false; }
            });

            BrowseImageCommand = new RelayCommand(_ =>
            {
                var dlg = new Microsoft.Win32.OpenFileDialog
                {
                    Filter = "Images (*.png;*.jpg;*.jpeg;*.bmp)|*.png;*.jpg;*.jpeg;*.bmp",
                    Title = "Select Need Photo"
                };
                if (dlg.ShowDialog() == true) EditImagePath = dlg.FileName;
            });

            _ = LoadPostsAsync();
        }

        /// <summary>
        /// Pre-selects a specific post when navigating from the dashboard carousel Edit button.
        /// </summary>
        public void PreSelectPost(NeedsPostModel post)
        {
            _pendingSelectId = post.NeedsPost_ID;
            var match = MyPosts.FirstOrDefault(p => p.NeedsPost_ID == post.NeedsPost_ID);
            if (match != null) { SelectedPost = match; _pendingSelectId = null; }
        }

        private async System.Threading.Tasks.Task LoadPostsAsync()
        {
            IsBusy = true;
            try
            {
                var posts = await KapwaDataService.GetNeedsPostsByOrg(_orgId);
                Application.Current.Dispatcher.Invoke(() =>
                {
                    MyPosts.Clear();
                    foreach (var p in posts) MyPosts.Add(p);

                    // Apply pending pre-selection (from dashboard carousel Edit button)
                    if (_pendingSelectId != null)
                    {
                        var match = MyPosts.FirstOrDefault(p => p.NeedsPost_ID == _pendingSelectId);
                        if (match != null) { SelectedPost = match; _pendingSelectId = null; }
                        else SelectedPost = null;
                    }
                    else
                    {
                        SelectedPost = null;
                    }
                });
            }
            catch { }
            finally { IsBusy = false; }
        }
    }
}