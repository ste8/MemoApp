using MemoApp.UI.MauiApp.Services;
using MemoApp.UI.MauiApp.ViewModels.FlashNumbers;

namespace MemoApp.UI.MauiApp.Views.FlashNumbers;

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
        if (string.IsNullOrEmpty(e.NewTextValue))
            return;

        var lastChar = e.NewTextValue.LastOrDefault();
        var upperChar = char.ToUpperInvariant(lastChar);
        System.Diagnostics.Debug.WriteLine($"Key pressed: {upperChar}");

        // Handle keyboard shortcuts for both active sessions and completed sessions
        switch (upperChar)
        {
            case 'N':
            case ' ': // Space bar for next (more intuitive)
                if (_viewModel.IsSessionActive && _viewModel.AdvanceToNextCommand?.CanExecute(null) == true)
                {
                    System.Diagnostics.Debug.WriteLine("Executing AdvanceToNextCommand");
                    _viewModel.AdvanceToNextCommand.Execute(null);
                }
                break;
                
            case 'R':
            case 'H': // Keep H for backward compatibility
                if (_viewModel.IsSessionActive && _viewModel.ToggleAssociationRevealCommand?.CanExecute(null) == true)
                {
                    System.Diagnostics.Debug.WriteLine("Executing ToggleAssociationRevealCommand");
                    _viewModel.ToggleAssociationRevealCommand.Execute(null);
                }
                break;
                
            case 'S':
                if (_viewModel.IsSessionCompleted && _viewModel.ShowStatisticsCommand?.CanExecute(null) == true)
                {
                    System.Diagnostics.Debug.WriteLine("Executing ShowStatisticsCommand");
                    _viewModel.ShowStatisticsCommand.Execute(null);
                }
                break;
                
            case (char)27: // ESC key
                if (_viewModel.StartNewSessionCommand?.CanExecute(null) == true)
                {
                    System.Diagnostics.Debug.WriteLine("Executing StartNewSessionCommand (ESC pressed)");
                    _viewModel.StartNewSessionCommand.Execute(null);
                }
                break;
        }

        // Clear the text to be ready for next input
        if (sender is Entry entry)
        {
            // Use dispatcher to avoid issues with text change during event
            Dispatcher.Dispatch(() => entry.Text = "");
        }
    }
}