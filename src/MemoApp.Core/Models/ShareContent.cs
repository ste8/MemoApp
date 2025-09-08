namespace MemoApp.Core.Models;

/// <summary>
/// Represents content to be shared from exercise results
/// </summary>
public class ShareContent
{
    /// <summary>
    /// Title for the share dialog
    /// </summary>
    public required string Title { get; init; }

    /// <summary>
    /// Main message content to share
    /// </summary>
    public required string Message { get; init; }

    /// <summary>
    /// Optional URL to include in the share
    /// </summary>
    public string? AppUrl { get; init; }

    /// <summary>
    /// Exercise type that generated this content
    /// </summary>
    public required string ExerciseType { get; init; }
}

/// <summary>
/// Base class for exercise results that can be shared
/// </summary>
public abstract class ShareableResult
{
    /// <summary>
    /// Duration of the exercise
    /// </summary>
    public TimeSpan Duration { get; set; }

    /// <summary>
    /// Type of exercise (FlashNumbers, BigNumber, etc.)
    /// </summary>
    public required string ExerciseType { get; set; }

    /// <summary>
    /// Timestamp when the exercise was completed
    /// </summary>
    public DateTime CompletedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Generate shareable content for this result
    /// </summary>
    /// <param name="localizationService">Service for getting localized strings</param>
    /// <returns>ShareContent ready to be shared</returns>
    public abstract ShareContent GenerateShareContent(Func<string, string> getString);
}

/// <summary>
/// Flash Numbers exercise result for sharing
/// </summary>
public class FlashNumbersShareResult : ShareableResult
{
    /// <summary>
    /// Total number of numbers memorized
    /// </summary>
    public int TotalNumbers { get; set; }

    /// <summary>
    /// Range of numbers used (e.g., "0-99")
    /// </summary>
    public required string NumberRange { get; set; }

    /// <summary>
    /// Average time per number
    /// </summary>
    public TimeSpan AverageTimePerNumber { get; set; }

    /// <summary>
    /// Fastest number completion time
    /// </summary>
    public TimeSpan FastestTime { get; set; }

    public override ShareContent GenerateShareContent(Func<string, string> getString)
    {
        var message = getString("Share_FlashNumbers_Template")
            .Replace("{TotalNumbers}", TotalNumbers.ToString())
            .Replace("{NumberRange}", NumberRange)
            .Replace("{TotalDuration}", FormatDuration(Duration))
            .Replace("{AverageTime}", FormatDuration(AverageTimePerNumber))
            .Replace("{FastestTime}", FormatDuration(FastestTime))
            .Replace("{AppUrl}", "https://memoapp.com"); // TODO: Get from configuration

        return new ShareContent
        {
            Title = getString("Share_FlashNumbers_Title"),
            Message = message,
            ExerciseType = ExerciseType,
            AppUrl = "https://memoapp.com"
        };
    }

    private static string FormatDuration(TimeSpan duration)
    {
        if (duration.TotalSeconds < 1)
            return $"{duration.TotalMilliseconds:F0}ms";
        else if (duration.TotalMinutes < 1)
            return $"{duration.TotalSeconds:F1}s";
        else
            return $"{duration.TotalMinutes:F1}m";
    }
}

/// <summary>
/// Big Number exercise result for sharing
/// </summary>
public class BigNumberShareResult : ShareableResult
{
    /// <summary>
    /// Number of digits in the memorized number
    /// </summary>
    public int NumberOfDigits { get; set; }

    /// <summary>
    /// Maximum pair value used in the exercise
    /// </summary>
    public int MaxPairValue { get; set; }

    /// <summary>
    /// Memorization phase duration
    /// </summary>
    public TimeSpan MemorizationTime { get; set; }

    public override ShareContent GenerateShareContent(Func<string, string> getString)
    {
        var difficultyLevel = MaxPairValue switch
        {
            <= 9 => getString("Share_Difficulty_Beginner"),
            <= 99 => getString("Share_Difficulty_Intermediate"),
            _ => getString("Share_Difficulty_Advanced")
        };

        var message = getString("Share_BigNumber_Template")
            .Replace("{NumberOfDigits}", NumberOfDigits.ToString())
            .Replace("{MemorizationTime}", FormatDuration(MemorizationTime))
            .Replace("{DifficultyLevel}", difficultyLevel)
            .Replace("{AppUrl}", "https://memoapp.com"); // TODO: Get from configuration

        return new ShareContent
        {
            Title = getString("Share_BigNumber_Title"),
            Message = message,
            ExerciseType = ExerciseType,
            AppUrl = "https://memoapp.com"
        };
    }

    private static string FormatDuration(TimeSpan duration)
    {
        if (duration.TotalSeconds < 1)
            return $"{duration.TotalMilliseconds:F0}ms";
        else if (duration.TotalMinutes < 1)
            return $"{duration.TotalSeconds:F1}s";
        else
            return $"{duration.TotalMinutes:F1}m";
    }
}