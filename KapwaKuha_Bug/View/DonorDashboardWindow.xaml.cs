using KapwaKuha.ViewModels;
using KapwaKuha.Services;
using System.Windows;

namespace KapwaKuha.View
{
    public partial class DonorDashboardWindow : Window
    {
            public DonorDashboardWindow(string donorId)
            {
                InitializeComponent();
                var vm = new DonorDashboardViewModel(donorId);
                DataContext = vm;

                // ── Wire up carousel scroll commands to the named ScrollViewer ──
                // The ViewModel exposes an Action the code-behind fills in after InitializeComponent.
                Loaded += (s, e) =>
                {
                    NavigationService.SetCurrent(this);

                    // Hook carousel arrows → PostsCarouselScroller
                    if (DataContext is DonorDashboardViewModel dashVm)
                    {
                        dashVm.CarouselScrollAction = offset =>
                        {
                            // PostsCarouselScroller is the x:Name on the ScrollViewer in XAML
                            if (PostsCarouselScroller != null)
                            {
                                double newOffset = PostsCarouselScroller.HorizontalOffset + offset;
                                PostsCarouselScroller.ScrollToHorizontalOffset(newOffset);
                            }
                        };
                    }
                };
            }
    }
    
}