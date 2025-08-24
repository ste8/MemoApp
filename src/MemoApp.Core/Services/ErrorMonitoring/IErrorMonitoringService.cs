using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MemoApp.Core.Services.ErrorMonitoring;

/// <summary>
/// Interface for critical error monitoring service focusing on actionable errors
/// </summary>
public interface IErrorMonitoringService
{
    /// <summary>
    /// Initializes the error monitoring service
    /// </summary>
    Task InitializeAsync();

    /// <summary>
    /// Logs critical errors that impact user experience and core functionality
    /// </summary>
    Task LogCriticalErrorAsync(string errorType, Exception exception, Dictionary<string, object>? context = null);

    /// <summary>
    /// Logs severe performance issues that affect usability
    /// </summary>
    Task LogPerformanceIssueAsync(string issueType, Dictionary<string, object>? context = null);

    /// <summary>
    /// Sets minimal user context for debugging
    /// </summary>
    Task SetUserContextAsync(object userContext);

    /// <summary>
    /// Enables or disables error reporting (for user consent)
    /// </summary>
    Task EnableErrorReportingAsync(bool enabled);

    /// <summary>
    /// Gets whether error reporting is currently enabled
    /// </summary>
    Task<bool> IsErrorReportingEnabledAsync();
}

/// <summary>
/// Critical error types for monitoring
/// </summary>
public static class CriticalErrorTypes
{
    // App Stability
    public const string AppCrash = "app_crash";
    public const string NavigationFailure = "navigation_failure";
    public const string UnhandledException = "unhandled_exception";
    public const string MauiUnhandledException = "maui_unhandled_exception";
    
    // Pack Management
    public const string PackDownloadFailed = "pack_download_failed";
    public const string PackUnusable = "pack_unusable";
    public const string PackCorruptionCritical = "pack_corruption_critical";
    
    // Exercise Core Failures
    public const string ExerciseStartupFailure = "exercise_startup_failure";
    public const string ExerciseFrozen = "exercise_frozen";
    
    // Settings & Data Loss
    public const string SettingsCorruption = "settings_corruption";
    public const string UserDataLoss = "user_data_loss";
    
    // Pattern-Based Failures
    public const string ImageBatchFailure = "image_batch_failure";
    public const string NetworkPersistentFailure = "network_persistent_failure";
}

/// <summary>
/// Performance issue types for monitoring
/// </summary>
public static class PerformanceIssueTypes
{
    public const string AppStartupSlow = "app_startup_slow";
    public const string ExerciseLoadTimeout = "exercise_load_timeout";
    public const string MemoryCritical = "memory_critical";
    public const string PackDownloadSlow = "pack_download_slow";
}