using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MemoApp.Localization.Services;
using MemoApp.UI.MauiApp.Services.Onboarding;
using Microsoft.Extensions.Logging;

namespace MemoApp.UI.MauiApp.ViewModels.Onboarding;

public partial class MajorSystemExampleViewModel : BaseViewModel
{
    private readonly IMajorSystemOnboardingService _majorSystemService;
    private readonly ILocalizationService _localizationService;
    private readonly ILogger<MajorSystemExampleViewModel> _logger;

    [ObservableProperty]
    private string currentNumber = string.Empty;

    [ObservableProperty]
    private string digitBreakdown = string.Empty;

    [ObservableProperty]
    private string wordFormation = string.Empty;

    [ObservableProperty]
    private string finalWord = string.Empty;

    [ObservableProperty]
    private string associationEmoji = string.Empty;

    [ObservableProperty]
    private string associationText = string.Empty;

    [ObservableProperty]
    private bool isAnimationInProgress = false;

    [ObservableProperty]
    private int currentStep = 0;

    [ObservableProperty]
    private bool showStep1 = false;

    [ObservableProperty]
    private bool showStep2 = false;

    [ObservableProperty]
    private bool showStep3 = false;

    [ObservableProperty]
    private bool showStep4 = false;

    public string Step1Title => _localizationService.GetString("Onboarding_Example_Step1");
    public string Step2Title => _localizationService.GetString("Onboarding_Example_Step2");
    public string Step3Title => _localizationService.GetString("Onboarding_Example_Step3");
    public string Step4Title => _localizationService.GetString("Onboarding_Example_Step4");

    public string ExampleExplanation => _localizationService.GetString("Onboarding_Example_Explanation");

    public MajorSystemExampleViewModel(
        IMajorSystemOnboardingService majorSystemService,
        ILocalizationService localizationService,
        ILogger<MajorSystemExampleViewModel> logger)
    {
        _majorSystemService = majorSystemService;
        _localizationService = localizationService;
        _logger = logger;

        Title = _localizationService.GetString("Onboarding_Example_Title");
    }

    public async Task InitializeAsync()
    {
        try
        {
            _logger.LogInformation("Initializing Major System example");
            
            // Get the appropriate example for current language
            var example = _majorSystemService.GetExample();
            
            CurrentNumber = example.Number.ToString();
            FinalWord = example.Word;
            AssociationEmoji = example.Emoji;
            AssociationText = _localizationService.GetString(example.RememberTextKey);
            
            // Build digit breakdown from conversion steps - each conversion on new line
            DigitBreakdown = string.Join("\n", example.ConversionSteps.Select(step => step.DisplayText));
            
            // Build word formation showing the letters
            WordFormation = string.Join("", example.ConversionSteps.Select(step => step.Letter));

            _logger.LogInformation("Example loaded: {Number} → {Word}", 
                CurrentNumber, FinalWord);

            // Start the demonstration animation
            await StartDemonstrationAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize Major System example");
        }
    }

    [RelayCommand]
    private async Task StartDemonstration()
    {
        await StartDemonstrationAsync();
    }

    private async Task StartDemonstrationAsync()
    {
        IsAnimationInProgress = true;
        CurrentStep = 0;

        // Reset all steps
        ShowStep1 = false;
        ShowStep2 = false;
        ShowStep3 = false;
        ShowStep4 = false;

        // Step 1: Show the number
        CurrentStep = 1;
        ShowStep1 = true;
        await Task.Delay(1500);

        // Step 2: Show digit breakdown
        CurrentStep = 2;
        ShowStep2 = true;
        await Task.Delay(2000);

        // Step 3: Show word formation
        CurrentStep = 3;
        ShowStep3 = true;
        await Task.Delay(2000);

        // Step 4: Show final association
        CurrentStep = 4;
        ShowStep4 = true;
        await Task.Delay(1500);

        IsAnimationInProgress = false;
        _logger.LogInformation("Demonstration completed for {Number} → {Word}", CurrentNumber, FinalWord);
    }

    [RelayCommand]
    private async Task Continue()
    {
        if (IsAnimationInProgress)
        {
            _logger.LogInformation("User tapped during animation, skipping to end");
            // Skip to the end of animation
            CurrentStep = 4;
            ShowStep1 = true;
            ShowStep2 = true;
            ShowStep3 = true;
            ShowStep4 = true;
            IsAnimationInProgress = false;
        }
        else
        {
            _logger.LogInformation("User completed example viewing, ready to continue");
        }
    }

    public bool IsDesktop => DeviceInfo.Platform == DevicePlatform.WinUI || DeviceInfo.Platform == DevicePlatform.macOS;
    public bool IsTablet => DeviceInfo.Idiom == DeviceIdiom.Tablet;
    public bool IsMobile => DeviceInfo.Idiom == DeviceIdiom.Phone;
}