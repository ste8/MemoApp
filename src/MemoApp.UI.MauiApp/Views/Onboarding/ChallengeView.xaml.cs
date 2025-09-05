using MemoApp.Localization.Services;

namespace MemoApp.UI.MauiApp.Views.Onboarding;

public partial class ChallengeView : ContentView
{
    private readonly ILocalizationService _localizationService;

    public ChallengeView(ILocalizationService localizationService)
    {
        InitializeComponent();
        _localizationService = localizationService;
        
        // Start the challenge animation when view appears
        Loaded += OnLoaded;
    }

    private async void OnLoaded(object sender, EventArgs e)
    {
        // Show number for 3 seconds
        await Task.Delay(3000);
        
        // Fade out the number
        await ChallengeNumber.FadeTo(0, 500);
        ChallengeNumber.Text = "?";
        await ChallengeNumber.FadeTo(1, 500);
        
        // Show the "hard, right?" message
        StatusLabel.Text = _localizationService.GetString("Onboarding_Challenge_Hard");
        await StatusLabel.FadeTo(1, 500);
    }
}