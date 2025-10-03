using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MemoApp.Localization.Services;
using MemoApp.UI.MauiApp.Views.Onboarding;
using Microsoft.Extensions.Logging;

namespace MemoApp.UI.MauiApp.ViewModels.Onboarding;

public partial class OnboardingNavigationViewModel : BaseViewModel
{
    private readonly ILocalizationService _localizationService;
    private readonly ILogger<OnboardingNavigationViewModel> _logger;
    private readonly IServiceProvider _serviceProvider;

    [ObservableProperty]
    private ObservableCollection<View> onboardingPages = new();

    [ObservableProperty]
    private int currentPosition;

    [ObservableProperty]
    private bool canSkip = false; // Start with no skip for Phase 1

    [ObservableProperty]
    private bool canGoBack;

    [ObservableProperty]
    private string currentPhaseTitle = "";

    [ObservableProperty]
    private bool isPhase1Complete = false;

    [ObservableProperty]
    private string nextButtonText;

    [ObservableProperty]
    private string progressText = "";

    public OnboardingNavigationViewModel(
        ILocalizationService localizationService,
        ILogger<OnboardingNavigationViewModel> logger,
        IServiceProvider serviceProvider)
    {
        _localizationService = localizationService;
        _logger = logger;
        _serviceProvider = serviceProvider;
        
        Title = _localizationService.GetString("Onboarding_Title");
        NextButtonText = _localizationService.GetString("Onboarding_Next");
    }

    public async Task InitializeAsync()
    {
        try
        {
            _logger.LogInformation("Initializing onboarding pages");

            // Create comprehensive 8-step onboarding pages
            OnboardingPages.Clear();

            // Step 1: Welcome + Challenge (merged)
            var welcomeChallengeView = _serviceProvider.GetRequiredService<WelcomeChallengeView>();
            OnboardingPages.Add(welcomeChallengeView);

            // Step 2: Memory Techniques Context
            var memoryTechniquesView = _serviceProvider.GetRequiredService<MemoryTechniquesContextView>();
            OnboardingPages.Add(memoryTechniquesView);

            // Step 3: Major System Introduction (concept explanation)
            var majorSystemView = _serviceProvider.GetRequiredService<MajorSystemIntroView>();
            OnboardingPages.Add(majorSystemView);

            // Step 4: Concrete Example (41→RAT, 31→MOTO demonstration)
            var exampleView = _serviceProvider.GetRequiredService<MajorSystemExampleView>();
            OnboardingPages.Add(exampleView);

            // Step 5: Interactive Practice (quiz)
            var practiceView = _serviceProvider.GetRequiredService<InteractivePracticeView>();
            OnboardingPages.Add(practiceView);

            // Step 6: Expectation-Setting Transition Page
            _logger.LogInformation("Creating OnboardingSettingsView...");
            var settingsTransitionView = _serviceProvider.GetRequiredService<OnboardingSettingsView>();
            if (settingsTransitionView == null)
            {
                _logger.LogError("OnboardingSettingsView is null!");
                throw new InvalidOperationException("Failed to create OnboardingSettingsView");
            }
            OnboardingPages.Add(settingsTransitionView);
            _logger.LogInformation("OnboardingSettingsView added successfully at index {Index}", OnboardingPages.Count - 1);

            // Step 7: Number Format Selection
            _logger.LogInformation("Creating OnboardingNumberFormatView...");
            var numberFormatView = _serviceProvider.GetRequiredService<OnboardingNumberFormatView>();
            if (numberFormatView == null)
            {
                _logger.LogError("OnboardingNumberFormatView is null!");
                throw new InvalidOperationException("Failed to create OnboardingNumberFormatView");
            }
            OnboardingPages.Add(numberFormatView);
            _logger.LogInformation("OnboardingNumberFormatView added successfully at index {Index}", OnboardingPages.Count - 1);

            // Step 8: Pack Download (final step, leads directly to main app)
            _logger.LogInformation("Creating PackDownloadOnboardingView...");
            var packDownloadView = _serviceProvider.GetRequiredService<PackDownloadOnboardingView>();
            if (packDownloadView == null)
            {
                _logger.LogError("PackDownloadOnboardingView is null!");
                throw new InvalidOperationException("Failed to create PackDownloadOnboardingView");
            }
            OnboardingPages.Add(packDownloadView);
            _logger.LogInformation("PackDownloadOnboardingView added successfully at index {Index}", OnboardingPages.Count - 1);

            _logger.LogInformation("Total onboarding pages added: {PageCount}", OnboardingPages.Count);

            // Validate that all expected views were added
            if (OnboardingPages.Count != OnboardingPhaseConfig.TOTAL_STEP_COUNT)
            {
                _logger.LogError("Expected {ExpectedCount} onboarding pages but got {ActualCount}!",
                    OnboardingPhaseConfig.TOTAL_STEP_COUNT, OnboardingPages.Count);
                throw new InvalidOperationException($"Expected {OnboardingPhaseConfig.TOTAL_STEP_COUNT} onboarding pages but got {OnboardingPages.Count}");
            }
            
            // Log all view types for debugging
            for (int i = 0; i < OnboardingPages.Count; i++)
            {
                _logger.LogInformation("Position {Index}: {ViewType}", i, OnboardingPages[i].GetType().Name);
            }
            
            UpdateNavigationState();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize onboarding");
        }
    }

