using MemoApp.Localization.Services;

namespace MemoApp.UI.MauiApp.Views.Onboarding;

public partial class InteractivePracticeView : ContentView
{
    private readonly ILocalizationService _localizationService;

    public InteractivePracticeView(ILocalizationService localizationService)
    {
        InitializeComponent();
        _localizationService = localizationService;
    }

    private async void OnOption1Clicked(object sender, EventArgs e)
    {
        System.Diagnostics.Debug.WriteLine("Option 1 (Moon) clicked - CORRECT!");
        await ProcessOptionClick(Option1, true);
    }

    private async void OnOption2Clicked(object sender, EventArgs e)
    {
        System.Diagnostics.Debug.WriteLine("Option 2 (Mum) clicked - INCORRECT");
        await ProcessOptionClick(Option2, false);
    }

    private async void OnOption3Clicked(object sender, EventArgs e)
    {
        System.Diagnostics.Debug.WriteLine("Option 3 (Onion) clicked - INCORRECT");
        await ProcessOptionClick(Option3, false);
    }

    private async Task ProcessOptionClick(Border clickedBorder, bool isCorrect)
    {
        try
        {
            if (isCorrect)
            {
                clickedBorder.BackgroundColor = Colors.Green;
                FeedbackLabel.Text = _localizationService.GetString("Onboarding_Practice_Correct");
                FeedbackLabel.TextColor = Colors.Green;
            }
            else
            {
                clickedBorder.BackgroundColor = Colors.Red;
                FeedbackLabel.Text = _localizationService.GetString("Onboarding_Practice_TryAgain");
                FeedbackLabel.TextColor = Colors.Red;
                
                // Show correct answer after a delay
                await Task.Delay(1000);
                Option1.BackgroundColor = Colors.Green;
            }
            
            await FeedbackLabel.FadeTo(1, 300);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error in ProcessOptionClick: {ex.Message}");
            
            // Fallback - show some visual feedback
            FeedbackLabel.Text = "Tap detected!";
            FeedbackLabel.TextColor = Colors.Blue;
            await FeedbackLabel.FadeTo(1, 300);
        }
    }
}