using MemoApp.UI.MauiApp.Services;
using MemoApp.UI.MauiApp.ViewModels;

namespace MemoApp.UI.MauiApp.Views;

public partial class FlashNumbersPage : ContentPage
{
    private readonly FlashNumbersViewModel _viewModel;
    private readonly IPackUpdateBackgroundService? _updateService;

    public FlashNumbersPage(FlashNumbersViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
        
        // Get update service if available
        _updateService = Application.Current?.Handler?.MauiContext?.Services
            .GetService<IPackUpdateBackgroundService>();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        
        // Notify update service that user is in an activity
        _updateService?.SetUserActivityState(true);
        
        await _viewModel.InitializeAsync();
        
        // Focus the keyboard handler for desktop platforms
        if (DeviceInfo.Platform == DevicePlatform.MacCatalyst || DeviceInfo.Platform == DevicePlatform.WinUI)
        {
            // Give it a moment for the page to fully load
            Dispatcher.DispatchDelayed(TimeSpan.FromMilliseconds(500), () =>
            {
                KeyboardInputHandler.Focus();
            });
        }
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        
        // Notify update service that user is no longer in an activity
        _updateService?.SetUserActivityState(false);
    }


    private void OnKeyboardTextChanged(object sender, TextChangedEventArgs e)
    {
        if (string.IsNullOrEmpty(e.NewTextValue) || !_viewModel.IsSessionActive)
            return;

        var lastChar = e.NewTextValue.LastOrDefault();
        System.Diagnostics.Debug.WriteLine($"Key pressed: {lastChar}");

        // Check if 'N' was pressed
        if (char.ToUpperInvariant(lastChar) == 'N')
        {
            System.Diagnostics.Debug.WriteLine("Executing AdvanceToNextCommand");
            _viewModel.AdvanceToNextCommand?.Execute(null);
        }

        // Clear the text to be ready for next input
        if (sender is Entry entry)
        {
            // Use dispatcher to avoid issues with text change during event
            Dispatcher.Dispatch(() => entry.Text = "");
        }
    }
}