    partial void OnCurrentPositionChanged(int value)
    {
        UpdateNavigationState();
    }

    private void UpdateNavigationState()
    {
        CanGoBack = CurrentPosition > 0;

        // Phase-based navigation logic using configuration
        var isInPhase1 = OnboardingPhaseConfig.IsInTutorialPhase(CurrentPosition);
        var isInPhase2 = OnboardingPhaseConfig.IsInSetupPhase(CurrentPosition);

        // Allow skip on both Phase 1 and Phase 2, except on the final step
        CanSkip = CurrentPosition < OnboardingPages.Count - 1;

        // Update phase indicators
        IsPhase1Complete = CurrentPosition > OnboardingPhaseConfig.TUTORIAL_PHASE_LAST_POSITION;

        // Update phase title and progress text
        if (isInPhase1)
        {
            CurrentPhaseTitle = _localizationService.GetString("Onboarding_Phase1_Tutorial");
            var phaseStep = OnboardingPhaseConfig.GetPhaseStepNumber(CurrentPosition);
            ProgressText = $"Tutorial: {phaseStep} of {OnboardingPhaseConfig.TUTORIAL_PHASE_STEP_COUNT}";
        }
        else
        {
            CurrentPhaseTitle = _localizationService.GetString("Onboarding_Phase2_Setup");
            var phaseStep = OnboardingPhaseConfig.GetPhaseStepNumber(CurrentPosition);
            ProgressText = $"Setup: {phaseStep} of {OnboardingPhaseConfig.SETUP_PHASE_STEP_COUNT}";
        }

        _logger.LogInformation("UpdateNavigationState - Position: {CurrentPosition}/{TotalPages}, Phase: {Phase}, CanGoBack: {CanGoBack}, CanSkip: {CanSkip}",
            CurrentPosition, OnboardingPages.Count - 1, isInPhase1 ? "Tutorial" : "Setup", CanGoBack, CanSkip);

        // Update button text based on position and phase
        if (CurrentPosition == OnboardingPhaseConfig.TUTORIAL_PHASE_LAST_POSITION)
        {
            // End of Phase 1 - transition to Phase 2
            NextButtonText = _localizationService.GetString("Onboarding_StartSetup");
            _logger.LogInformation("End of Phase 1 - NextButtonText set to 'Start Setup'");
        }
        else if (CurrentPosition == OnboardingPages.Count - 1)
        {
            // Final page - complete onboarding
            NextButtonText = _localizationService.GetString("Onboarding_StartTraining");
            CanSkip = false; // Never allow skip on final step
            _logger.LogInformation("On last page - NextButtonText set to 'Start Training'");
        }
        else
        {
            NextButtonText = _localizationService.GetString("Onboarding_Next");
            _logger.LogInformation("Standard navigation - NextButtonText set to 'Next'");
        }
    }

