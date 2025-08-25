using Microsoft.Extensions.Configuration;

namespace MemoApp.UI.MauiApp.Configuration;

/// <summary>
/// Extension methods for type-safe configuration access with fallback values
/// </summary>
public static class ConfigurationExtensions
{
    // Default values for pack management configuration
    private const string DefaultPacksIndexUrl = "https://github.com/ste8/MemoApp-Packs/releases/download/packs-manifest/packs.json";
    private const int DefaultCacheDurationMinutes = 30;

    /// <summary>
    /// Gets the packs index URL from configuration with fallback to default
    /// </summary>
    /// <param name="configuration">The configuration instance</param>
    /// <returns>The configured or default packs index URL</returns>
    public static string GetPacksIndexUrl(this IConfiguration configuration)
    {
        return configuration["PackManagement:PacksIndexUrl"] ?? DefaultPacksIndexUrl;
    }

    /// <summary>
    /// Gets the cache duration in minutes from configuration with fallback to default
    /// </summary>
    /// <param name="configuration">The configuration instance</param>
    /// <returns>The configured or default cache duration in minutes</returns>
    public static int GetCacheDurationMinutes(this IConfiguration configuration)
    {
        return int.TryParse(configuration["PackManagement:CacheDurationMinutes"], out var value) 
            ? value 
            : DefaultCacheDurationMinutes;
    }

    /// <summary>
    /// Gets the Sentry DSN from configuration
    /// </summary>
    /// <param name="configuration">The configuration instance</param>
    /// <returns>The Sentry DSN or null if not configured</returns>
    public static string? GetSentryDsn(this IConfiguration configuration)
    {
        return configuration["Sentry:Dsn"];
    }

    /// <summary>
    /// Checks if error monitoring is enabled in configuration
    /// </summary>
    /// <param name="configuration">The configuration instance</param>
    /// <returns>True if error monitoring is enabled, false otherwise</returns>
    public static bool IsErrorMonitoringEnabled(this IConfiguration configuration)
    {
        return bool.TryParse(configuration["ErrorMonitoring:Enabled"], out var enabled) && enabled;
    }
}