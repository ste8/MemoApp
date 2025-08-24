using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MemoApp.Core.Services.ErrorMonitoring;

/// <summary>
/// Extension methods for critical error monitoring with pattern-based logging
/// </summary>
public static class ErrorMonitoringExtensions
{
    // Pattern-based error tracking to avoid noise
    private static readonly Dictionary<string, DateTime> LastErrorTime = new();
    private static readonly Dictionary<string, int> ErrorCounts = new();
    private const int ERROR_THRESHOLD = 5; // Log after 5 occurrences
    private const int RATE_LIMIT_MINUTES = 5; // Rate limit per error type

    /// <summary>
    /// Logs a critical pack download failure
    /// </summary>
    public static async Task LogPackDownloadFailureAsync(
        this IErrorMonitoringService errorService,
        string packId,
        string downloadUrl,
        Exception exception,
        int? retryAttempt = null,
        long? availableStorageMb = null,
        bool? networkAvailable = null)
    {
        var context = new Dictionary<string, object>
        {
            ["pack_id"] = packId,
            ["download_url"] = downloadUrl,
            ["network_available"] = networkAvailable ?? false
        };

        if (retryAttempt.HasValue)
            context["retry_attempt"] = retryAttempt.Value;

        if (availableStorageMb.HasValue)
            context["storage_available_mb"] = availableStorageMb.Value;

        await errorService.LogCriticalErrorAsync(
            CriticalErrorTypes.PackDownloadFailed,
            exception,
            context);
    }

    /// <summary>
    /// Logs when a pack becomes unusable (extraction/parsing failure)
    /// </summary>
    public static async Task LogPackUnusableAsync(
        this IErrorMonitoringService errorService,
        string packId,
        Exception exception,
        string failureReason,
        long? fileSizeMb = null)
    {
        var context = new Dictionary<string, object>
        {
            ["pack_id"] = packId,
            ["failure_reason"] = failureReason // "extraction_failed", "json_corrupt", "missing_files"
        };

        if (fileSizeMb.HasValue)
            context["file_size_mb"] = fileSizeMb.Value;

        await errorService.LogCriticalErrorAsync(
            CriticalErrorTypes.PackUnusable,
            exception,
            context);
    }

    /// <summary>
    /// Logs critical pack corruption
    /// </summary>
    public static async Task LogPackCorruptionAsync(
        this IErrorMonitoringService errorService,
        string packId,
        string corruptionType,
        string impact)
    {
        var context = new Dictionary<string, object>
        {
            ["pack_id"] = packId,
            ["corruption_type"] = corruptionType, // "manifest_unreadable", "images_missing", "structure_invalid"
            ["impact"] = impact // "exercise_cannot_start"
        };

        await errorService.LogCriticalErrorAsync(
            CriticalErrorTypes.PackCorruptionCritical,
            new InvalidOperationException($"Pack corruption: {corruptionType}"),
            context);
    }

    /// <summary>
    /// Logs exercise startup failure
    /// </summary>
    public static async Task LogExerciseStartupFailureAsync(
        this IErrorMonitoringService errorService,
        string exerciseType,
        Exception exception,
        string failureReason,
        string? packId = null,
        Dictionary<string, object>? userSettings = null)
    {
        var context = new Dictionary<string, object>
        {
            ["exercise_type"] = exerciseType,
            ["failure_reason"] = failureReason // "no_tokens_available", "images_missing", "settings_invalid"
        };

        if (!string.IsNullOrEmpty(packId))
            context["pack_id"] = packId;

        if (userSettings != null)
            context["user_settings"] = userSettings;

        await errorService.LogCriticalErrorAsync(
            CriticalErrorTypes.ExerciseStartupFailure,
            exception,
            context);
    }

    /// <summary>
    /// Logs when exercise becomes unresponsive
    /// </summary>
    public static async Task LogExerciseFrozenAsync(
        this IErrorMonitoringService errorService,
        string exerciseType,
        int freezeDurationSeconds,
        long memoryUsageMb,
        int loadedImagesCount)
    {
        var context = new Dictionary<string, object>
        {
            ["exercise_type"] = exerciseType,
            ["freeze_duration_seconds"] = freezeDurationSeconds,
            ["memory_usage_mb"] = memoryUsageMb,
            ["loaded_images_count"] = loadedImagesCount
        };

        await errorService.LogCriticalErrorAsync(
            CriticalErrorTypes.ExerciseFrozen,
            new TimeoutException($"Exercise frozen for {freezeDurationSeconds} seconds"),
            context);
    }

