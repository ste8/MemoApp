using MemoApp.UI.MauiApp.ViewModels.Onboarding;
using Microsoft.Extensions.Logging;

namespace MemoApp.UI.MauiApp.Views.Onboarding;

public partial class WelcomeChallengeView : ContentView
{
    private readonly ILogger<WelcomeChallengeView> _logger;
    private readonly List<View> _panels;
    private int _currentPanelIndex = 0;
    private bool _isAnimating = false;

    // Animation constants
    private const uint AnimationDuration = 350;
    private const double ExitScale = 0.95;
    private const double EnterScale = 0.95;
    private const double ExitOpacity = 0.3;

    public WelcomeChallengeView(
        WelcomeChallengeViewModel viewModel,
        ILogger<WelcomeChallengeView> logger)
    {
        InitializeComponent();
        _logger = logger;
        BindingContext = viewModel;

        // Initialize panels list
        _panels = new List<View>
        {
            EasyPanel,
            MediumPanel,
            HardPanel,
            ConclusionPanel
        };

        _logger.LogInformation("Initialized WelcomeChallengeView with sequential panel reveal");
    }

    /// <summary>
    /// Handles the parent's Next button - advances to next panel or returns true when all panels shown
    /// </summary>
    public async Task<bool> HandleNextAsync()
    {
        if (_currentPanelIndex < _panels.Count - 1)
        {
            await TransitionToPanel(_currentPanelIndex + 1);
            return false; // Not finished with this page yet
        }

        return true; // All panels shown, ready to advance to next onboarding step
    }

    /// <summary>
    /// Handles the parent's Back button - goes to previous panel or returns true when at first panel
    /// </summary>
    public async Task<bool> HandleBackAsync()
    {
        if (_currentPanelIndex > 0)
        {
            await TransitionToPanel(_currentPanelIndex - 1);
            return false; // Handled internally
        }

        return true; // At first panel, allow parent to go back
    }

    private async Task TransitionToPanel(int newIndex)
    {
        if (_isAnimating || newIndex == _currentPanelIndex || newIndex < 0 || newIndex >= _panels.Count)
            return;

        _isAnimating = true;

        try
        {
            var exitingPanel = _panels[_currentPanelIndex];
            var enteringPanel = _panels[newIndex];
            var forward = newIndex > _currentPanelIndex;

            await AnimatePanelTransition(exitingPanel, enteringPanel, forward);

            _currentPanelIndex = newIndex;
            _logger.LogInformation("Transitioned to panel {PanelIndex}", newIndex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during panel transition to index {NewIndex}", newIndex);
        }
        finally
        {
            _isAnimating = false;
        }
    }

    private async Task AnimatePanelTransition(View exitingPanel, View enteringPanel, bool forward)
    {
        var direction = forward ? -1 : 1;
        var screenWidth = DeviceDisplay.MainDisplayInfo.Width / DeviceDisplay.MainDisplayInfo.Density;

        // Prepare entering panel
        enteringPanel.TranslationX = screenWidth * -direction;
        enteringPanel.Scale = EnterScale;
        enteringPanel.Opacity = 0.8;
        enteringPanel.IsVisible = true;

        // Animate both panels simultaneously
        var exitTask = Task.WhenAll(
            exitingPanel.TranslateTo(screenWidth * direction, 0, AnimationDuration, Easing.CubicInOut),
            exitingPanel.ScaleTo(ExitScale, AnimationDuration, Easing.CubicIn),
            exitingPanel.FadeTo(ExitOpacity, AnimationDuration)
        );

        var enterTask = Task.WhenAll(
            enteringPanel.TranslateTo(0, 0, AnimationDuration, Easing.CubicInOut),
            enteringPanel.ScaleTo(1, AnimationDuration, Easing.CubicOut),
            enteringPanel.FadeTo(1, AnimationDuration)
        );

        await Task.WhenAll(exitTask, enterTask);

        // Clean up exiting panel
        exitingPanel.IsVisible = false;
        exitingPanel.TranslationX = 0;
        exitingPanel.Scale = 1;
        exitingPanel.Opacity = 1;
    }
}