    [RelayCommand]
    private async Task Next()
    {
        try
        {
            _logger.LogInformation("Next command executed. CurrentPosition: {CurrentPosition}, TotalPages: {TotalPages}",
                CurrentPosition, OnboardingPages.Count);

            // Defensive check
            if (OnboardingPages.Count == 0)
            {
                _logger.LogError("No onboarding pages loaded! Completing onboarding as fallback.");
                await CompleteOnboarding();
                return;
            }

            // Check if current view is WelcomeChallengeView and delegate to it
            if (CurrentPosition >= 0 && CurrentPosition < OnboardingPages.Count)
            {
                var currentView = OnboardingPages[CurrentPosition];
                if (currentView is WelcomeChallengeView welcomeView)
                {
                    var canAdvance = await welcomeView.HandleNextAsync();
                    if (!canAdvance)
                    {
                        // WelcomeChallengeView handled it internally (showing next panel)
                        return;
                    }
                    // Otherwise fall through to advance to next onboarding step
                }
            }

            if (CurrentPosition < OnboardingPages.Count - 1)
            {
                int newPosition = CurrentPosition + 1;
                _logger.LogInformation("Advancing from position {CurrentPosition} to {NewPosition}. Next view: {ViewType}",
                    CurrentPosition, newPosition,
                    newPosition < OnboardingPages.Count ? OnboardingPages[newPosition].GetType().Name : "None");

                CurrentPosition = newPosition;
            }
            else
            {
                _logger.LogInformation("At final position {CurrentPosition}/{TotalPages}. Completing onboarding.",
                    CurrentPosition, OnboardingPages.Count - 1);
                await CompleteOnboarding();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in Next command. CurrentPosition: {CurrentPosition}, TotalPages: {TotalPages}",
                CurrentPosition, OnboardingPages.Count);

            // Don't complete onboarding on error - let user try again
            throw;
        }
    }

    [RelayCommand]
    private async Task Back()
    {
        // Check if current view is WelcomeChallengeView and delegate to it
        if (CurrentPosition >= 0 && CurrentPosition < OnboardingPages.Count)
        {
            var currentView = OnboardingPages[CurrentPosition];
            if (currentView is WelcomeChallengeView welcomeView)
            {
                var canGoBack = await welcomeView.HandleBackAsync();
                if (!canGoBack)
                {
                    // WelcomeChallengeView handled it internally (showing previous panel)
                    return;
                }
                // Otherwise fall through to go to previous onboarding step
            }
        }

        if (CurrentPosition > 0)
        {
            CurrentPosition--;
        }
    }

    [RelayCommand]
    private async Task Skip()
    {
        // Only allow skip in Phase 2 (Setup)
        if (OnboardingPhaseConfig.IsInTutorialPhase(CurrentPosition))
        {
            // Should not reach here due to CanSkip logic, but add safety check
            _logger.LogWarning("Skip attempted during Phase 1 (Tutorial), but this should not be allowed");
            return;
        }

        // Phase 2 skip options: either skip current step or complete onboarding
        var skipOptions = new List<string>
        {
            _localizationService.GetString("Onboarding_SkipToEnd"),
            _localizationService.GetString("Onboarding_SkipThisStep")
        };

        var action = await Application.Current.MainPage.DisplayActionSheet(
            _localizationService.GetString("Onboarding_SkipOptionsTitle"),
            _localizationService.GetString("Common_Cancel"),
            null,
            skipOptions.ToArray());

        if (action == _localizationService.GetString("Onboarding_SkipToEnd"))
        {
            await CompleteOnboarding();
        }
        else if (action == _localizationService.GetString("Onboarding_SkipThisStep"))
        {
            // Skip to next step in Phase 2
            if (CurrentPosition < OnboardingPages.Count - 1)
            {
                CurrentPosition++;
            }
            else
            {
                await CompleteOnboarding();
            }
        }
    }

    private async Task CompleteOnboarding()
    {
        try
        {
            // Mark onboarding as completed
            Preferences.Set("OnboardingCompleted", "true");
            Preferences.Set("OnboardingVersion", "1.0");
            
            // Navigate to main app by replacing the Window's Page
            var appShell = _serviceProvider.GetRequiredService<AppShell>();
            
            // Get the current window
            var currentWindow = Application.Current?.Windows?.FirstOrDefault();
            if (currentWindow != null)
            {
                // Replace the window's page with the AppShell
                currentWindow.Page = appShell;
            }
            else
            {
                // Fallback: try to set MainPage directly
                Application.Current.MainPage = appShell;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to complete onboarding");
            
            // Show error to user
            var currentPage = Application.Current?.MainPage ?? Application.Current?.Windows?.FirstOrDefault()?.Page;
            if (currentPage != null)
            {
                await currentPage.DisplayAlert(
                    _localizationService.GetString("Common_Error"),
                    _localizationService.GetString("Onboarding_CompletionError"),
                    _localizationService.GetString("Common_OK"));
            }
        }
    }
}