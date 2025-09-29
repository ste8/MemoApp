using Microsoft.Extensions.Logging;
using MemoApp.UI.MauiApp.Constants;

namespace MemoApp.UI.MauiApp.Services.Freemium;

/// <summary>
/// Service responsible for tracking exercise counts with dual tracking (started/completed).
/// Maintains both individual exercise counts and total counts using platform preferences.
/// Supports legacy single-count methods for backward compatibility.
/// </summary>
public class ExerciseCountService : IExerciseCountService
{
    private readonly ILogger<ExerciseCountService> _logger;

    // Legacy keys for backward compatibility
    private const string TotalCountKey = "total_exercise_count";
    private const string ExerciseCountKeyPrefix = "exercise_count_";

    // New dual tracking keys
    private const string TotalStartedCountKey = "total_exercise_started_count";
    private const string TotalCompletedCountKey = "total_exercise_completed_count";
    private const string ExerciseStartedCountKeyPrefix = "exercise_started_count_";
    private const string ExerciseCompletedCountKeyPrefix = "exercise_completed_count_";
    
    public ExerciseCountService(ILogger<ExerciseCountService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }
    
    public async Task IncrementExerciseCountAsync(string exerciseType)
    {
        // For backward compatibility, delegate to started count
        await IncrementExerciseStartedCountAsync(exerciseType);
    }

    public async Task IncrementExerciseStartedCountAsync(string exerciseType)
    {
        try
        {
            if (string.IsNullOrEmpty(exerciseType))
            {
                _logger.LogWarning("Exercise type is null or empty, skipping started count increment");
                return;
            }

            // Migrate legacy data if needed
            await MigrateLegacyDataIfNeeded(exerciseType);

            // Increment individual started count
            var individualKey = GetExerciseStartedCountKey(exerciseType);
            var individualCount = Preferences.Get(individualKey, 0);
            individualCount++;
            Preferences.Set(individualKey, individualCount);

            // Increment total started count
            var totalCount = Preferences.Get(TotalStartedCountKey, 0);
            totalCount++;
            Preferences.Set(TotalStartedCountKey, totalCount);

            // Update legacy total count for backward compatibility
            Preferences.Set(TotalCountKey, totalCount);

            _logger.LogInformation("Incremented {ExerciseType} started count to {IndividualCount} (total: {TotalCount})",
                exerciseType, individualCount, totalCount);

            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error incrementing started exercise count for {ExerciseType}", exerciseType);
        }
    }

    public async Task IncrementExerciseCompletedCountAsync(string exerciseType)
    {
        try
        {
            if (string.IsNullOrEmpty(exerciseType))
            {
                _logger.LogWarning("Exercise type is null or empty, skipping completed count increment");
                return;
            }

            // Increment individual completed count
            var individualKey = GetExerciseCompletedCountKey(exerciseType);
            var individualCount = Preferences.Get(individualKey, 0);
            individualCount++;
            Preferences.Set(individualKey, individualCount);

            // Increment total completed count
            var totalCount = Preferences.Get(TotalCompletedCountKey, 0);
            totalCount++;
            Preferences.Set(TotalCompletedCountKey, totalCount);

            _logger.LogInformation("Incremented {ExerciseType} completed count to {IndividualCount} (total: {TotalCount})",
                exerciseType, individualCount, totalCount);

            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error incrementing completed exercise count for {ExerciseType}", exerciseType);
        }
    }
    
    public async Task<int> GetExerciseCountAsync(string exerciseType)
    {
        // For backward compatibility, delegate to started count
        return await GetExerciseStartedCountAsync(exerciseType);
    }

    public async Task<int> GetExerciseStartedCountAsync(string exerciseType)
    {
        try
        {
            if (string.IsNullOrEmpty(exerciseType))
            {
                _logger.LogWarning("Exercise type is null or empty, returning 0");
                return 0;
            }

            // Migrate legacy data if needed
            await MigrateLegacyDataIfNeeded(exerciseType);

            var key = GetExerciseStartedCountKey(exerciseType);
            return await Task.FromResult(Preferences.Get(key, 0));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting started exercise count for {ExerciseType}", exerciseType);
            return 0;
        }
    }

