using MemoApp.UI.MauiApp.ViewModels;

namespace MemoApp.UI.MauiApp.Views;

public partial class TrainingSessionPage : ContentPage
{
    private readonly TrainingSessionViewModel _viewModel;

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