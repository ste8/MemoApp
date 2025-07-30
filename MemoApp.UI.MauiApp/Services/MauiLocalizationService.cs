using MemoApp.Localization.Services;

namespace MemoApp.UI.MauiApp.Services;

/// <summary>
/// MAUI-specific implementation of LocalizationService that uses Microsoft.Maui.Essentials.Preferences
/// for persistent language storage.
/// </summary>
public class MauiLocalizationService : LocalizationService
{
    private const string LanguagePreferenceKey = "app_language_preference";
    private const string NumberFormatPreferenceKey = "app_number_format_preference";

    /// <summary>
    /// Gets the stored language preference using MAUI Preferences.
    /// </summary>
    protected override string? GetStoredLanguagePreference()
    {
        try
        {
            return Preferences.Get(LanguagePreferenceKey, string.Empty);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Sets the stored language preference using MAUI Preferences.
    /// </summary>
    protected override void SetStoredLanguagePreference(string languageCode)
    {
        try
        {
            Preferences.Set(LanguagePreferenceKey, languageCode);
        }
        catch
        {
            // Ignore save errors - not critical for app functionality
        }
    }

    /// <summary>
    /// Gets the stored number format preference using MAUI Preferences.
    /// </summary>
    protected override NumberFormat? GetStoredNumberFormatPreference()
    {
        try
        {
            var storedValue = Preferences.Get(NumberFormatPreferenceKey, (int)NumberFormat.Padded);
            return (NumberFormat)storedValue;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Sets the stored number format preference using MAUI Preferences.
    /// </summary>
    protected override void SetStoredNumberFormatPreference(NumberFormat numberFormat)
    {
        try
        {
            Preferences.Set(NumberFormatPreferenceKey, (int)numberFormat);
        }
        catch
        {
            // Ignore save errors - not critical for app functionality
        }
    }
}