    /// <summary>
    /// Logs settings corruption
    /// </summary>
    public static async Task LogSettingsCorruptionAsync(
        this IErrorMonitoringService errorService,
        string corruptedSetting,
        Exception exception,
        string impact,
        bool recoveryAttempted = false)
    {
        var context = new Dictionary<string, object>
        {
            ["corrupted_setting"] = corruptedSetting, // "pack_selection", "exercise_preferences"
            ["impact"] = impact, // "app_unusable", "feature_broken"
            ["recovery_attempted"] = recoveryAttempted
        };

        await errorService.LogCriticalErrorAsync(
            CriticalErrorTypes.SettingsCorruption,
            exception,
            context);
    }

    /// <summary>
    /// Logs user data loss
    /// </summary>
    public static async Task LogUserDataLossAsync(
        this IErrorMonitoringService errorService,
        string dataType,
        Exception exception,
        string estimatedLoss)
    {
        var context = new Dictionary<string, object>
        {
            ["data_type"] = dataType, // "exercise_progress", "pack_preferences"
            ["estimated_loss"] = estimatedLoss // "3_months_progress"
        };

        await errorService.LogCriticalErrorAsync(
            CriticalErrorTypes.UserDataLoss,
            exception,
            context);
    }

    /// <summary>
    /// Pattern-based image batch failure logging
    /// </summary>
    public static async Task LogImageBatchFailureAsync(
        this IErrorMonitoringService errorService,
        string packId,
        string category,
        int failedImagesCount,
        int totalImages,
        Exception? lastException = null)
    {
        // Only log when failure rate is significant
        var failureRate = (failedImagesCount * 100.0 / totalImages);
        if (failureRate < 20) // Less than 20% failure rate is not critical
            return;

        var context = new Dictionary<string, object>
        {
            ["pack_id"] = packId,
            ["category"] = category,
            ["failed_images_count"] = failedImagesCount,
            ["failure_rate_percent"] = failureRate
        };

        await errorService.LogCriticalErrorAsync(
            CriticalErrorTypes.ImageBatchFailure,
            lastException ?? new InvalidOperationException($"Image batch failure: {failedImagesCount}/{totalImages}"),
            context);
    }

    /// <summary>
    /// Rate-limited network error logging
    /// </summary>
    public static async Task LogPersistentNetworkFailureAsync(
        this IErrorMonitoringService errorService,
        string operation,
        Exception exception,
        double failureDurationMinutes)
    {
        // Only log if network has been failing for > 5 minutes
        if (failureDurationMinutes < 5)
            return;

        var context = new Dictionary<string, object>
        {
            ["operation"] = operation,
            ["failure_duration_minutes"] = failureDurationMinutes,
            ["connection_type"] = "unknown" // Will be set by caller if available
        };

        await errorService.LogCriticalErrorAsync(
            CriticalErrorTypes.NetworkPersistentFailure,
            exception,
            context);
    }

    /// <summary>
    /// Tracks critical performance issues only
    /// </summary>
    public static async Task TrackCriticalPerformanceAsync(
        this IErrorMonitoringService errorService,
        string issueType,
        long durationMs,
        Dictionary<string, object>? additionalContext = null)
    {
        var context = new Dictionary<string, object>();
        
        // Add duration-specific keys based on issue type
        switch (issueType)
        {
            case PerformanceIssueTypes.AppStartupSlow:
                context["startup_time_ms"] = durationMs;
                break;
            case PerformanceIssueTypes.ExerciseLoadTimeout:
                context["timeout_duration_ms"] = durationMs;
                break;
            case PerformanceIssueTypes.PackDownloadSlow:
                context["download_duration_ms"] = durationMs;
                break;
        }

        // Add additional context
        if (additionalContext != null)
        {
            foreach (var kvp in additionalContext)
            {
                context[kvp.Key] = kvp.Value;
            }
        }

        await errorService.LogPerformanceIssueAsync(issueType, context);
    }

    /// <summary>
    /// Tracks memory pressure
    /// </summary>
    public static async Task TrackMemoryPressureAsync(
        this IErrorMonitoringService errorService,
        long currentMemoryMb,
        long availableMemoryMb,
        int loadedImagesCount,
        string? activeExercise = null)
    {
        var memoryPressureRatio = (double)currentMemoryMb / (currentMemoryMb + availableMemoryMb);
        
        // Only log critical memory pressure (> 90% used)
        if (memoryPressureRatio < 0.9)
            return;

        var context = new Dictionary<string, object>
        {
            ["current_memory_mb"] = currentMemoryMb,
            ["available_memory_mb"] = availableMemoryMb,
            ["loaded_images_count"] = loadedImagesCount,
            ["memory_pressure_ratio"] = memoryPressureRatio
        };

        if (!string.IsNullOrEmpty(activeExercise))
            context["active_exercise"] = activeExercise;

        await errorService.LogPerformanceIssueAsync(
            PerformanceIssueTypes.MemoryCritical,
            context);
    }
}