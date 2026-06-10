// FILE: ViewModels/FeedbackViewModel.cs  (NEW)
using System.Windows;
using System.Windows.Input;
using KapwaKuha.Commands;
using KapwaKuha.Models;
using KapwaKuha.Services;

namespace KapwaKuha.ViewModels
{
    public class FeedbackViewModel : ObservableObject
    {
        private readonly string _claimId;
        private readonly string _donorId;
        private readonly string _donorName;

        private int _selectedStars = 5;
        public int SelectedStars
        {
            get => _selectedStars;
            set
            {
                _selectedStars = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(StarDisplay));
                OnPropertyChanged(nameof(Star1Filled));
                OnPropertyChanged(nameof(Star2Filled));
                OnPropertyChanged(nameof(Star3Filled));
                OnPropertyChanged(nameof(Star4Filled));
                OnPropertyChanged(nameof(Star5Filled));
            }
        }

        private string _comment = string.Empty;
        public string Comment
        {
            get => _comment;
            set { _comment = value; OnPropertyChanged(); }
        }

        private string _errorMessage = string.Empty;
        public string ErrorMessage
        {
            get => _errorMessage;
            set { _errorMessage = value; OnPropertyChanged(); }
        }

        private bool _errorVisible;
        public bool ErrorVisible
        {
            get => _errorVisible;
            set { _errorVisible = value; OnPropertyChanged(); }
        }

        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            set { _isLoading = value; OnPropertyChanged(); }
        }

        public string DonorLabel => $"Rate your donation from: {_donorName}";
        public string StarDisplay => new string('★', SelectedStars) + new string('☆', 5 - SelectedStars);

        // Per-star fill — bound to star button backgrounds in XAML
        public bool Star1Filled => SelectedStars >= 1;
        public bool Star2Filled => SelectedStars >= 2;
        public bool Star3Filled => SelectedStars >= 3;
        public bool Star4Filled => SelectedStars >= 4;
        public bool Star5Filled => SelectedStars >= 5;

        public ICommand SetStarCommand { get; }
        public ICommand SubmitCommand { get; }
        public ICommand CancelCommand { get; }

        // Action invoked after successful submit so the calling window can close
        public System.Action? OnSubmitted { get; set; }

        public FeedbackViewModel(string claimId, string donorId, string donorName)
        {
            _claimId = claimId;
            _donorId = donorId;
            _donorName = donorName;

            SetStarCommand = new RelayCommand(param =>
            {
                if (param == null) return;
                if (int.TryParse(param.ToString(), out int stars))
                    SelectedStars = stars;
            });

            CancelCommand = new RelayCommand(_ => OnSubmitted?.Invoke());

            SubmitCommand = new AsyncRelayCommand(async _ =>
            {
                IsLoading = true;
                ErrorVisible = false;
                try
                {
                    // Guard: already rated?
                    bool alreadyRated = await KapwaDataService.HasAlreadyRatedClaim(_claimId);
                    if (alreadyRated)
                    {
                        ErrorMessage = "You have already submitted feedback for this claim.";
                        ErrorVisible = true;
                        return;
                    }

                    string fbId = await KapwaDataService.GetNextFeedbackId();
                    var fb = new FeedbackModel
                    {
                        Feedback_ID = fbId,
                        Donor_ID = _donorId,
                        Claim_ID = _claimId,
                        Stars = SelectedStars,
                        Comment = Comment
                    };
                    await KapwaDataService.SubmitFeedback(fb);
                    MessageBox.Show(
                        $"Thank you for your feedback! You rated {SelectedStars} ★",
                        "Feedback Submitted", MessageBoxButton.OK, MessageBoxImage.Information);
                    OnSubmitted?.Invoke();
                }
                catch { }
                finally { IsLoading = false; }
            });
        }
    }
}