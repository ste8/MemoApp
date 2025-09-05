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
            
            // Add Personalization Screen
            var personalizationView = _serviceProvider.GetRequiredService<PersonalizationView>();
            OnboardingPages.Add(personalizationView);
            
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
        
        // Update button text for last page
        if (CurrentPosition == OnboardingPages.Count - 1)
        {
            NextButtonText = _localizationService.GetString("Onboarding_StartTraining");
            CanSkip = false;
        }
        else
        {
            NextButtonText = _localizationService.GetString("Onboarding_Next");
        }
    }

    [RelayCommand]
    private async Task Next()
    {
        if (CurrentPosition < OnboardingPages.Count - 1)
        {
            CurrentPosition++;
        }
        else
        {
            await CompleteOnboarding();
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
            
            // Navigate to main app
            Application.Current.MainPage = _serviceProvider.GetRequiredService<AppShell>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to complete onboarding");
        }
    }
}