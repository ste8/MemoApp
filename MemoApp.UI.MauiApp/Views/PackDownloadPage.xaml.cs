using MemoApp.UI.MauiApp.ViewModels;

namespace MemoApp.UI.MauiApp.Views;

public partial class PackDownloadPage : ContentPage
{
    private readonly PackDownloadViewModel _viewModel;

    public PackDownloadPage(PackDownloadViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        
        // Initialize asynchronously without blocking UI
        _ = InitializeViewModelAsync();
    }

    private async Task InitializeViewModelAsync()
    {
        try
        {
            await _viewModel.InitializeAsync();
        }
        catch (Exception ex)
        {
            // Log error but don't crash the UI
            System.Diagnostics.Debug.WriteLine($"Error initializing PackDownloadPage: {ex}");
            
            // Show error on main thread
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                await DisplayAlert("Error", "Failed to load packs. Please try refreshing.", "OK");
            });
        }
    }
}