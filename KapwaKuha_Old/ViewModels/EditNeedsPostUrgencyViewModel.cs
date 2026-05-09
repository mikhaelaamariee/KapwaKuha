using System.Collections.ObjectModel;
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

                // --- LOADING LOGIC: Populates fields when a post is clicked ---
                if (value != null)
                {
                    SelectedUrgency = value.Urgency;
                    EditTitle = value.Title;
                    EditImagePath = value.ImagePath ?? string.Empty;
                }
                else
                {
                    // Clear fields if nothing is selected
                    EditTitle = string.Empty;
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

        // --- EDIT FIELDS ---
        private string _editTitle = string.Empty;
        public string EditTitle
        {
            get => _editTitle;
            set { _editTitle = value; OnPropertyChanged(); }
        }

        private string _editImagePath = string.Empty;
        public string EditImagePath
        {
            get => _editImagePath;
            set { _editImagePath = value; OnPropertyChanged(); OnPropertyChanged(nameof(HasEditImage)); }
        }

        public bool HasEditImage =>
            !string.IsNullOrEmpty(_editImagePath) && System.IO.File.Exists(_editImagePath);

        // --- COMMANDS ---
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
                    $"Save changes to \"{SelectedPost.Title}\"?",
                    "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (confirm != MessageBoxResult.Yes) return;

                try
                {
                    IsBusy = true;

                    // --- SAVING LOGIC: Assign edited values back to the model ---
                    SelectedPost.Urgency = SelectedUrgency;
                    SelectedPost.Title = EditTitle.Trim();
                    SelectedPost.ImagePath = EditImagePath;

                    // Call the new Update method to save to database
                    await KapwaDataService.UpdateNeedsPost(SelectedPost);

                    MessageBox.Show("✅ Post updated successfully!", "Saved",
                        MessageBoxButton.OK, MessageBoxImage.Information);

                    await LoadPostsAsync(); // Refresh list to show changes
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
                    SelectedPost = null;
                });
            }
            catch { }
            finally { IsBusy = false; }
        }
    }
}