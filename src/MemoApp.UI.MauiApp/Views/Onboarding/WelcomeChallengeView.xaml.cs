using MemoApp.UI.MauiApp.ViewModels.Onboarding;
using Microsoft.Extensions.Logging;

namespace MemoApp.UI.MauiApp.Views.Onboarding;

public partial class WelcomeChallengeView : ContentView
{
    private readonly ILogger<WelcomeChallengeView> _logger;
    private readonly WelcomeChallengeViewModel _viewModel;

    public WelcomeChallengeView(
        WelcomeChallengeViewModel viewModel,
        ILogger<WelcomeChallengeView> logger)
    {
        InitializeComponent();
        _viewModel = viewModel;
        _logger = logger;
        BindingContext = viewModel;

        _logger.LogInformation("Initialized rhetorical challenge demonstration view");
    }
}