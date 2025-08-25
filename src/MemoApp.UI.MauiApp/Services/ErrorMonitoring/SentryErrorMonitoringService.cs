using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MemoApp.Core.Services.ErrorMonitoring;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Sentry;

namespace MemoApp.UI.MauiApp.Services.ErrorMonitoring;

/// <summary>
/// Sentry implementation of error monitoring focusing on critical errors only
/// </summary>
public class SentryErrorMonitoringService : IErrorMonitoringService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<SentryErrorMonitoringService> _logger;
    private bool _isEnabled;
    
    // Pattern-based error tracking to avoid noise
    private readonly Dictionary<string, DateTime> _lastErrorTime = new();
    private readonly Dictionary<string, int> _errorCounts = new();
    private const int ERROR_THRESHOLD = 5; // Log after 5 occurrences
    private const int RATE_LIMIT_MINUTES = 5; // Rate limit per error type

    public SentryErrorMonitoringService(IConfiguration configuration, ILogger<SentryErrorMonitoringService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task InitializeAsync()
    {
        try
        {
            var dsn = _configuration["Sentry:Dsn"];
            _logger.LogDebug("Checking Sentry DSN configuration: '{Dsn}' (Length: {Length})", 
                dsn ?? "NULL", dsn?.Length ?? 0);
            
            if (string.IsNullOrEmpty(dsn))
            {
                _logger.LogWarning("Sentry DSN not configured. Error monitoring disabled.");
                return;
            }

            // Check if Sentry is already initialized (by MAUI integration)
            if (SentrySdk.IsEnabled)
            {
                _logger.LogInformation("Sentry SDK already initialized by MAUI integration");
                _isEnabled = true;
            }
            else
            {
                _logger.LogWarning("Sentry SDK not initialized. Error monitoring disabled.");
                return;
            }
            
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize error monitoring service");
            _isEnabled = false;
        }
    }

    public async Task LogCriticalErrorAsync(string errorType, Exception exception, Dictionary<string, object>? context = null)
    {
        if (!_isEnabled)
        {
            _logger.LogError(exception, "Critical error (monitoring disabled): {ErrorType}", errorType);
            return;
        }

        // Check rate limiting
        if (IsRateLimited(errorType))
        {
            _logger.LogDebug("Rate limited error type: {ErrorType}", errorType);
            return;
        }

        try
        {
            // Configure scope and capture exception
            SentrySdk.ConfigureScope(scope =>
            {
                // Set error type tag for grouping
                scope.SetTag("error_type", errorType);
                scope.SetTag("critical", "true");
                scope.Level = SentryLevel.Error;
                
                // Add context data
                if (context != null)
                {
                    foreach (var kvp in context)
                    {
                        scope.SetExtra(kvp.Key, kvp.Value);
                    }
                }
                
                // Add device and app context
                AddDeviceContext(scope);
            });
            
            // Capture the exception
            SentrySdk.CaptureException(exception);
            
            _logger.LogInformation("Critical error logged to Sentry: {ErrorType}", errorType);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log critical error to Sentry");
        }
        
        await Task.CompletedTask;
    }

    public async Task LogPerformanceIssueAsync(string issueType, Dictionary<string, object>? context = null)
    {
        if (!_isEnabled)
        {
            _logger.LogWarning("Performance issue (monitoring disabled): {IssueType}", issueType);
            return;
        }

        // Only log severe performance issues
        if (!IsSeverePerformanceIssue(issueType, context))
        {
            _logger.LogDebug("Performance issue below threshold: {IssueType}", issueType);
            return;
        }

        try
        {
            var transaction = SentrySdk.StartTransaction($"performance.{issueType}", issueType);
            transaction.Status = SpanStatus.DeadlineExceeded;
            
            if (context != null)
            {
                foreach (var kvp in context)
                {
                    transaction.SetExtra(kvp.Key, kvp.Value);
                }
            }
            
            transaction.Finish();
            
            _logger.LogInformation("Performance issue logged to Sentry: {IssueType}", issueType);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log performance issue to Sentry");
        }
        
        await Task.CompletedTask;
    }

    public async Task SetUserContextAsync(object userContext)
    {
        if (!_isEnabled)
            return;

        try
        {
            // Extract minimal user context
            var contextDict = userContext as Dictionary<string, object> ?? 
                             ConvertToDict(userContext);
            
            var user = new SentryUser
            {
                Id = contextDict.TryGetValue("session_id", out var sessionId) ? 
                    sessionId?.ToString() : Guid.NewGuid().ToString()
            };
            
            // Add other context as user data
            if (contextDict != null)
            {
                user.Other = new Dictionary<string, string>();
                foreach (var kvp in contextDict)
                {
                    if (kvp.Key != "session_id" && kvp.Value != null)
                    {
                        user.Other[kvp.Key] = kvp.Value.ToString() ?? string.Empty;
                    }
                }
            }
            
            SentrySdk.ConfigureScope(scope => scope.User = user);
            
            _logger.LogDebug("User context set for error monitoring");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to set user context");
        }
        
        await Task.CompletedTask;
    }

    public async Task EnableErrorReportingAsync(bool enabled)
    {
        _isEnabled = enabled;
        
        if (!enabled)
        {
            await SentrySdk.FlushAsync(TimeSpan.FromSeconds(2));
            SentrySdk.Close();
            _logger.LogInformation("Error reporting disabled");
        }
        else
        {
            await InitializeAsync();
        }
    }

    public async Task<bool> IsErrorReportingEnabledAsync()
    {
        return await Task.FromResult(_isEnabled);
    }

    // Helper methods for pattern-based logging

    private bool IsRateLimited(string errorType)
    {
        var now = DateTime.UtcNow;
        
        if (_lastErrorTime.TryGetValue(errorType, out var lastTime))
        {
            if ((now - lastTime).TotalMinutes < RATE_LIMIT_MINUTES)
            {
                // Within rate limit window
                _errorCounts.TryGetValue(errorType, out var count);
                _errorCounts[errorType] = count + 1;
                
                // Only log if we've hit the threshold
                if (_errorCounts[errorType] == ERROR_THRESHOLD)
                {
                    _lastErrorTime[errorType] = now;
                    _errorCounts[errorType] = 0;
                    return false; // Log this occurrence
                }
                
                return true; // Rate limited
            }
        }
        
        // First occurrence or outside window
        _lastErrorTime[errorType] = now;
        _errorCounts[errorType] = 1;
        return false; // Log it
    }

    private bool IsSeverePerformanceIssue(string issueType, Dictionary<string, object>? context)
    {
        if (context == null)
            return false;

        return issueType switch
        {
            PerformanceIssueTypes.AppStartupSlow => 
                context.TryGetValue("startup_time_ms", out var startTime) && 
                Convert.ToInt64(startTime) > 10000, // > 10 seconds
            
            PerformanceIssueTypes.ExerciseLoadTimeout => 
                context.TryGetValue("timeout_duration_ms", out var loadTime) && 
                Convert.ToInt64(loadTime) > 15000, // > 15 seconds
            
            PerformanceIssueTypes.MemoryCritical => 
                context.TryGetValue("memory_pressure_ratio", out var ratio) && 
                Convert.ToDouble(ratio) > 0.9, // > 90% memory used
            
            _ => true // Log other performance issues by default
        };
    }

    private void AddDeviceContext(Scope scope)
    {
        scope.SetTag("platform", DeviceInfo.Platform.ToString());
        scope.SetTag("device_model", DeviceInfo.Model);
        scope.SetTag("os_version", DeviceInfo.VersionString);
        scope.SetTag("app_version", AppInfo.VersionString);
        scope.SetTag("app_build", AppInfo.BuildString);
        
        // Add current memory usage if available
        if (GC.GetTotalMemory(false) is var memoryBytes && memoryBytes > 0)
        {
            scope.SetExtra("memory_usage_mb", memoryBytes / (1024 * 1024));
        }
    }


    private Dictionary<string, object> ConvertToDict(object obj)
    {
        var dict = new Dictionary<string, object>();
        var type = obj.GetType();
        
        foreach (var prop in type.GetProperties())
        {
            try
            {
                var value = prop.GetValue(obj);
                if (value != null)
                {
                    dict[prop.Name] = value;
                }
            }
            catch
            {
                // Skip properties that can't be read
            }
        }
        
        return dict;
    }
}