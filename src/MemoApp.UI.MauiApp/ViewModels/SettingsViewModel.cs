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
    private readonly IAppInfoService _appInfoService;
    private readonly IUserNotificationService _userNotificationService;
    private readonly ILogger<SettingsViewModel> _logger;

    [ObservableProperty]
    private string currentLanguageName = string.Empty;

    [ObservableProperty]
    private string currentNumberFormatName = string.Empty;

    [ObservableProperty]
    private string currentNumberFormatDescription = string.Empty;

    [ObservableProperty]
    private string currentNumberFormatComparisonSymbol = string.Empty;

    [ObservableProperty]
    private string currentPackName = string.Empty;

    [ObservableProperty]
    private bool hasPacksDownloaded;

    [ObservableProperty]
    private ObservableCollection<Pack> downloadedPacks = new();

    // Language switching loading state properties
    [ObservableProperty]
    private bool isLanguageSwitching;

    [ObservableProperty]
    private bool isLanguageSwitchEnabled = true;

    [ObservableProperty]
    private string languageSwitchMessage = string.Empty;

    // App Info properties
    [ObservableProperty]
    private string appName = string.Empty;

    [ObservableProperty]
    private string version = string.Empty;


    public ObservableCollection<string> AvailableLanguages { get; }
    public ObservableCollection<string> AvailableNumberFormats { get; }

    public SettingsViewModel(
        ILocalizationService localizationService, 
        IPackManagerService packManager,
        IPackDownloadService downloadService,
        IPackUpdateBackgroundService? updateService,
        IAnalyticsService analyticsService,
        IAppInfoService appInfoService,
        IUserNotificationService userNotificationService,
        ILogger<SettingsViewModel> logger)
    {
        _localizationService = localizationService ?? throw new ArgumentNullException(nameof(localizationService));
        _packManager = packManager ?? throw new ArgumentNullException(nameof(packManager));
        _downloadService = downloadService ?? throw new ArgumentNullException(nameof(downloadService));
        _updateService = updateService;
        _analyticsService = analyticsService ?? throw new ArgumentNullException(nameof(analyticsService));
        _appInfoService = appInfoService ?? throw new ArgumentNullException(nameof(appInfoService));
        _userNotificationService = userNotificationService ?? throw new ArgumentNullException(nameof(userNotificationService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        Title = _localizationService.GetString("Settings_Title");
        AvailableLanguages = new ObservableCollection<string>();
        AvailableNumberFormats = new ObservableCollection<string>();
        
        // Initialize with fallback values to ensure UI never shows empty
        CurrentLanguageName = _localizationService.GetString("Language_English");
        CurrentNumberFormatName = _localizationService.GetString("NumberFormat_Padded");
        CurrentNumberFormatDescription = _localizationService.GetString("OnboardingNumberFormat_PaddedDescription");
        CurrentNumberFormatComparisonSymbol = GetNumberFormatComparisonSymbol(NumberFormat.Padded);
        CurrentPackName = _localizationService.GetString("Settings_NoPacksDownloaded");
        
        // Initialize app info properties
        InitializeAppInfo();
        
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

                // Start loading state immediately
                IsLanguageSwitching = true;
                IsLanguageSwitchEnabled = false;
                LanguageSwitchMessage = string.Format(
                    _localizationService.GetString("Language_SwitchingTo"),
                    selectedLanguage);

                // Brief delay to show loading state (allows UI to update)
                await Task.Delay(100);

                // Find the culture code for the selected display name
                var selectedCulture = _localizationService.GetAvailableCultures()
                    .FirstOrDefault(c => GetLanguageDisplayName(c) == selectedLanguage);

                if (selectedCulture != null)
                {
                    _localizationService.SetCulture(selectedCulture);
                    await _localizationService.SaveLanguagePreferenceAsync();

                    // Update current display name immediately
                    CurrentLanguageName = selectedLanguage;

                    // Allow time for UI to update (resource loading and XAML binding refresh)
                    await Task.Delay(300);

                    // Track language change analytics
                    await _analyticsService.TrackEventAsync(AnalyticsEvents.LanguageChanged, new Dictionary<string, object>
                    {
                        [AnalyticsParameters.LanguageCode] = selectedCulture.TwoLetterISOLanguageName,
                        [AnalyticsParameters.LanguageName] = selectedLanguage
                    });

                    // Announce to screen readers for accessibility
                    try
                    {
                        SemanticScreenReader.Announce(string.Format(
                            _localizationService.GetString("Accessibility_LanguageChanged"),
                            selectedLanguage));
                    }
                    catch (Exception screenReaderEx)
                    {
                        // Screen reader announcement is non-critical, just log and continue
                        _logger.LogWarning(screenReaderEx, "Failed to announce language change to screen reader");
                    }

                    _logger.LogInformation("Language changed successfully to: {Language}", selectedLanguage);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error selecting language");
            await _userNotificationService.ShowErrorToastAsync(
                _localizationService.GetString("Settings_SaveFailed"));
        }
        finally
        {
            // Reset loading state
            IsLanguageSwitching = false;
            IsLanguageSwitchEnabled = true;
            LanguageSwitchMessage = string.Empty;
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
                    CurrentNumberFormatDescription = GetNumberFormatDescription(numberFormat.Value);
                    CurrentNumberFormatComparisonSymbol = GetNumberFormatComparisonSymbol(numberFormat.Value);

                    _logger.LogInformation("Number format changed successfully to: {Format}", selectedFormat);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error selecting number format");
            await _userNotificationService.ShowErrorToastAsync(
                _localizationService.GetString("Settings_SaveFailed"));
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
            await _userNotificationService.ShowErrorToastAsync(
                _localizationService.GetString("Settings_SaveFailed"));
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
            await _userNotificationService.ShowErrorToastAsync(
                _localizationService.GetString("Navigation_Failed"));
        }
    }

    [RelayCommand]
    private async Task SendFeedbackAsync()
    {
        try
        {
            await Shell.Current.GoToAsync(nameof(FeedbackPage));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error navigating to feedback page");
            await _userNotificationService.ShowErrorToastAsync(
                _localizationService.GetString("Navigation_Failed"));
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
            
            // Notify user about the failure
            await _userNotificationService.ShowWarningToastAsync(
                _localizationService.GetString("Settings_LoadFailed_UsingDefaults"));
            
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
                CurrentNumberFormatDescription = _localizationService.GetString("OnboardingNumberFormat_PaddedDescription");
                CurrentNumberFormatComparisonSymbol = GetNumberFormatComparisonSymbol(NumberFormat.Padded);
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

            // Set current number format display name, description, and comparison symbol
            CurrentNumberFormatName = GetNumberFormatDisplayName(_localizationService.CurrentNumberFormat);
            CurrentNumberFormatDescription = GetNumberFormatDescription(_localizationService.CurrentNumberFormat);
            CurrentNumberFormatComparisonSymbol = GetNumberFormatComparisonSymbol(_localizationService.CurrentNumberFormat);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading number format options");
            // Add fallback options
            AvailableNumberFormats.Add(_localizationService.GetString("NumberFormat_Padded"));
            AvailableNumberFormats.Add(_localizationService.GetString("NumberFormat_Natural"));
            CurrentNumberFormatName = _localizationService.GetString("NumberFormat_Padded");
            CurrentNumberFormatDescription = _localizationService.GetString("OnboardingNumberFormat_PaddedDescription");
            CurrentNumberFormatComparisonSymbol = GetNumberFormatComparisonSymbol(NumberFormat.Padded);
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

    private string GetNumberFormatDescription(NumberFormat numberFormat)
    {
        try
        {
            return numberFormat switch
            {
                NumberFormat.Padded => _localizationService.GetString("OnboardingNumberFormat_PaddedDescription"),
                NumberFormat.Natural => _localizationService.GetString("OnboardingNumberFormat_NaturalDescription"),
                _ => _localizationService.GetString("OnboardingNumberFormat_PaddedDescription")
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting number format description for format: {Format}", numberFormat);
            return _localizationService.GetString("OnboardingNumberFormat_PaddedDescription");
        }
    }

    private string GetNumberFormatComparisonSymbol(NumberFormat numberFormat)
    {
        try
        {
            return numberFormat switch
            {
                NumberFormat.Padded => "≠", // Zero counts: 01 ≠ 1
                NumberFormat.Natural => "=", // Zero doesn't count: 01 = 1
                _ => "≠"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting number format comparison symbol for format: {Format}", numberFormat);
            return "≠";
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

    private void InitializeAppInfo()
    {
        try
        {
            var appDetails = _appInfoService.GetAppDetails();
            
            AppName = appDetails.AppName;
            Version = appDetails.Version;
            
            _logger.LogDebug("App info initialized: {AppName} v{Version}", AppName, Version);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing app info");
            
            // Fallback values if app info fails to load
            AppName = "MemoRaise";
            Version = "Unknown";
        }
    }
}