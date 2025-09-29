namespace MemoApp.UI.MauiApp.Services.Freemium;

/// <summary>
/// Service responsible for tracking exercise counts for both individual exercises and total counts.
/// Tracks both started and completed counts for comprehensive analytics and ad frequency determination.
/// </summary>
public interface IExerciseCountService
{
    /// <summary>
    /// Increments the started count for a specific exercise type and updates the total count.
    /// For backward compatibility, this maps to IncrementExerciseStartedCountAsync.
    /// </summary>
    /// <param name="exerciseType">The type of exercise (use ExerciseIdentifiers constants)</param>
    /// <returns>A task representing the asynchronous operation</returns>
    Task IncrementExerciseCountAsync(string exerciseType);

    /// <summary>
    /// Increments the started count for a specific exercise type and updates the total started count.
    /// Called when user begins a training session.
    /// </summary>
    /// <param name="exerciseType">The type of exercise (use ExerciseIdentifiers constants)</param>
    /// <returns>A task representing the asynchronous operation</returns>
    Task IncrementExerciseStartedCountAsync(string exerciseType);

    /// <summary>
    /// Increments the completed count for a specific exercise type and updates the total completed count.
    /// Called when user successfully finishes a training session.
    /// </summary>
    /// <param name="exerciseType">The type of exercise (use ExerciseIdentifiers constants)</param>
    /// <returns>A task representing the asynchronous operation</returns>
    Task IncrementExerciseCompletedCountAsync(string exerciseType);
    
    /// <summary>
    /// Gets the started count for a specific exercise type.
    /// For backward compatibility, this maps to GetExerciseStartedCountAsync.
    /// </summary>
    /// <param name="exerciseType">The type of exercise (use ExerciseIdentifiers constants)</param>
    /// <returns>The number of times this exercise has been started</returns>
    Task<int> GetExerciseCountAsync(string exerciseType);

    /// <summary>
    /// Gets the started count for a specific exercise type.
    /// </summary>
    /// <param name="exerciseType">The type of exercise (use ExerciseIdentifiers constants)</param>
    /// <returns>The number of times this exercise has been started</returns>
    Task<int> GetExerciseStartedCountAsync(string exerciseType);

    /// <summary>
    /// Gets the completed count for a specific exercise type.
    /// </summary>
    /// <param name="exerciseType">The type of exercise (use ExerciseIdentifiers constants)</param>
    /// <returns>The number of times this exercise has been completed</returns>
    Task<int> GetExerciseCompletedCountAsync(string exerciseType);

    /// <summary>
    /// Gets the completion rate for a specific exercise type (completed/started ratio).
    /// </summary>
    /// <param name="exerciseType">The type of exercise (use ExerciseIdentifiers constants)</param>
    /// <returns>Completion rate as a percentage (0.0 to 1.0), or 0 if no sessions started</returns>
    Task<double> GetCompletionRateAsync(string exerciseType);

    /// <summary>
    /// Gets the total started count across all exercise types.
    /// For backward compatibility, this maps to GetTotalExerciseStartedCountAsync.
    /// </summary>
    /// <returns>The total number of exercises started across all types</returns>
    Task<int> GetTotalExerciseCountAsync();

    /// <summary>
    /// Gets the total started count across all exercise types.
    /// </summary>
    /// <returns>The total number of exercises started across all types</returns>
    Task<int> GetTotalExerciseStartedCountAsync();

    /// <summary>
    /// Gets the total completed count across all exercise types.
    /// </summary>
    /// <returns>The total number of exercises completed across all types</returns>
    Task<int> GetTotalExerciseCompletedCountAsync();
    
    /// <summary>
    /// Resets both started and completed counts for a specific exercise type.
    /// </summary>
    /// <param name="exerciseType">The type of exercise (use ExerciseIdentifiers constants)</param>
    /// <returns>A task representing the asynchronous operation</returns>
    Task ResetExerciseCountAsync(string exerciseType);

    /// <summary>
    /// Resets the started count for a specific exercise type.
    /// </summary>
    /// <param name="exerciseType">The type of exercise (use ExerciseIdentifiers constants)</param>
    /// <returns>A task representing the asynchronous operation</returns>
    Task ResetExerciseStartedCountAsync(string exerciseType);

    /// <summary>
    /// Resets the completed count for a specific exercise type.
    /// </summary>
    /// <param name="exerciseType">The type of exercise (use ExerciseIdentifiers constants)</param>
    /// <returns>A task representing the asynchronous operation</returns>
    Task ResetExerciseCompletedCountAsync(string exerciseType);

    /// <summary>
    /// Resets all counts (started and completed) across all exercise types.
    /// </summary>
    /// <returns>A task representing the asynchronous operation</returns>
    Task ResetTotalCountAsync();

    /// <summary>
    /// Gets started counts for all exercise types as a dictionary.
    /// For backward compatibility, this maps to GetAllExerciseStartedCountsAsync.
    /// </summary>
    /// <returns>Dictionary with exercise type as key and started count as value</returns>
    Task<Dictionary<string, int>> GetAllExerciseCountsAsync();

    /// <summary>
    /// Gets started counts for all exercise types as a dictionary.
    /// </summary>
    /// <returns>Dictionary with exercise type as key and started count as value</returns>
    Task<Dictionary<string, int>> GetAllExerciseStartedCountsAsync();

    /// <summary>
    /// Gets completed counts for all exercise types as a dictionary.
    /// </summary>
    /// <returns>Dictionary with exercise type as key and completed count as value</returns>
    Task<Dictionary<string, int>> GetAllExerciseCompletedCountsAsync();

    /// <summary>
    /// Gets completion rates for all exercise types as a dictionary.
    /// </summary>
    /// <returns>Dictionary with exercise type as key and completion rate (0.0-1.0) as value</returns>
    Task<Dictionary<string, double>> GetAllCompletionRatesAsync();
}