    public async Task<int> GetExerciseCompletedCountAsync(string exerciseType)
    {
        try
        {
            if (string.IsNullOrEmpty(exerciseType))
            {
                _logger.LogWarning("Exercise type is null or empty, returning 0");
                return 0;
            }

            var key = GetExerciseCompletedCountKey(exerciseType);
            return await Task.FromResult(Preferences.Get(key, 0));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting completed exercise count for {ExerciseType}", exerciseType);
            return 0;
        }
    }

    public async Task<double> GetCompletionRateAsync(string exerciseType)
    {
        try
        {
            var startedCount = await GetExerciseStartedCountAsync(exerciseType);
            var completedCount = await GetExerciseCompletedCountAsync(exerciseType);

            if (startedCount == 0)
                return 0.0;

            return (double)completedCount / startedCount;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating completion rate for {ExerciseType}", exerciseType);
            return 0.0;
        }
    }
    
    public async Task<int> GetTotalExerciseCountAsync()
    {
        // For backward compatibility, delegate to started count
        return await GetTotalExerciseStartedCountAsync();
    }

    public async Task<int> GetTotalExerciseStartedCountAsync()
    {
        try
        {
            // Migrate legacy data if needed
            await MigrateLegacyTotalDataIfNeeded();

            return await Task.FromResult(Preferences.Get(TotalStartedCountKey, 0));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting total started exercise count");
            return 0;
        }
    }

    public async Task<int> GetTotalExerciseCompletedCountAsync()
    {
        try
        {
            return await Task.FromResult(Preferences.Get(TotalCompletedCountKey, 0));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting total completed exercise count");
            return 0;
        }
    }
    
    public async Task ResetExerciseCountAsync(string exerciseType)
    {
        // Reset both started and completed counts
        await ResetExerciseStartedCountAsync(exerciseType);
        await ResetExerciseCompletedCountAsync(exerciseType);
    }

    public async Task ResetExerciseStartedCountAsync(string exerciseType)
    {
        try
        {
            if (string.IsNullOrEmpty(exerciseType))
            {
                _logger.LogWarning("Exercise type is null or empty, skipping started count reset");
                return;
            }

            var key = GetExerciseStartedCountKey(exerciseType);
            var oldCount = Preferences.Get(key, 0);
            Preferences.Set(key, 0);

            // Also reset legacy key for backward compatibility
            var legacyKey = GetExerciseCountKey(exerciseType);
            Preferences.Set(legacyKey, 0);

            // Recalculate total counts
            await RecalculateTotalCountsAsync();

            _logger.LogInformation("Reset {ExerciseType} started count from {OldCount} to 0", exerciseType, oldCount);

            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resetting started exercise count for {ExerciseType}", exerciseType);
        }
    }

    public async Task ResetExerciseCompletedCountAsync(string exerciseType)
    {
        try
        {
            if (string.IsNullOrEmpty(exerciseType))
            {
                _logger.LogWarning("Exercise type is null or empty, skipping completed count reset");
                return;
            }

            var key = GetExerciseCompletedCountKey(exerciseType);
            var oldCount = Preferences.Get(key, 0);
            Preferences.Set(key, 0);

            // Recalculate total counts
            await RecalculateTotalCountsAsync();

            _logger.LogInformation("Reset {ExerciseType} completed count from {OldCount} to 0", exerciseType, oldCount);

            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resetting completed exercise count for {ExerciseType}", exerciseType);
        }
    }
    
    public async Task ResetTotalCountAsync()
    {
        try
        {
            var oldStartedCount = Preferences.Get(TotalStartedCountKey, 0);
            var oldCompletedCount = Preferences.Get(TotalCompletedCountKey, 0);

            // Reset total counts
            Preferences.Set(TotalStartedCountKey, 0);
            Preferences.Set(TotalCompletedCountKey, 0);
            Preferences.Set(TotalCountKey, 0); // Legacy compatibility

            // Reset all individual counts
            foreach (var exerciseType in ExerciseTypes.All)
            {
                var startedKey = GetExerciseStartedCountKey(exerciseType);
                var completedKey = GetExerciseCompletedCountKey(exerciseType);
                var legacyKey = GetExerciseCountKey(exerciseType);

                Preferences.Set(startedKey, 0);
                Preferences.Set(completedKey, 0);
                Preferences.Set(legacyKey, 0); // Legacy compatibility
            }

            _logger.LogInformation("Reset all exercise counts - started: {OldStartedCount} to 0, completed: {OldCompletedCount} to 0",
                oldStartedCount, oldCompletedCount);

            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resetting total exercise counts");
        }
    }
    
