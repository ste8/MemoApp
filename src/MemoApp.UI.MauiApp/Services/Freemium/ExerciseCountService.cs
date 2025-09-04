using Microsoft.Extensions.Logging;
using MemoApp.UI.MauiApp.Constants;

namespace MemoApp.UI.MauiApp.Services.Freemium;

/// <summary>
/// Service responsible for tracking exercise completion counts.
/// Maintains both individual exercise counts and total counts using platform preferences.
/// </summary>
public class ExerciseCountService : IExerciseCountService
{
    private readonly ILogger<ExerciseCountService> _logger;
    
    private const string TotalCountKey = "total_exercise_count";
    private const string ExerciseCountKeyPrefix = "exercise_count_";
    
    public ExerciseCountService(ILogger<ExerciseCountService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }
    
    public async Task IncrementExerciseCountAsync(string exerciseType)
    {
        try
        {
            if (string.IsNullOrEmpty(exerciseType))
            {
                _logger.LogWarning("Exercise type is null or empty, skipping increment");
                return;
            }
            
            // Increment individual exercise count
            var individualKey = GetExerciseCountKey(exerciseType);
            var individualCount = Preferences.Get(individualKey, 0);
            individualCount++;
            Preferences.Set(individualKey, individualCount);
            
            // Increment total count
            var totalCount = Preferences.Get(TotalCountKey, 0);
            totalCount++;
            Preferences.Set(TotalCountKey, totalCount);
            
            _logger.LogInformation("Incremented {ExerciseType} count to {IndividualCount} (total: {TotalCount})", 
                exerciseType, individualCount, totalCount);
            
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error incrementing exercise count for {ExerciseType}", exerciseType);
        }
    }
    
    public async Task<int> GetExerciseCountAsync(string exerciseType)
    {
        try
        {
            if (string.IsNullOrEmpty(exerciseType))
            {
                _logger.LogWarning("Exercise type is null or empty, returning 0");
                return 0;
            }
            
            var key = GetExerciseCountKey(exerciseType);
            return await Task.FromResult(Preferences.Get(key, 0));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting exercise count for {ExerciseType}", exerciseType);
            return 0;
        }
    }
    
    public async Task<int> GetTotalExerciseCountAsync()
    {
        try
        {
            return await Task.FromResult(Preferences.Get(TotalCountKey, 0));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting total exercise count");
            return 0;
        }
    }
    
    public async Task ResetExerciseCountAsync(string exerciseType)
    {
        try
        {
            if (string.IsNullOrEmpty(exerciseType))
            {
                _logger.LogWarning("Exercise type is null or empty, skipping reset");
                return;
            }
            
            var key = GetExerciseCountKey(exerciseType);
            var oldCount = Preferences.Get(key, 0);
            Preferences.Set(key, 0);
            
            // Recalculate total count
            await RecalculateTotalCountAsync();
            
            _logger.LogInformation("Reset {ExerciseType} count from {OldCount} to 0", exerciseType, oldCount);
            
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resetting exercise count for {ExerciseType}", exerciseType);
        }
    }
    
    public async Task ResetTotalCountAsync()
    {
        try
        {
            var oldCount = Preferences.Get(TotalCountKey, 0);
            Preferences.Set(TotalCountKey, 0);
            
            // Also reset all individual counts
            foreach (var exerciseType in ExerciseTypes.All)
            {
                var key = GetExerciseCountKey(exerciseType);
                Preferences.Set(key, 0);
            }
            
            _logger.LogInformation("Reset total exercise count from {OldCount} to 0 and all individual counts", oldCount);
            
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resetting total exercise count");
        }
    }
    
    public async Task<Dictionary<string, int>> GetAllExerciseCountsAsync()
    {
        try
        {
            var counts = new Dictionary<string, int>();
            
            foreach (var exerciseType in ExerciseTypes.All)
            {
                counts[exerciseType] = await GetExerciseCountAsync(exerciseType);
            }
            
            return counts;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all exercise counts");
            return new Dictionary<string, int>();
        }
    }
    
    private string GetExerciseCountKey(string exerciseType)
    {
        return $"{ExerciseCountKeyPrefix}{exerciseType.ToLowerInvariant()}";
    }
    
    private async Task RecalculateTotalCountAsync()
    {
        try
        {
            var allCounts = await GetAllExerciseCountsAsync();
            var newTotal = allCounts.Values.Sum();
            Preferences.Set(TotalCountKey, newTotal);
            
            _logger.LogInformation("Recalculated total count to {NewTotal}", newTotal);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recalculating total count");
        }
    }
}