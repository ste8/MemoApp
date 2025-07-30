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

    [ObservableProperty]
    private string currentNumberFormatName = string.Empty;

    public ObservableCollection<string> AvailableLanguages { get; }
    public ObservableCollection<string> AvailableNumberFormats { get; }

    public SettingsViewModel(ILocalizationService localizationService, ILogger<SettingsViewModel> logger)
    {
        _localizationService = localizationService ?? throw new ArgumentNullException(nameof(localizationService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        Title = _localizationService.GetString("Settings_Title");
        AvailableLanguages = new ObservableCollection<string>();
        AvailableNumberFormats = new ObservableCollection<string>();
        
        // Initialize with fallback values to ensure UI never shows empty
        CurrentLanguageName = _localizationService.GetString("Language_English");
        CurrentNumberFormatName = _localizationService.GetString("NumberFormat_Padded");
        
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

    [RelayCommand]
    private async Task SelectNumberFormatAsync()
    {
        try
        {
            var formatNames = AvailableNumberFormats.ToArray();
            var selectedFormat = await Shell.Current.DisplayActionSheet(
                _localizationService.GetString("NumberFormat_Title"),
                _localizationService.GetString("Common_Cancel"),
                null,
                formatNames);

            if (!string.IsNullOrEmpty(selectedFormat) && selectedFormat != _localizationService.GetString("Common_Cancel"))
            {
                _logger.LogInformation("User selected number format: {Format}", selectedFormat);
                
                // Convert display name to NumberFormat enum
                var numberFormat = GetNumberFormatFromDisplayName(selectedFormat);
                
                if (numberFormat.HasValue)
                {
                    _localizationService.SetNumberFormat(numberFormat.Value);
                    await _localizationService.SaveNumberFormatPreferenceAsync();
                    
                    CurrentNumberFormatName = selectedFormat;
                    
                    _logger.LogInformation("Number format changed successfully to: {Format}", selectedFormat);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error selecting number format");
        }
    }

    public async Task InitializeAsync()
    {
        try
        {
            _logger.LogInformation("Starting SettingsViewModel initialization");
            
            // Load saved preferences first
            await _localizationService.LoadSavedLanguageAsync();
            await _localizationService.LoadSavedNumberFormatAsync();
            
            // Load options and set current values
            LoadLanguageOptions();
            LoadNumberFormatOptions();
            
            _logger.LogInformation("Settings initialized successfully with language: {Language}", CurrentLanguageName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing settings");
            // Ensure we have fallback values even if initialization fails
            if (string.IsNullOrEmpty(CurrentLanguageName))
            {
                CurrentLanguageName = _localizationService.GetString("Language_English");
                if (AvailableLanguages.Count == 0)
                {
                    AvailableLanguages.Add(_localizationService.GetString("Language_English"));
                    AvailableLanguages.Add(_localizationService.GetString("Language_Italian"));
                }
            }
            if (string.IsNullOrEmpty(CurrentNumberFormatName))
            {
                CurrentNumberFormatName = _localizationService.GetString("NumberFormat_Padded");
                if (AvailableNumberFormats.Count == 0)
                {
                    AvailableNumberFormats.Add(_localizationService.GetString("NumberFormat_Padded"));
                    AvailableNumberFormats.Add(_localizationService.GetString("NumberFormat_Natural"));
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
            AvailableLanguages.Add(_localizationService.GetString("Language_English"));
            AvailableLanguages.Add(_localizationService.GetString("Language_Italian"));
            CurrentLanguageName = _localizationService.GetString("Language_English");
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
                "en" => _localizationService.GetString("Language_English"),
                "it" => _localizationService.GetString("Language_Italian"),
                _ => culture?.DisplayName ?? "Unknown"
            };
        }
    }

    private void LoadNumberFormatOptions()
    {
        try
        {
            AvailableNumberFormats.Clear();
            
            // Add available number format options
            AvailableNumberFormats.Add(_localizationService.GetString("NumberFormat_Padded"));
            AvailableNumberFormats.Add(_localizationService.GetString("NumberFormat_Natural"));
            
            // Set current number format display name
            CurrentNumberFormatName = GetNumberFormatDisplayName(_localizationService.CurrentNumberFormat);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading number format options");
            // Add fallback options
            AvailableNumberFormats.Add(_localizationService.GetString("NumberFormat_Padded"));
            AvailableNumberFormats.Add(_localizationService.GetString("NumberFormat_Natural"));
            CurrentNumberFormatName = _localizationService.GetString("NumberFormat_Padded");
        }
    }

    private string GetNumberFormatDisplayName(NumberFormat numberFormat)
    {
        try
        {
            return numberFormat switch
            {
                NumberFormat.Padded => _localizationService.GetString("NumberFormat_Padded"),
                NumberFormat.Natural => _localizationService.GetString("NumberFormat_Natural"),
                _ => _localizationService.GetString("NumberFormat_Padded")
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting number format display name for format: {Format}", numberFormat);
            return _localizationService.GetString("NumberFormat_Padded");
        }
    }

    private NumberFormat? GetNumberFormatFromDisplayName(string displayName)
    {
        try
        {
            if (displayName == _localizationService.GetString("NumberFormat_Padded"))
                return NumberFormat.Padded;
            if (displayName == _localizationService.GetString("NumberFormat_Natural"))
                return NumberFormat.Natural;
            
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting number format from display name: {DisplayName}", displayName);
            return null;
        }
    }
}