namespace MemoApp.Core.Services.Analytics;

/// <summary>
/// Interface for analytics services providing event tracking, user properties, and screen views
/// </summary>
public interface IAnalyticsService
{
    /// <summary>
    /// Gets whether analytics tracking is currently enabled
    /// </summary>
    bool IsEnabled { get; }

    /// <summary>
    /// Enables or disables analytics tracking
    /// </summary>
    /// <param name="enabled">True to enable analytics, false to disable</param>
    Task SetAnalyticsEnabledAsync(bool enabled);

    /// <summary>
    /// Tracks a custom event with optional parameters
    /// </summary>
    /// <param name="eventName">The name of the event (snake_case, max 40 chars)</param>
    /// <param name="parameters">Optional event parameters (max 25 parameters)</param>
    Task TrackEventAsync(string eventName, IDictionary<string, object>? parameters = null);

    /// <summary>
    /// Sets a user property for analytics segmentation
    /// </summary>
    /// <param name="name">The property name (snake_case)</param>
    /// <param name="value">The property value (string, number, or boolean)</param>
    Task SetUserPropertyAsync(string name, object value);

    /// <summary>
    /// Tracks a screen view event
    /// </summary>
    /// <param name="screenName">The name of the screen</param>
    /// <param name="parameters">Optional screen parameters</param>
    Task TrackScreenViewAsync(string screenName, IDictionary<string, object>? parameters = null);

    /// <summary>
    /// Sets the user ID for analytics (for cross-session tracking)
    /// </summary>
    /// <param name="userId">Anonymous user identifier</param>
    Task SetUserIdAsync(string? userId);

    /// <summary>
    /// Logs app start event with platform and version information
    /// </summary>
    Task TrackAppStartAsync();

    /// <summary>
    /// Flushes any pending analytics data (useful before app shutdown)
    /// </summary>
    Task FlushAsync();
}