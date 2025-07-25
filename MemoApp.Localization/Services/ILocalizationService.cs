using System.Globalization;

namespace MemoApp.Localization.Services;

/// <summary>
/// Service for handling application localization across different platforms.
/// Provides methods to get localized strings and manage culture settings.
/// </summary>
public interface ILocalizationService
{
    /// <summary>
    /// Gets a localized string by key.
    /// </summary>
    /// <param name="key">The resource key</param>
    /// <returns>The localized string, or the key if not found</returns>
    string GetString(string key);

    /// <summary>
    /// Gets a localized string by key with format arguments.
    /// </summary>
    /// <param name="key">The resource key</param>
    /// <param name="args">Format arguments</param>
    /// <returns>The formatted localized string, or the key if not found</returns>
    string GetString(string key, params object[] args);

    /// <summary>
    /// Gets the current culture.
    /// </summary>
    CultureInfo CurrentCulture { get; }

    /// <summary>
    /// Sets the application culture.
    /// </summary>
    /// <param name="culture">The culture to set</param>
    void SetCulture(CultureInfo culture);

    /// <summary>
    /// Sets the application culture by culture name.
    /// </summary>
    /// <param name="cultureName">The culture name (e.g., "en", "it")</param>
    void SetCulture(string cultureName);

    /// <summary>
    /// Gets all available cultures supported by the application.
    /// </summary>
    /// <returns>List of supported cultures</returns>
    IEnumerable<CultureInfo> GetAvailableCultures();

    /// <summary>
    /// Event raised when the culture changes.
    /// </summary>
    event EventHandler<CultureInfo>? CultureChanged;
}