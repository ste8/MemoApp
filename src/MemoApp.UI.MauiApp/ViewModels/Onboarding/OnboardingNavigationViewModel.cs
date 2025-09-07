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
            
            // Create onboarding pages
            OnboardingPages.Clear();
            
            // Add Welcome Screen
            var welcomeView = _serviceProvider.GetRequiredService<WelcomeView>();
            OnboardingPages.Add(welcomeView);
            
            // Add Challenge Screen
            var challengeView = _serviceProvider.GetRequiredService<ChallengeView>();
            OnboardingPages.Add(challengeView);
            
            // Add Major System Intro
            var majorSystemView = _serviceProvider.GetRequiredService<MajorSystemIntroView>();
            OnboardingPages.Add(majorSystemView);
            
            // Add Example Screen
            var exampleView = _serviceProvider.GetRequiredService<ExampleView>();
            OnboardingPages.Add(exampleView);
            
            // Add Practice Screen
            var practiceView = _serviceProvider.GetRequiredService<InteractivePracticeView>();
            OnboardingPages.Add(practiceView);
            
            // Add Navigation Guide
            var navigationView = _serviceProvider.GetRequiredService<NavigationGuideView>();
            OnboardingPages.Add(navigationView);
            
            // Add Pack Download Screen (Step 7)
            _logger.LogInformation("Creating PackDownloadOnboardingView...");
            var packDownloadView = _serviceProvider.GetRequiredService<PackDownloadOnboardingView>();
            if (packDownloadView == null)
            {
                _logger.LogError("PackDownloadOnboardingView is null!");
                throw new InvalidOperationException("Failed to create PackDownloadOnboardingView");
            }
            OnboardingPages.Add(packDownloadView);
            _logger.LogInformation("PackDownloadOnboardingView added successfully at index {Index}", OnboardingPages.Count - 1);
            
            // Add Personalization Screen
            _logger.LogInformation("Creating PersonalizationView...");
            var personalizationView = _serviceProvider.GetRequiredService<PersonalizationView>();
            if (personalizationView == null)
            {
                _logger.LogError("PersonalizationView is null!");
                throw new InvalidOperationException("Failed to create PersonalizationView");
            }
            OnboardingPages.Add(personalizationView);
            _logger.LogInformation("PersonalizationView added successfully at index {Index}", OnboardingPages.Count - 1);
            
            _logger.LogInformation("Total onboarding pages added: {PageCount}", OnboardingPages.Count);
            
            // Validate that all expected views were added
            if (OnboardingPages.Count != 8)
            {
                _logger.LogError("Expected 8 onboarding pages but got {ActualCount}!", OnboardingPages.Count);
                throw new InvalidOperationException($"Expected 8 onboarding pages but got {OnboardingPages.Count}");
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
        
        // Update button text for last page
        if (CurrentPosition == OnboardingPages.Count - 1)
        {
            NextButtonText = _localizationService.GetString("Onboarding_StartTraining");
            CanSkip = false;
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
        var confirm = await Application.Current.MainPage.DisplayAlert(
            _localizationService.GetString("Onboarding_SkipTitle"),
            _localizationService.GetString("Onboarding_SkipMessage"),
            _localizationService.GetString("Common_Yes"),
            _localizationService.GetString("Common_No"));

        if (confirm)
        {
            await CompleteOnboarding();
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