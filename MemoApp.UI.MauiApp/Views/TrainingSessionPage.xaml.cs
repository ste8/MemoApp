using MemoApp.UI.MauiApp.ViewModels;

namespace MemoApp.UI.MauiApp.Views;

public partial class TrainingSessionPage : ContentPage
{
    private readonly TrainingSessionViewModel _viewModel;
    private bool _keyboardHandlerHasFocus = false;

    public TrainingSessionPage(TrainingSessionViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.InitializeAsync();
        
        // Give focus to the invisible keyboard handler for desktop platforms
        if (DeviceInfo.Platform == DevicePlatform.MacCatalyst || DeviceInfo.Platform == DevicePlatform.WinUI)
        {
            KeyboardHandler.Focus();
        }
    }

    private void OnKeyboardInput(object sender, TextChangedEventArgs e)
    {
        if (string.IsNullOrEmpty(e.NewTextValue))
            return;

        // Get the last character typed
        var lastChar = e.NewTextValue.LastOrDefault();
        
        // Check if 'N' was pressed and session is active
        if (char.ToUpperInvariant(lastChar) == 'N' && _viewModel.IsSessionActive)
        {
            _viewModel.AdvanceToNextCommand?.Execute(null);
        }
        
        // Clear the entry to be ready for next input
        if (sender is Entry entry)
        {
            entry.Text = "";
        }
    }

    private void OnKeyboardHandlerFocused(object sender, FocusEventArgs e)
    {
        _keyboardHandlerHasFocus = true;
    }

    private void OnKeyboardHandlerUnfocused(object sender, FocusEventArgs e)
    {
        _keyboardHandlerHasFocus = false;
        
        // Refocus if we're still on this page and on desktop
        if (DeviceInfo.Platform == DevicePlatform.MacCatalyst || DeviceInfo.Platform == DevicePlatform.WinUI)
        {
            // Small delay to avoid focus fighting
            Dispatcher.DispatchDelayed(TimeSpan.FromMilliseconds(100), () =>
            {
                if (!_keyboardHandlerHasFocus && _viewModel.IsSessionActive)
                {
                    KeyboardHandler.Focus();
                }
            });
        }
    }
}