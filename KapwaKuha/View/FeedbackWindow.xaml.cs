using KapwaKuha.ViewModels;
using KapwaKuha.Services;
using System.Windows;

namespace KapwaKuha.View
{
    public partial class FeedbackWindow : Window
    {
        public FeedbackWindow(string claimId, string donorId, string donorName)
        {
            InitializeComponent();
            var vm = new FeedbackViewModel(claimId, donorId, donorName);
            vm.OnSubmitted += () => this.Close();
            DataContext = vm;
        }
    }
}