    public async Task<Dictionary<string, int>> GetAllExerciseCountsAsync()
    {
        // For backward compatibility, delegate to started counts
        return await GetAllExerciseStartedCountsAsync();
    }

    public async Task<Dictionary<string, int>> GetAllExerciseStartedCountsAsync()
    {
        try
        {
            var counts = new Dictionary<string, int>();

            foreach (var exerciseType in ExerciseTypes.All)
            {
                counts[exerciseType] = await GetExerciseStartedCountAsync(exerciseType);
            }

            return counts;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all started exercise counts");
            return new Dictionary<string, int>();
        }
    }

    public async Task<Dictionary<string, int>> GetAllExerciseCompletedCountsAsync()
    {
        try
        {
            var counts = new Dictionary<string, int>();

            foreach (var exerciseType in ExerciseTypes.All)
            {
                counts[exerciseType] = await GetExerciseCompletedCountAsync(exerciseType);
            }

            return counts;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all completed exercise counts");
            return new Dictionary<string, int>();
        }
    }

    public async Task<Dictionary<string, double>> GetAllCompletionRatesAsync()
    {
        try
        {
            var rates = new Dictionary<string, double>();

            foreach (var exerciseType in ExerciseTypes.All)
            {
                rates[exerciseType] = await GetCompletionRateAsync(exerciseType);
            }

            return rates;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all completion rates");
            return new Dictionary<string, double>();
        }
    }
    
    // Key generation methods
    private string GetExerciseCountKey(string exerciseType)
    {
        return $"{ExerciseCountKeyPrefix}{exerciseType.ToLowerInvariant()}";
    }

    private string GetExerciseStartedCountKey(string exerciseType)
    {
        return $"{ExerciseStartedCountKeyPrefix}{exerciseType.ToLowerInvariant()}";
    }

    private string GetExerciseCompletedCountKey(string exerciseType)
    {
        return $"{ExerciseCompletedCountKeyPrefix}{exerciseType.ToLowerInvariant()}";
    }

    // Migration and recalculation methods
    private async Task MigrateLegacyDataIfNeeded(string exerciseType)
    {
        try
        {
            var startedKey = GetExerciseStartedCountKey(exerciseType);
            var legacyKey = GetExerciseCountKey(exerciseType);

            // If new started key doesn't exist but legacy key does, migrate
            if (!Preferences.ContainsKey(startedKey) && Preferences.ContainsKey(legacyKey))
            {
                var legacyCount = Preferences.Get(legacyKey, 0);
                Preferences.Set(startedKey, legacyCount);

                _logger.LogInformation("Migrated legacy data for {ExerciseType}: {Count} started sessions",
                    exerciseType, legacyCount);
            }

            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error migrating legacy data for {ExerciseType}", exerciseType);
        }
    }

    private async Task MigrateLegacyTotalDataIfNeeded()
    {
        try
        {
            // If new total started key doesn't exist but legacy total key does, migrate
            if (!Preferences.ContainsKey(TotalStartedCountKey) && Preferences.ContainsKey(TotalCountKey))
            {
                var legacyTotal = Preferences.Get(TotalCountKey, 0);
                Preferences.Set(TotalStartedCountKey, legacyTotal);

                _logger.LogInformation("Migrated legacy total data: {Count} total started sessions", legacyTotal);
            }

            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error migrating legacy total data");
        }
    }

    private async Task RecalculateTotalCountsAsync()
    {
        try
        {
            var allStartedCounts = await GetAllExerciseStartedCountsAsync();
            var allCompletedCounts = await GetAllExerciseCompletedCountsAsync();

            var newTotalStarted = allStartedCounts.Values.Sum();
            var newTotalCompleted = allCompletedCounts.Values.Sum();

            Preferences.Set(TotalStartedCountKey, newTotalStarted);
            Preferences.Set(TotalCompletedCountKey, newTotalCompleted);
            Preferences.Set(TotalCountKey, newTotalStarted); // Legacy compatibility

            _logger.LogInformation("Recalculated total counts - started: {TotalStarted}, completed: {TotalCompleted}",
                newTotalStarted, newTotalCompleted);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recalculating total counts");
        }
    }

    // Legacy method for backward compatibility
    private async Task RecalculateTotalCountAsync()
    {
        await RecalculateTotalCountsAsync();
    }
}