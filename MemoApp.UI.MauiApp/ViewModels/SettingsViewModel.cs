using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MemoApp.Core.MajorSystem;
using MemoApp.Core.Packs;
using MemoApp.Core.Services.Packs;
using MemoApp.Core.Services.Analytics;
using MemoApp.Localization.Services;
using MemoApp.UI.MauiApp.Services;
using MemoApp.UI.MauiApp.Views;
using Microsoft.Extensions.Logging;

namespace MemoApp.UI.MauiApp.ViewModels;

public partial class SettingsViewModel : BaseViewModel
{
    private readonly ILocalizationService _localizationService;
    private readonly IPackManagerService _packManager;
    private readonly IPackDownloadService _downloadService;
    private readonly IPackUpdateBackgroundService? _updateService;
    private readonly IAnalyticsService _analyticsService;
    private readonly ILogger<SettingsViewModel> _logger;

    [ObservableProperty]
    private string currentLanguageName = string.Empty;

    [ObservableProperty]
    private string currentNumberFormatName = string.Empty;

    [ObservableProperty]
    private string currentPackName = string.Empty;

    [ObservableProperty]
    private bool hasPacksDownloaded;

    [ObservableProperty]
    private ObservableCollection<Pack> downloadedPacks = new();

    public ObservableCollection<string> AvailableLanguages { get; }
    public ObservableCollection<string> AvailableNumberFormats { get; }

    public SettingsViewModel(
        ILocalizationService localizationService, 
        IPackManagerService packManager,
        IPackDownloadService downloadService,
        IPackUpdateBackgroundService? updateService,
        IAnalyticsService analyticsService,
        ILogger<SettingsViewModel> logger)
    {
        _localizationService = localizationService ?? throw new ArgumentNullException(nameof(localizationService));
        _packManager = packManager ?? throw new ArgumentNullException(nameof(packManager));
        _downloadService = downloadService ?? throw new ArgumentNullException(nameof(downloadService));
        _updateService = updateService;
        _analyticsService = analyticsService ?? throw new ArgumentNullException(nameof(analyticsService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        Title = _localizationService.GetString("Settings_Title");
        AvailableLanguages = new ObservableCollection<string>();
        AvailableNumberFormats = new ObservableCollection<string>();
        
        // Initialize with fallback values to ensure UI never shows empty
        CurrentLanguageName = _localizationService.GetString("Language_English");
        CurrentNumberFormatName = _localizationService.GetString("NumberFormat_Padded");
        CurrentPackName = _localizationService.GetString("Settings_NoPacksDownloaded");
        
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
                    
                    // Track language change analytics
                    await _analyticsService.TrackEventAsync(AnalyticsEvents.LanguageChanged, new Dictionary<string, object>
                    {
                        [AnalyticsParameters.LanguageCode] = selectedCulture.TwoLetterISOLanguageName,
                        [AnalyticsParameters.LanguageName] = selectedLanguage
                    });
                    
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
                    
                    // Track number format change analytics
                    await _analyticsService.TrackEventAsync(AnalyticsEvents.NumberFormatChanged, new Dictionary<string, object>
                    {
                        [AnalyticsParameters.NumberFormatName] = selectedFormat,
                        [AnalyticsParameters.NumberFormat] = numberFormat.Value.ToString()
                    });
                    
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

    [RelayCommand]
    private async Task SelectPackAsync()
    {
        try
        {
            if (DownloadedPacks.Count == 0)
            {
                await Shell.Current.DisplayAlert(
                    _localizationService.GetString("Settings_SelectPack"),
                    _localizationService.GetString("Settings_NoPacksDownloaded"),
                    _localizationService.GetString("Common_OK"));
                return;
            }

            var packNames = DownloadedPacks.Select(p => GetPackDisplayName(p)).ToArray();
            var selectedPack = await Shell.Current.DisplayActionSheet(
                _localizationService.GetString("Settings_SelectPack"),
                _localizationService.GetString("Common_Cancel"),
                null,
                packNames);

            if (!string.IsNullOrEmpty(selectedPack) && selectedPack != _localizationService.GetString("Common_Cancel"))
            {
                var pack = DownloadedPacks.FirstOrDefault(p => GetPackDisplayName(p) == selectedPack);
                if (pack != null)
                {
                    await _packManager.SetCurrentPackAsync(pack.Id);
                    
                    // Track pack selection analytics
                    await _analyticsService.TrackEventAsync(AnalyticsEvents.PackSelected, new Dictionary<string, object>
                    {
                        [AnalyticsParameters.PackName] = pack.InternationalName,
                        [AnalyticsParameters.PackLanguage] = pack.LanguageCode,
                        [AnalyticsParameters.PackId] = pack.Id
                    });
                    
                    CurrentPackName = GetPackDisplayName(pack);
                    _logger.LogInformation("Pack changed to: {PackId}", pack.Id);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error selecting pack");
        }
    }

    [RelayCommand]
    private async Task DownloadPacksAsync()
    {
        try
        {
            await Shell.Current.GoToAsync(nameof(PackDownloadPage));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error navigating to pack download page");
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
            await LoadPacksAsync();
            
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

    private async Task LoadPacksAsync()
    {
        try
        {
            DownloadedPacks.Clear();
            
            var packs = await _packManager.GetDownloadedPacksAsync();
            foreach (var pack in packs)
            {
                DownloadedPacks.Add(pack);
            }

            HasPacksDownloaded = DownloadedPacks.Count > 0;
            
            var currentPack = await _packManager.GetCurrentPackAsync();
            if (currentPack != null)
            {
                CurrentPackName = GetPackDisplayName(currentPack);
            }
            else
            {
                CurrentPackName = _localizationService.GetString("Settings_NoPacksDownloaded");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading packs");
            CurrentPackName = _localizationService.GetString("Settings_NoPacksDownloaded");
        }
    }

    private string GetPackDisplayName(Pack pack)
    {
        var currentLanguage = _localizationService.GetCurrentLanguage();
        var useNative = currentLanguage == "it" && pack.LanguageCode == "it";
        return pack.GetDisplayName(useNative);
    }
}