using System.Globalization;
using System.Resources;
using MemoApp.Localization.Resources;

namespace MemoApp.Localization.Services;

/// <summary>
/// Default implementation of ILocalizationService using RESX resources.
/// This service works for both MAUI and Blazor applications.
/// </summary>
public class LocalizationService : ILocalizationService
{
    private readonly ResourceManager _resourceManager;
    private CultureInfo _currentCulture;

    /// <summary>
    /// Supported cultures by the application.
    /// </summary>
    private static readonly CultureInfo[] SupportedCultures = 
    [
        new CultureInfo("en"), // English (default)
        new CultureInfo("it")  // Italian
    ];

    public LocalizationService()
    {
        _resourceManager = AppResources.ResourceManager;
        _currentCulture = CultureInfo.CurrentUICulture;
        
        // Ensure the current culture is supported, fallback to English
        if (!SupportedCultures.Any(c => c.TwoLetterISOLanguageName == _currentCulture.TwoLetterISOLanguageName))
        {
            _currentCulture = SupportedCultures[0]; // English
        }
    }

    public CultureInfo CurrentCulture => _currentCulture;

    public event EventHandler<CultureInfo>? CultureChanged;

    public string GetString(string key)
    {
        try
        {
            var value = _resourceManager.GetString(key, _currentCulture);
            return value ?? key; // Return key if translation not found
        }
        catch
        {
            return key; // Return key if any error occurs
        }
    }

    public string GetString(string key, params object[] args)
    {
        try
        {
            var format = GetString(key);
            return string.Format(_currentCulture, format, args);
        }
        catch
        {
            return key; // Return key if formatting fails
        }
    }

    public void SetCulture(CultureInfo culture)
    {
        if (culture == null)
            throw new ArgumentNullException(nameof(culture));

        // Ensure the culture is supported
        var supportedCulture = SupportedCultures.FirstOrDefault(c => 
            c.TwoLetterISOLanguageName == culture.TwoLetterISOLanguageName);
        
        if (supportedCulture == null)
        {
            supportedCulture = SupportedCultures[0]; // Fallback to English
        }

        if (_currentCulture.TwoLetterISOLanguageName != supportedCulture.TwoLetterISOLanguageName)
        {
            _currentCulture = supportedCulture;
            
            // Set the thread culture for proper formatting
            CultureInfo.CurrentCulture = _currentCulture;
            CultureInfo.CurrentUICulture = _currentCulture;
            
            CultureChanged?.Invoke(this, _currentCulture);
        }
    }

    public void SetCulture(string cultureName)
    {
        if (string.IsNullOrWhiteSpace(cultureName))
            throw new ArgumentException("Culture name cannot be null or empty", nameof(cultureName));

        try
        {
            var culture = new CultureInfo(cultureName);
            SetCulture(culture);
        }
        catch (CultureNotFoundException)
        {
            // Invalid culture name, fallback to English
            SetCulture(SupportedCultures[0]);
        }
    }

    public IEnumerable<CultureInfo> GetAvailableCultures()
    {
        return SupportedCultures.AsEnumerable();
    }

    private const string LanguagePreferenceKey = "app_language_preference";

    public async Task LoadSavedLanguageAsync()
    {
        await Task.Run(() =>
        {
            try
            {
                // Try to get saved language preference
                // For now, we'll use a simple approach that works across platforms
                // In a real MAUI app, you'd use Microsoft.Maui.Essentials.Preferences
                var savedLanguage = GetStoredLanguagePreference();
                
                if (!string.IsNullOrEmpty(savedLanguage))
                {
                    SetCulture(savedLanguage);
                }
            }
            catch
            {
                // If loading fails, use the current culture (already set in constructor)
            }
        });
    }

    public async Task SaveLanguagePreferenceAsync()
    {
        await Task.Run(() =>
        {
            try
            {
                // Save current culture preference
                SetStoredLanguagePreference(_currentCulture.TwoLetterISOLanguageName);
            }
            catch
            {
                // Ignore save errors - not critical
            }
        });
    }

    /// <summary>
    /// Gets the stored language preference. Override this method in platform-specific implementations.
    /// </summary>
    protected virtual string? GetStoredLanguagePreference()
    {
        // Default implementation - can be overridden by platform-specific services
        // For MAUI, this would use Microsoft.Maui.Essentials.Preferences
        return null;
    }

    /// <summary>
    /// Sets the stored language preference. Override this method in platform-specific implementations.
    /// </summary>
    protected virtual void SetStoredLanguagePreference(string languageCode)
    {
        // Default implementation - can be overridden by platform-specific services
        // For MAUI, this would use Microsoft.Maui.Essentials.Preferences
    }
}