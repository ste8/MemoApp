using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MemoApp.Localization.Services;
using Microsoft.Extensions.Logging;

namespace MemoApp.UI.MauiApp.ViewModels;

public partial class SettingsViewModel : BaseViewModel
{
    private readonly ILocalizationService _localizationService;
    private readonly ILogger<SettingsViewModel> _logger;

    [ObservableProperty]
    private string currentLanguageName = string.Empty;

    public ObservableCollection<string> AvailableLanguages { get; }

    public SettingsViewModel(ILocalizationService localizationService, ILogger<SettingsViewModel> logger)
    {
        _localizationService = localizationService ?? throw new ArgumentNullException(nameof(localizationService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        Title = "Settings"; // Will be localized in the view
        AvailableLanguages = new ObservableCollection<string>();
        
        // Initialize with a fallback language to ensure UI never shows empty
        CurrentLanguageName = "English";
        
        _logger.LogInformation("SettingsViewModel constructor completed");
    }

    [RelayCommand]
    private async Task SelectLanguageAsync()
    {
        try
        {
            var languageNames = AvailableLanguages.ToArray();
            var selectedLanguage = await Shell.Current.DisplayActionSheet(
                _localizationService.GetString("Language_Title"),
                _localizationService.GetString("Common_Cancel"),
                null,
                languageNames);

            if (!string.IsNullOrEmpty(selectedLanguage) && selectedLanguage != _localizationService.GetString("Common_Cancel"))
            {
                _logger.LogInformation("User selected language: {Language}", selectedLanguage);
                
                // Find the culture code for the selected display name
                var selectedCulture = _localizationService.GetAvailableCultures()
                    .FirstOrDefault(c => GetLanguageDisplayName(c) == selectedLanguage);
                
                if (selectedCulture != null)
                {
                    _localizationService.SetCulture(selectedCulture);
                    await _localizationService.SaveLanguagePreferenceAsync();
                    
                    CurrentLanguageName = selectedLanguage;
                    
                    _logger.LogInformation("Language changed successfully to: {Language}", selectedLanguage);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error selecting language");
        }
    }

    public async Task InitializeAsync()
    {
        try
        {
            _logger.LogInformation("Starting SettingsViewModel initialization");
            
            // Load saved language preference first
            await _localizationService.LoadSavedLanguageAsync();
            
            // Load language options and set current language
            LoadLanguageOptions();
            
            _logger.LogInformation("Settings initialized successfully with language: {Language}", CurrentLanguageName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing settings");
            // Ensure we have a fallback language even if initialization fails
            if (string.IsNullOrEmpty(CurrentLanguageName))
            {
                CurrentLanguageName = "English";
                if (AvailableLanguages.Count == 0)
                {
                    AvailableLanguages.Add("English");
                    AvailableLanguages.Add("Italiano");
                }
            }
        }
    }

    private void LoadLanguageOptions()
    {
        try
        {
            AvailableLanguages.Clear();
            
            // Add available languages based on supported cultures
            foreach (var culture in _localizationService.GetAvailableCultures())
            {
                var displayName = GetLanguageDisplayName(culture);
                AvailableLanguages.Add(displayName);
            }
            
            // Set current language display name
            CurrentLanguageName = GetLanguageDisplayName(_localizationService.CurrentCulture);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading language options");
            // Add fallback options
            AvailableLanguages.Add("English");
            AvailableLanguages.Add("Italiano");
            CurrentLanguageName = "English";
        }
    }

    private string GetLanguageDisplayName(CultureInfo culture)
    {
        try
        {
            return culture.TwoLetterISOLanguageName switch
            {
                "en" => _localizationService.GetString("Language_English"),
                "it" => _localizationService.GetString("Language_Italian"),
                _ => culture.DisplayName
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting language display name for culture: {Culture}", culture?.Name);
            // Return fallback name
            return culture?.TwoLetterISOLanguageName switch
            {
                "en" => "English",
                "it" => "Italiano",
                _ => culture?.DisplayName ?? "Unknown"
            };
        }
    }
}