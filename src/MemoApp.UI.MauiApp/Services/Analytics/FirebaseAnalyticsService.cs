using MemoApp.Core.Services.Analytics;
using Microsoft.Extensions.Logging;
using Plugin.Firebase.Analytics;

namespace MemoApp.UI.MauiApp.Services.Analytics;

/// <summary>
/// Firebase Analytics implementation for iOS and Android platforms
/// </summary>
public class FirebaseAnalyticsService : IAnalyticsService
{
    private readonly ILogger<FirebaseAnalyticsService> _logger;
    private readonly SemaphoreSlim _initializationSemaphore;
    private bool _isInitialized;
    private bool _isEnabled;

    public FirebaseAnalyticsService(ILogger<FirebaseAnalyticsService> logger)
    {
        _logger = logger;
        _initializationSemaphore = new SemaphoreSlim(1, 1);
        
        // Load analytics preference from storage
        _isEnabled = Preferences.Get("analytics_enabled", true); // Default to enabled
    }

    public bool IsEnabled => _isEnabled;

    public async Task SetAnalyticsEnabledAsync(bool enabled)
    {
        try
        {
            await EnsureInitializedAsync();
            
            // Note: Plugin.Firebase v3.x doesn't expose SetAnalyticsCollectionEnabled directly
            // We track the state locally and skip events when disabled
            _isEnabled = enabled;
            
            // Store preference for persistence across app restarts
            Preferences.Set("analytics_enabled", enabled);
            
            _logger.LogInformation("Firebase Analytics collection {Status}", enabled ? "enabled" : "disabled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to set analytics collection status to {Status}", enabled);
        }
    }

