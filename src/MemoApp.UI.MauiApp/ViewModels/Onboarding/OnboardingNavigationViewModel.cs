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
    private bool canSkip = true;

    [ObservableProperty]
    private bool canGoBack;

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
            
            // Create streamlined 5-step onboarding pages
            OnboardingPages.Clear();
            
            // Step 1: Welcome + Challenge (merged)
            var welcomeChallengeView = _serviceProvider.GetRequiredService<WelcomeChallengeView>();
            OnboardingPages.Add(welcomeChallengeView);
            
            // Step 2: Major System Intro + Example (combined concept)
            var majorSystemView = _serviceProvider.GetRequiredService<MajorSystemIntroView>();
            OnboardingPages.Add(majorSystemView);
            
            // Step 3: Interactive Practice
            var practiceView = _serviceProvider.GetRequiredService<InteractivePracticeView>();
            OnboardingPages.Add(practiceView);
            
            // Step 4: Pack Download (moved earlier, better timing)
            _logger.LogInformation("Creating PackDownloadOnboardingView...");
            var packDownloadView = _serviceProvider.GetRequiredService<PackDownloadOnboardingView>();
            if (packDownloadView == null)
            {
                _logger.LogError("PackDownloadOnboardingView is null!");
                throw new InvalidOperationException("Failed to create PackDownloadOnboardingView");
            }
            OnboardingPages.Add(packDownloadView);
            _logger.LogInformation("PackDownloadOnboardingView added successfully at index {Index}", OnboardingPages.Count - 1);
            
            // Step 5: Navigation Guide + Personalization (final setup)
            var navigationView = _serviceProvider.GetRequiredService<NavigationGuideView>();
            OnboardingPages.Add(navigationView);
            
            _logger.LogInformation("Total onboarding pages added: {PageCount}", OnboardingPages.Count);
            
            // Validate that all expected views were added
            if (OnboardingPages.Count != 5)
            {
                _logger.LogError("Expected 5 onboarding pages but got {ActualCount}!", OnboardingPages.Count);
                throw new InvalidOperationException($"Expected 5 onboarding pages but got {OnboardingPages.Count}");
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
        CanSkip = CurrentPosition < OnboardingPages.Count - 1;
        
        _logger.LogInformation("UpdateNavigationState - Position: {CurrentPosition}/{TotalPages}, CanGoBack: {CanGoBack}, CanSkip: {CanSkip}",
            CurrentPosition, OnboardingPages.Count - 1, CanGoBack, CanSkip);
        
        // Update progress text
        ProgressText = $"{CurrentPosition + 1} of {OnboardingPages.Count}";
        
        // Update button text for last page
        if (CurrentPosition == OnboardingPages.Count - 1)
        {
            NextButtonText = _localizationService.GetString("Onboarding_StartTraining");
            CanSkip = false; // Hide skip on final step for clear completion flow
            _logger.LogInformation("On last page - NextButtonText set to 'Start Training'");
        }
        else
        {
            NextButtonText = _localizationService.GetString("Onboarding_Next");
            _logger.LogInformation("Not on last page - NextButtonText set to 'Next'");
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
    private void Back()
    {
        if (CurrentPosition > 0)
        {
            CurrentPosition--;
        }
    }

    [RelayCommand]
    private async Task Skip()
    {
        // Provide contextual skip options based on current phase
        var skipOptions = GetContextualSkipOptions();
        
        var action = await Application.Current.MainPage.DisplayActionSheet(
            _localizationService.GetString("Onboarding_SkipOptionsTitle"),
            _localizationService.GetString("Common_Cancel"),
            null,
            skipOptions.ToArray());

        if (action == _localizationService.GetString("Onboarding_SkipToEnd"))
        {
            await CompleteOnboarding();
        }
        else if (action == _localizationService.GetString("Onboarding_SkipToPractice") && CurrentPosition < 2)
        {
            CurrentPosition = 2; // Jump to practice
        }
        else if (action == _localizationService.GetString("Onboarding_SkipToSetup") && CurrentPosition < 3)
        {
            CurrentPosition = 3; // Jump to setup phase
        }
    }
    
    private List<string> GetContextualSkipOptions()
    {
        var options = new List<string>();
        
        // Always allow skipping to the end
        options.Add(_localizationService.GetString("Onboarding_SkipToEnd"));
        
        // Phase-specific skip options
        switch (CurrentPosition)
        {
            case 0: // Welcome+Challenge - offer skip to practice or setup
                options.Add(_localizationService.GetString("Onboarding_SkipToPractice"));
                options.Add(_localizationService.GetString("Onboarding_SkipToSetup"));
                break;
            case 1: // Major System - offer skip to practice or setup  
                options.Add(_localizationService.GetString("Onboarding_SkipToPractice"));
                options.Add(_localizationService.GetString("Onboarding_SkipToSetup"));
                break;
            case 2: // Practice - offer skip to setup
                options.Add(_localizationService.GetString("Onboarding_SkipToSetup"));
                break;
            default:
                // For later phases, only offer complete skip
                break;
        }
        
        return options;
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