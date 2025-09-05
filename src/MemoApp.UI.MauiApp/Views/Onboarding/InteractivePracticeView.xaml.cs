using MemoApp.UI.MauiApp.ViewModels.Onboarding;

namespace MemoApp.UI.MauiApp.Views.Onboarding;

public partial class InteractivePracticeView : ContentView
{
    private InteractivePracticeViewModel? _viewModel;
    private int _wrongAttempts = 0;

    public InteractivePracticeView(InteractivePracticeViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
        
        // Initialize the practice data
        _viewModel.LoadPractice();
        _viewModel.LoadLocalizedText();
        
        // Reset state
        _wrongAttempts = 0;
        
        // Subscribe to ViewModel events for visual feedback
        _viewModel.PropertyChanged += OnViewModelPropertyChanged;
    }

    /// <summary>
    /// Handles property changes from the ViewModel for visual feedback
    /// </summary>
    private async void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        // No longer needed since we're using direct event handlers
    }

    private async void OnOption0Clicked(object sender, EventArgs e)
    {
        await ProcessOptionClick(Option0, 0);
    }

    private async void OnOption1Clicked(object sender, EventArgs e)
    {
        await ProcessOptionClick(Option1, 1);
    }

    private async void OnOption2Clicked(object sender, EventArgs e)
    {
        await ProcessOptionClick(Option2, 2);
    }

    private async Task ProcessOptionClick(Border clickedBorder, int optionIndex)
    {
        try
        {
            if (_viewModel?.Practice == null || optionIndex < 0 || optionIndex >= _viewModel.Practice.Options.Count)
                return;

            // Don't allow clicks if already answered
            if (_viewModel.IsAnswered)
                return;

            var selectedOption = _viewModel.Practice.Options[optionIndex];
            var correctIndex = _viewModel.CorrectAnswerIndex;
            
            if (selectedOption.IsCorrect)
            {
                // Correct answer - turn green
                clickedBorder.BackgroundColor = Colors.Green;
                
                // Show correct feedback
                _viewModel.FeedbackText = GetLocalizedString(_viewModel.Practice.CorrectFeedbackKey);
                _viewModel.FeedbackColor = Colors.Green;
                _viewModel.IsFeedbackVisible = true;
                
                _viewModel.IsAnswered = true;
            }
            else
            {
                // Wrong answer - turn red
                clickedBorder.BackgroundColor = Colors.Red;
                _wrongAttempts++;
                
                // Show incorrect feedback
                _viewModel.FeedbackText = GetLocalizedString(_viewModel.Practice.TryAgainFeedbackKey);
                _viewModel.FeedbackColor = Colors.Red;
                _viewModel.IsFeedbackVisible = true;
                
                // Only show correct answer after second wrong attempt
                if (_wrongAttempts >= 2)
                {
                    await Task.Delay(1500);
                    
                    var borders = new[] { Option0, Option1, Option2 };
                    if (correctIndex >= 0 && correctIndex < borders.Length)
                    {
                        borders[correctIndex].BackgroundColor = Colors.Green;
                    }
                    
                    _viewModel.IsAnswered = true;
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error in ProcessOptionClick: {ex.Message}");
        }
    }
    
    private string GetLocalizedString(string key)
    {
        // For now, use simple hardcoded messages - these could be localized later
        if (key != null && (key.Contains("Correct") || key.Contains("correct")))
        {
            return "Correct! 32 = MN = Moon";
        }
        else
        {
            return "Not quite. Think: 3=M, 2=N. Which word has both sounds?";
        }
    }

    /// <summary>
    /// Clean up event subscriptions
    /// </summary>
    protected override void OnBindingContextChanged()
    {
        if (_viewModel != null)
        {
            _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
        }
        
        base.OnBindingContextChanged();
        
        _viewModel = BindingContext as InteractivePracticeViewModel;
        if (_viewModel != null)
        {
            _viewModel.PropertyChanged += OnViewModelPropertyChanged;
        }
    }
}