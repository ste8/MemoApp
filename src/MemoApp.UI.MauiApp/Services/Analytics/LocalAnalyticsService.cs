using MemoApp.Core.Services.Analytics;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace MemoApp.UI.MauiApp.Services.Analytics;

/// <summary>
/// Local analytics implementation for Windows and macOS platforms
/// Provides analytics functionality without Firebase dependency
/// </summary>
public class LocalAnalyticsService : IAnalyticsService
{
    private readonly ILogger<LocalAnalyticsService> _logger;
    private bool _isEnabled;

    public LocalAnalyticsService(ILogger<LocalAnalyticsService> logger)
    {
        _logger = logger;
        _isEnabled = GetAnalyticsEnabledFromPreferences();
    }

    public bool IsEnabled => _isEnabled;

    public async Task SetAnalyticsEnabledAsync(bool enabled)
    {
        _isEnabled = enabled;
        
        try
        {
            Preferences.Set("analytics_enabled", enabled);
            _logger.LogInformation("Local analytics collection {Status}", enabled ? "enabled" : "disabled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save analytics preference");
        }

        await Task.CompletedTask;
    }

    public async Task TrackEventAsync(string eventName, IDictionary<string, object>? parameters = null)
    {
        if (!_isEnabled)
        {
            return;
        }

        try
        {
            var eventData = new
            {
                Timestamp = DateTimeOffset.UtcNow,
                EventName = eventName,
                Parameters = parameters ?? new Dictionary<string, object>(),
                Platform = GetCurrentPlatform(),
                AppVersion = GetAppVersion(),
                SessionId = GetSessionId()
            };

            var json = JsonSerializer.Serialize(eventData, new JsonSerializerOptions 
            { 
                WriteIndented = true 
            });

            _logger.LogInformation("Analytics Event Tracked: {EventName}", eventName);
            _logger.LogDebug("Event Details: {EventJson}", json);

            // In a production app, you might want to:
            // - Store events locally and batch upload them
            // - Send to a custom analytics endpoint
            // - Use Application Insights or another desktop analytics solution
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to track event: {EventName}", eventName);
        }

        await Task.CompletedTask;
    }

    public async Task SetUserPropertyAsync(string name, object value)
    {
        if (!_isEnabled)
        {
            return;
        }

        try
        {
            var propertyKey = $"user_property_{name}";
            var stringValue = value?.ToString() ?? string.Empty;
            
            Preferences.Set(propertyKey, stringValue);
            
            _logger.LogInformation("User Property Set: {PropertyName} = {PropertyValue}", name, stringValue);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to set user property: {PropertyName}", name);
        }

        await Task.CompletedTask;
    }

    public async Task TrackScreenViewAsync(string screenName, IDictionary<string, object>? parameters = null)
    {
        if (!_isEnabled)
        {
            return;
        }

        var screenParameters = new Dictionary<string, object>
        {
            ["screen_name"] = screenName,
            ["screen_class"] = screenName
        };

        if (parameters != null)
        {
            foreach (var param in parameters)
            {
                screenParameters[param.Key] = param.Value;
            }
        }

        await TrackEventAsync("screen_view", screenParameters);
    }

    public async Task SetUserIdAsync(string? userId)
    {
        if (!_isEnabled)
        {
            return;
        }

        try
        {
            if (!string.IsNullOrEmpty(userId))
            {
                Preferences.Set("analytics_user_id", userId);
                _logger.LogInformation("User ID Set: {UserId}", "***"); // Don't log actual ID for privacy
            }
            else
            {
                Preferences.Remove("analytics_user_id");
                _logger.LogInformation("User ID Cleared");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to set user ID");
        }

        await Task.CompletedTask;
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
            _logger.LogDebug("Local analytics flush requested (no-op for local service)");
            
            // In a production app, this is where you might:
            // - Upload queued events to a server
            // - Save pending events to persistent storage
            // - Clear temporary analytics data
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to flush analytics");
        }

        await Task.CompletedTask;
    }

    #region Private Methods

    private bool GetAnalyticsEnabledFromPreferences()
    {
        try
        {
            return Preferences.Get("analytics_enabled", true); // Default to enabled
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to read analytics preference, defaulting to enabled");
            return true;
        }
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

    private string GetSessionId()
    {
        try
        {
            const string sessionKey = "current_session_id";
            
            if (!Preferences.ContainsKey(sessionKey))
            {
                var newSessionId = Guid.NewGuid().ToString("N")[..8]; // Short session ID
                Preferences.Set(sessionKey, newSessionId);
                return newSessionId;
            }

            return Preferences.Get(sessionKey, "unknown");
        }
        catch
        {
            return "unknown";
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