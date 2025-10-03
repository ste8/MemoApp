using CommunityToolkit.Mvvm.ComponentModel;
using MemoApp.Localization.Services;
using Microsoft.Extensions.Logging;

namespace MemoApp.UI.MauiApp.ViewModels.Onboarding;

/// <summary>
/// ViewModel for the Welcome Challenge view with rhetorical demonstrations.
/// Shows multiple "would you be able to..." examples to create desire for training.
/// Uses sequential panel reveal with animations controlled by the View.
/// </summary>
public partial class WelcomeChallengeViewModel : BaseViewModel
{
    private readonly ILocalizationService _localizationService;
    private readonly ILogger<WelcomeChallengeViewModel> _logger;

    [ObservableProperty]
    private string easyNumber = "7394";

    [ObservableProperty]
    private string mediumNumber = "8251739406";

    [ObservableProperty]
    private string hardNumber = "94736281059473628190";

    [ObservableProperty]
    private string easyQuestion = "";

    [ObservableProperty]
    private string mediumQuestion = "";

    [ObservableProperty]
    private string hardQuestion = "";

    [ObservableProperty]
    private string conclusionMessage = "";

    public WelcomeChallengeViewModel(
        ILocalizationService localizationService,
        ILogger<WelcomeChallengeViewModel> logger)
    {
        _localizationService = localizationService;
        _logger = logger;

        LoadRhetoricalQuestions();

        _logger.LogInformation("Initialized rhetorical challenge demonstration with sequential panels");
    }

    private void LoadRhetoricalQuestions()
    {
        // Load localized rhetorical questions
        EasyQuestion = _localizationService.GetString("Onboarding_RhetoricalQuestion_Easy");
        MediumQuestion = _localizationService.GetString("Onboarding_RhetoricalQuestion_Medium");
        HardQuestion = _localizationService.GetString("Onboarding_RhetoricalQuestion_Hard");
        ConclusionMessage = _localizationService.GetString("Onboarding_RhetoricalConclusion");

        _logger.LogInformation("Loaded rhetorical questions for demonstration");
    }
}
