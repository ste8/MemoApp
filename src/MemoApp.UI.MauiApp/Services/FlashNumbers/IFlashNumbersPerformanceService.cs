using System;

namespace MemoApp.UI.MauiApp.Services.FlashNumbers;

/// <summary>
/// Service for managing Flash Numbers performance thresholds.
/// Provides centralized configuration for categorizing response times.
/// </summary>
public interface IFlashNumbersPerformanceService
{
    /// <summary>
    /// Gets the fast response threshold.
    /// Responses under this time are considered "fast".
    /// </summary>
    TimeSpan FastThreshold { get; }

    /// <summary>
    /// Gets the slow response threshold.
    /// Responses over this time are considered "slow".
    /// </summary>
    TimeSpan SlowThreshold { get; }

    /// <summary>
    /// Categorizes a response time into performance levels.
    /// </summary>
    /// <param name="responseTime">The response time to categorize</param>
    /// <returns>The performance category (Fast, Medium, or Slow)</returns>
    PerformanceCategory CategorizeResponseTime(TimeSpan responseTime);

    /// <summary>
    /// Gets the localized display text for a performance category's threshold.
    /// </summary>
    /// <param name="category">The performance category</param>
    /// <returns>Localized threshold text (e.g., "< 1.0s")</returns>
    string GetThresholdDisplayText(PerformanceCategory category);
}

/// <summary>
/// Performance categories for Flash Numbers response times.
/// </summary>
public enum PerformanceCategory
{
    /// <summary>
    /// Fast response (expert level)
    /// </summary>
    Fast,

    /// <summary>
    /// Medium response (developing proficiency)
    /// </summary>
    Medium,

    /// <summary>
    /// Slow response (needs practice)
    /// </summary>
    Slow
}