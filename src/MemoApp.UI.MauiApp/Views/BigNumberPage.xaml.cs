using MemoApp.UI.MauiApp.ViewModels;

namespace MemoApp.UI.MauiApp.Views;

public partial class BigNumberPage : ContentPage
{
    public BigNumberPage(BigNumberViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
        
        // Subscribe to auto-scroll events from ViewModel
        viewModel.ScrollToVerificationResults += OnScrollToVerificationResults;
    }
    
    private async void OnScrollToVerificationResults(object? sender, EventArgs e)
    {
        try
        {
            // Wait a brief moment for the results section to become visible
            await Task.Delay(100);
            
            // Auto-scroll to bring verification results into view
            await MainScrollView.ScrollToAsync(VerificationResultsSection, ScrollToPosition.Start, true);
            
            // Add haptic feedback on mobile platforms
#if ANDROID || IOS
            if (DeviceInfo.Platform == DevicePlatform.Android || DeviceInfo.Platform == DevicePlatform.iOS)
            {
                try 
                {
                    HapticFeedback.Default.Perform(HapticFeedbackType.Click);
                }
                catch 
                {
                    // Haptic feedback not available, continue without it
                }
            }
#endif
        }
        catch (Exception ex)
        {
            // Log error but don't crash the app
            System.Diagnostics.Debug.WriteLine($"Auto-scroll error: {ex.Message}");
        }
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        
        if (BindingContext is BigNumberViewModel viewModel)
        {
            // Set the container element reference for accurate size measurement
            viewModel.SetContainerElement(NumberDisplayFrame);
            
            await viewModel.InitializeAsync();
        }
    }
    
    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        
        // Unsubscribe from events to prevent memory leaks
        if (BindingContext is BigNumberViewModel viewModel)
        {
            viewModel.ScrollToVerificationResults -= OnScrollToVerificationResults;
        }
    }
}