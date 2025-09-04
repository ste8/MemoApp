namespace MemoApp.UI.MauiApp.Services.Freemium;

/// <summary>
/// Service responsible for tracking exercise completion counts for both individual exercises and total counts.
/// Used primarily for ad frequency determination and analytics.
/// </summary>
public interface IExerciseCountService
{
    /// <summary>
    /// Increments the count for a specific exercise type and updates the total count.
    /// </summary>
    /// <param name="exerciseType">The type of exercise (use ExerciseIdentifiers constants)</param>
    /// <returns>A task representing the asynchronous operation</returns>
    Task IncrementExerciseCountAsync(string exerciseType);
    
    /// <summary>
    /// Gets the count for a specific exercise type.
    /// </summary>
    /// <param name="exerciseType">The type of exercise (use ExerciseIdentifiers constants)</param>
    /// <returns>The number of times this exercise has been completed</returns>
    Task<int> GetExerciseCountAsync(string exerciseType);
    
    /// <summary>
    /// Gets the total count across all exercise types.
    /// </summary>
    /// <returns>The total number of exercises completed across all types</returns>
    Task<int> GetTotalExerciseCountAsync();
    
    /// <summary>
    /// Resets the count for a specific exercise type.
    /// </summary>
    /// <param name="exerciseType">The type of exercise (use ExerciseIdentifiers constants)</param>
    /// <returns>A task representing the asynchronous operation</returns>
    Task ResetExerciseCountAsync(string exerciseType);
    
    /// <summary>
    /// Resets the total count across all exercise types.
    /// </summary>
    /// <returns>A task representing the asynchronous operation</returns>
    Task ResetTotalCountAsync();
    
    /// <summary>
    /// Gets counts for all exercise types as a dictionary.
    /// </summary>
    /// <returns>Dictionary with exercise type as key and count as value</returns>
    Task<Dictionary<string, int>> GetAllExerciseCountsAsync();
}