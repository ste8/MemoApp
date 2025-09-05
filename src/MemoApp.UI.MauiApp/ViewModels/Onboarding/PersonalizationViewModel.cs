using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MemoApp.Localization.Services;
using Microsoft.Extensions.Logging;

namespace MemoApp.UI.MauiApp.ViewModels.Onboarding;

public partial class PersonalizationViewModel : BaseViewModel
{
    private readonly ILocalizationService _localizationService;
    private readonly ILogger<PersonalizationViewModel> _logger;

    [ObservableProperty]
    private string selectedLanguage = "en";

    [ObservableProperty]
    private string selectedLevel = "Beginner";

    [ObservableProperty]
    private bool enableReminders = false;

    // Language button colors
    [ObservableProperty]
    private Color englishButtonColor;

    [ObservableProperty]
    private Color englishTextColor;

    [ObservableProperty]
    private Color italianButtonColor;

    [ObservableProperty]
    private Color italianTextColor;

    // Level button colors
    [ObservableProperty]
    private Color beginnerButtonColor;

    [ObservableProperty]
    private Color beginnerTextColor;

    [ObservableProperty]
    private Color intermediateButtonColor;

    [ObservableProperty]
    private Color intermediateTextColor;

    [ObservableProperty]
    private Color expertButtonColor;

    [ObservableProperty]
    private Color expertTextColor;

    public PersonalizationViewModel(
        ILocalizationService localizationService,
        ILogger<PersonalizationViewModel> logger)
    {
        _localizationService = localizationService;
        _logger = logger;
        
        Title = _localizationService.GetString("Onboarding_Personalization_Title");
        
        // Set initial language based on current culture
        SelectedLanguage = _localizationService.CurrentCulture.TwoLetterISOLanguageName;
        UpdateLanguageButtonColors();
        UpdateLevelButtonColors();
    }

    [RelayCommand]
    private void SelectLanguage(string language)
    {
        SelectedLanguage = language;
        UpdateLanguageButtonColors();
        
        // Apply the language change
        var culture = language == "it" ? "it-IT" : "en-US";
        _localizationService.SetCulture(culture);
    }

    [RelayCommand]
    private void SelectLevel(string level)
    {
        SelectedLevel = level;
        UpdateLevelButtonColors();
        
        // Save the preference
        Preferences.Set("UserLevel", level);
    }

    partial void OnSelectedLanguageChanged(string value)
    {
        UpdateLanguageButtonColors();
    }

    partial void OnSelectedLevelChanged(string value)
    {
        UpdateLevelButtonColors();
    }

    private void UpdateLanguageButtonColors()
    {
        var primaryColor = Application.Current.Resources["Primary"] as Color;
        var grayColor = Application.Current.Resources["Gray100"] as Color;
        
        if (SelectedLanguage == "en")
        {
            EnglishButtonColor = primaryColor;
            EnglishTextColor = Colors.White;
            ItalianButtonColor = grayColor;
            ItalianTextColor = Colors.Black;
        }
        else
        {
            EnglishButtonColor = grayColor;
            EnglishTextColor = Colors.Black;
            ItalianButtonColor = primaryColor;
            ItalianTextColor = Colors.White;
        }
    }

    private void UpdateLevelButtonColors()
    {
        var primaryColor = Application.Current.Resources["Primary"] as Color;
        var grayColor = Application.Current.Resources["Gray100"] as Color;
        
        // Reset all
        BeginnerButtonColor = grayColor;
        BeginnerTextColor = Colors.Black;
        IntermediateButtonColor = grayColor;
        IntermediateTextColor = Colors.Black;
        ExpertButtonColor = grayColor;
        ExpertTextColor = Colors.Black;
        
        // Highlight selected
        switch (SelectedLevel)
        {
            case "Beginner":
                BeginnerButtonColor = primaryColor;
                BeginnerTextColor = Colors.White;
                break;
            case "Intermediate":
                IntermediateButtonColor = primaryColor;
                IntermediateTextColor = Colors.White;
                break;
            case "Expert":
                ExpertButtonColor = primaryColor;
                ExpertTextColor = Colors.White;
                break;
        }
    }

    partial void OnEnableRemindersChanged(bool value)
    {
        // Save reminder preference
        Preferences.Set("EnableDailyReminders", value);
        
        if (value)
        {
            // TODO: Schedule daily notifications
            _logger.LogInformation("Daily reminders enabled");
        }
        else
        {
            // TODO: Cancel scheduled notifications
            _logger.LogInformation("Daily reminders disabled");
        }
    }
}