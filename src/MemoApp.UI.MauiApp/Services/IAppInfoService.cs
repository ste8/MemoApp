namespace MemoApp.UI.MauiApp.Services;

/// <summary>
/// Service for retrieving application information like version, build details, etc.
/// </summary>
public interface IAppInfoService
{
    /// <summary>
    /// Gets the application name
    /// </summary>
    string AppName { get; }

    /// <summary>
    /// Gets the application version (e.g., "1.0.0")
    /// </summary>
    string Version { get; }

    /// <summary>
    /// Gets the application build string (e.g., "1.0.0.123")
    /// </summary>
    string BuildString { get; }

    /// <summary>
    /// Gets the formatted version string for display (e.g., "v1.0.0")
    /// </summary>
    string DisplayVersion { get; }

    /// <summary>
    /// Gets the application package name/bundle ID
    /// </summary>
    string PackageName { get; }

    /// <summary>
    /// Gets the current platform name
    /// </summary>
    string Platform { get; }

    /// <summary>
    /// Gets the target framework version
    /// </summary>
    string FrameworkVersion { get; }

    /// <summary>
    /// Gets comprehensive app information for About/Settings display
    /// </summary>
    AppInfoDetails GetAppDetails();
}

/// <summary>
/// Detailed application information
/// </summary>
public record AppInfoDetails(
    string AppName,
    string Version,
    string BuildString,
    string PackageName,
    string Platform,
    string FrameworkVersion,
    DateTime? BuildDate = null,
    string? Copyright = null
);