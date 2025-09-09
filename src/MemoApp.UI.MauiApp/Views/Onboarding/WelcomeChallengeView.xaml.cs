using Microsoft.Extensions.Logging;

namespace MemoApp.UI.MauiApp.Views.Onboarding;

public partial class WelcomeChallengeView : ContentView
{
    private readonly ILogger<WelcomeChallengeView> _logger;
    private bool _animationStarted = false;

    public WelcomeChallengeView(ILogger<WelcomeChallengeView> logger)
    {
        InitializeComponent();
        _logger = logger;
    }

    protected override void OnParentSet()
    {
        base.OnParentSet();
        
        // Start animation when view becomes visible
        if (Parent != null && !_animationStarted)
        {
            _animationStarted = true;
            Device.BeginInvokeOnMainThread(async () =>
            {
                try
                {
                    await Task.Delay(1500); // Wait after welcome content is visible
                    await AnimateChallengeReveal();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during challenge animation");
                }
            });
        }
    }

    private async Task AnimateChallengeReveal()
    {
        try
        {
            // Animate status label appearance
            await StatusLabel.FadeTo(1, 600, Easing.CubicOut);
            
            // Brief pause then number highlight effect
            await Task.Delay(800);
            
            // Subtle number frame pulse effect
            await NumberFrame.ScaleTo(1.05, 200, Easing.CubicOut);
            await NumberFrame.ScaleTo(1.0, 200, Easing.CubicIn);
            
            // Change status to challenge user
            await Task.Delay(1500);
            await StatusLabel.FadeTo(0.7, 300);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in challenge reveal animation");
        }
    }
}