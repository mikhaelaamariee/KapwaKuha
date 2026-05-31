// FILE: Models/FeedbackModel.cs  (NEW — replaces DonorRatings)
using System;
using KapwaKuha.ViewModels;

namespace KapwaKuha.Models
{
    public class FeedbackModel : ObservableObject
    {
        public string Feedback_ID { get; set; } = string.Empty;
        public string Donor_ID { get; set; } = string.Empty;
        public string Claim_ID { get; set; } = string.Empty;

        private int _stars = 5;
        public int Stars
        {
            get => _stars;
            set
            {
                // Enforce 1-5 at model layer too — mirrors SQL CHECK constraint
                _stars = Math.Clamp(value, 1, 5);
                OnPropertyChanged();
                OnPropertyChanged(nameof(StarDisplay));
            }
        }

        public string Comment { get; set; } = string.Empty;
        public DateTime RatedAt { get; set; } = DateTime.Now;

        // UI helper: "★★★★☆" style display
        public string StarDisplay => new string('★', Stars) + new string('☆', 5 - Stars);

        // Badge color for star count
        public string StarColor => Stars >= 4 ? "#2DC653" : Stars == 3 ? "#B8860B" : "#C0304A";
    }
}