    public async Task TrackEventAsync(string eventName, IDictionary<string, object>? parameters = null)
    {
        if (!_isEnabled)
        {
            _logger.LogDebug("Analytics tracking is disabled, skipping event: {EventName}", eventName);
            return;
        }

        try
        {
            await EnsureInitializedAsync();

            var sanitizedEventName = SanitizeEventName(eventName);
            var sanitizedParameters = SanitizeParameters(parameters);

            // Check if Firebase Analytics is available
            if (CrossFirebaseAnalytics.Current == null)
            {
                _logger.LogWarning("Firebase Analytics is not available, skipping event: {EventName}", sanitizedEventName);
                return;
            }

            try
            {
                if (sanitizedParameters?.Any() == true)
                {
                    CrossFirebaseAnalytics.Current.LogEvent(sanitizedEventName, sanitizedParameters);
                    _logger.LogDebug("Tracked event: {EventName} with {ParameterCount} parameters", 
                        sanitizedEventName, sanitizedParameters.Count);
                }
                else
                {
                    CrossFirebaseAnalytics.Current.LogEvent(sanitizedEventName, new Dictionary<string, object>());
                    _logger.LogDebug("Tracked event: {EventName} (no parameters)", sanitizedEventName);
                }
            }
            catch (NullReferenceException ex)
            {
                _logger.LogWarning(ex, "Firebase Analytics not properly initialized, skipping event: {EventName}", sanitizedEventName);
                return;
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Firebase Analytics invalid operation for event: {EventName}, continuing without analytics", sanitizedEventName);
                return;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Firebase Analytics error for event: {EventName}, continuing without analytics", sanitizedEventName);
                return;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to track event: {EventName}", eventName);
        }
    }

    public async Task SetUserPropertyAsync(string name, object value)
    {
        if (!_isEnabled)
        {
            _logger.LogDebug("Analytics tracking is disabled, skipping user property: {PropertyName}", name);
            return;
        }

        try
        {
            await EnsureInitializedAsync();

            var sanitizedName = SanitizePropertyName(name);
            var stringValue = SanitizePropertyValue(value);

            if (CrossFirebaseAnalytics.Current == null)
            {
                _logger.LogWarning("Firebase Analytics is not available, skipping user property: {PropertyName}", sanitizedName);
                return;
            }

            try
            {
                CrossFirebaseAnalytics.Current.SetUserProperty(sanitizedName, stringValue);
            }
            catch (NullReferenceException ex)
            {
                _logger.LogWarning(ex, "Firebase Analytics not properly initialized, skipping user property: {PropertyName}", sanitizedName);
                return;
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Firebase Analytics invalid operation for user property: {PropertyName}, continuing without analytics", sanitizedName);
                return;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Firebase Analytics error for user property: {PropertyName}, continuing without analytics", sanitizedName);
                return;
            }
            _logger.LogDebug("Set user property: {PropertyName} = {PropertyValue}", sanitizedName, stringValue);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to set user property: {PropertyName}", name);
        }
    }

    public async Task TrackScreenViewAsync(string screenName, IDictionary<string, object>? parameters = null)
    {
        if (!_isEnabled)
        {
            _logger.LogDebug("Analytics tracking is disabled, skipping screen view: {ScreenName}", screenName);
            return;
        }

        try
        {
            await EnsureInitializedAsync();

            var screenParameters = new Dictionary<string, object>
            {
                ["screen_name"] = screenName,
                ["screen_class"] = screenName // Assuming screen name matches class name
            };

            // Add any additional parameters
            if (parameters != null)
            {
                foreach (var param in parameters)
                {
                    if (!screenParameters.ContainsKey(param.Key))
                    {
                        screenParameters[param.Key] = param.Value;
                    }
                }
            }

            var sanitizedParameters = SanitizeParameters(screenParameters);
            
            if (CrossFirebaseAnalytics.Current == null)
            {
                _logger.LogWarning("Firebase Analytics is not available, skipping screen view: {ScreenName}", screenName);
                return;
            }
            
            try
            {
                CrossFirebaseAnalytics.Current.LogEvent("screen_view", sanitizedParameters);
            }
            catch (NullReferenceException ex)
            {
                _logger.LogWarning(ex, "Firebase Analytics not properly initialized, skipping screen view: {ScreenName}", screenName);
                return;
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Firebase Analytics invalid operation for screen view: {ScreenName}, continuing without analytics", screenName);
                return;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Firebase Analytics error for screen view: {ScreenName}, continuing without analytics", screenName);
                return;
            }
            
            _logger.LogDebug("Tracked screen view: {ScreenName}", screenName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to track screen view: {ScreenName}", screenName);
        }
    }

    public async Task SetUserIdAsync(string? userId)
    {
        if (!_isEnabled)
        {
            _logger.LogDebug("Analytics tracking is disabled, skipping user ID set");
            return;
        }

        try
        {
            await EnsureInitializedAsync();
            
            if (CrossFirebaseAnalytics.Current == null)
            {
                _logger.LogWarning("Firebase Analytics is not available, skipping set user ID");
                return;
            }
            
            try
            {
                CrossFirebaseAnalytics.Current.SetUserId(userId);
            }
            catch (NullReferenceException ex)
            {
                _logger.LogWarning(ex, "Firebase Analytics not properly initialized, skipping set user ID");
                return;
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Firebase Analytics invalid operation for set user ID, continuing without analytics");
                return;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Firebase Analytics error for set user ID, continuing without analytics");
                return;
            }
            
            _logger.LogDebug("Set user ID: {UserId}", !string.IsNullOrEmpty(userId) ? "***" : "null");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to set user ID");
        }
    }

    public async Task TrackAppStartAsync()
    {
        try
        {
            var parameters = new Dictionary<string, object>
            {
                [AnalyticsParameters.Platform] = GetCurrentPlatform(),
                [AnalyticsParameters.AppVersion] = GetAppVersion(),
                [AnalyticsParameters.UILanguage] = GetCurrentLanguage(),
                [AnalyticsParameters.FirstLaunch] = await IsFirstLaunchAsync()
            };

            await TrackEventAsync(AnalyticsEvents.AppStart, parameters);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to track app start event");
        }
    }

    public async Task FlushAsync()
    {
        try
        {
            // Firebase Analytics automatically handles data uploading, but we can ensure initialization
            await EnsureInitializedAsync();
            _logger.LogDebug("Analytics flush requested (Firebase handles uploads automatically)");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to flush analytics");
        }
    }

    #region Private Methods

    private async Task EnsureInitializedAsync()
    {
        if (_isInitialized)
            return;

        await _initializationSemaphore.WaitAsync();
        try
        {
            if (_isInitialized)
                return;

            // Firebase should be initialized in platform-specific code (MainActivity/AppDelegate)
            // We just mark as initialized here
            _isInitialized = true;
            _logger.LogInformation("Firebase Analytics service initialized");
        }
        finally
        {
            _initializationSemaphore.Release();
        }
    }

    private string SanitizeEventName(string eventName)
    {
        if (string.IsNullOrWhiteSpace(eventName))
            throw new ArgumentException("Event name cannot be null or empty", nameof(eventName));

        // Firebase Analytics event name requirements:
        // - Max 40 characters
        // - Letters, digits, and underscores only
        // - Must start with letter
        var sanitized = eventName.ToLowerInvariant()
            .Replace('-', '_')
            .Replace(' ', '_');

        // Remove invalid characters
        sanitized = string.Concat(sanitized.Where(c => char.IsLetterOrDigit(c) || c == '_'));

        // Ensure starts with letter
        if (sanitized.Length > 0 && !char.IsLetter(sanitized[0]))
        {
            sanitized = "event_" + sanitized;
        }

        // Limit to 40 characters
        if (sanitized.Length > 40)
        {
            sanitized = sanitized.Substring(0, 40);
        }

        if (string.IsNullOrEmpty(sanitized))
        {
            sanitized = "unknown_event";
        }

        return sanitized;
    }

    private string SanitizePropertyName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Property name cannot be null or empty", nameof(name));

        // Similar to event names but for user properties
        var sanitized = name.ToLowerInvariant()
            .Replace('-', '_')
            .Replace(' ', '_');

        sanitized = string.Concat(sanitized.Where(c => char.IsLetterOrDigit(c) || c == '_'));

        if (sanitized.Length > 0 && !char.IsLetter(sanitized[0]))
        {
            sanitized = "property_" + sanitized;
        }

        if (sanitized.Length > 24) // User properties have a 24 character limit
        {
            sanitized = sanitized.Substring(0, 24);
        }

        if (string.IsNullOrEmpty(sanitized))
        {
            sanitized = "unknown_property";
        }

        return sanitized;
    }

    private string SanitizePropertyValue(object value)
    {
        if (value == null)
            return string.Empty;

        var stringValue = value.ToString() ?? string.Empty;
        
        // Firebase user property values are limited to 36 characters
        if (stringValue.Length > 36)
        {
            stringValue = stringValue.Substring(0, 36);
        }

        return stringValue;
    }

    private IDictionary<string, object>? SanitizeParameters(IDictionary<string, object>? parameters)
    {
        if (parameters == null || !parameters.Any())
            return null;

        var sanitized = new Dictionary<string, object>();
        var parameterCount = 0;

        foreach (var kvp in parameters)
        {
            // Firebase Analytics limit: 25 parameters per event
            if (parameterCount >= 25)
            {
                _logger.LogWarning("Exceeded maximum parameter count (25) for Firebase event, truncating");
                break;
            }

            if (string.IsNullOrWhiteSpace(kvp.Key))
                continue;

            var sanitizedKey = SanitizeParameterName(kvp.Key);
            var sanitizedValue = SanitizeParameterValue(kvp.Value);

            if (!string.IsNullOrEmpty(sanitizedKey) && sanitizedValue != null)
            {
                sanitized[sanitizedKey] = sanitizedValue;
                parameterCount++;
            }
        }

        return sanitized.Any() ? sanitized : null;
    }

    private string SanitizeParameterName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return string.Empty;

        var sanitized = name.ToLowerInvariant()
            .Replace('-', '_')
            .Replace(' ', '_');

        sanitized = string.Concat(sanitized.Where(c => char.IsLetterOrDigit(c) || c == '_'));

        if (sanitized.Length > 0 && !char.IsLetter(sanitized[0]))
        {
            sanitized = "param_" + sanitized;
        }

        if (sanitized.Length > 40)
        {
            sanitized = sanitized.Substring(0, 40);
        }

        return sanitized;
    }

    private object? SanitizeParameterValue(object value)
    {
        if (value == null)
            return null;

        return value switch
        {
            string stringValue when stringValue.Length > 100 => stringValue.Substring(0, 100), // Firebase param limit
            string stringValue => stringValue,
            bool boolValue => boolValue,
            int intValue => intValue,
            long longValue => longValue,
            double doubleValue => doubleValue,
            float floatValue => (double)floatValue,
            _ => value.ToString()?.Substring(0, Math.Min(100, value.ToString()?.Length ?? 0))
        };
    }

    private string GetCurrentPlatform()
    {
        return DeviceInfo.Platform.ToString();
    }

    private string GetAppVersion()
    {
        return AppInfo.VersionString;
    }

    private string GetCurrentLanguage()
    {
        try
        {
            return System.Globalization.CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
        }
        catch
        {
            return "en";
        }
    }

    private async Task<bool> IsFirstLaunchAsync()
    {
        try
        {
            return !Preferences.ContainsKey("app_launched_before");
        }
        catch
        {
            return false;
        }
        finally
        {
            // Mark that the app has been launched
            try
            {
                Preferences.Set("app_launched_before", true);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to set first launch preference");
            }
        }
    }

    #endregion
}