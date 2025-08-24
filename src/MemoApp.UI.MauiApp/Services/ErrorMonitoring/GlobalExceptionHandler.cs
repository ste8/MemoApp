using System;
using System.Collections.Generic;
using MemoApp.Core.Services.ErrorMonitoring;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.Controls;

namespace MemoApp.UI.MauiApp.Services.ErrorMonitoring;

/// <summary>
/// Global exception handler for capturing all unhandled exceptions
/// </summary>
public class GlobalExceptionHandler
{
    private readonly IErrorMonitoringService _errorService;
    private readonly ILogger<GlobalExceptionHandler> _logger;
    private DateTime _sessionStartTime;
    private string? _currentPage;

    public GlobalExceptionHandler(IErrorMonitoringService errorService, ILogger<GlobalExceptionHandler> logger)
    {
        _errorService = errorService;
        _logger = logger;
        _sessionStartTime = DateTime.UtcNow;
    }

    public void Initialize()
    {
        try
        {
            // Capture all unhandled exceptions at domain level
            AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
            
            // Note: Platform-specific exception handling should be added per platform if needed
            // For now, AppDomain.UnhandledException will capture most unhandled exceptions
            
            // Track navigation for crash context
            if (Application.Current != null)
            {
                Application.Current.PageAppearing += OnPageAppearing;
            }
            
            _logger.LogInformation("Global exception handler initialized");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize global exception handler");
        }
    }

    private async void OnUnhandledException(object? sender, UnhandledExceptionEventArgs e)
    {
        try
        {
            if (e.ExceptionObject is Exception exception)
            {
                var context = new Dictionary<string, object>
                {
                    ["is_terminating"] = e.IsTerminating,
                    ["session_duration_minutes"] = (DateTime.UtcNow - _sessionStartTime).TotalMinutes,
                    ["crash_location"] = _currentPage ?? "unknown",
                    ["exception_type"] = exception.GetType().Name,
                    ["platform"] = DeviceInfo.Platform.ToString()
                };

                await _errorService.LogCriticalErrorAsync(
                    CriticalErrorTypes.UnhandledException,
                    exception,
                    context);
                
                _logger.LogCritical(exception, "Unhandled exception captured. Terminating: {IsTerminating}", e.IsTerminating);
            }
        }
        catch (Exception ex)
        {
            // Last resort logging
            _logger.LogError(ex, "Failed to log unhandled exception");
        }
    }


    private void OnPageAppearing(object? sender, Page e)
    {
        try
        {
            _currentPage = e?.GetType().Name ?? "unknown";
            _logger.LogDebug("Current page updated: {PageName}", _currentPage);
        }
        catch
        {
            // Ignore errors in page tracking
        }
    }

    private string DetermineUserAction(string? pageName)
    {
        return pageName switch
        {
            "FlashNumbersPage" => "training_exercise",
            "AssociationsPage" => "viewing_associations",
            "PackDownloadPage" => "downloading_pack",
            "SettingsPage" => "changing_settings",
            "NumberMemorizationPage" => "memory_exercise",
            "MainPage" => "main_navigation",
            _ => "unknown_action"
        };
    }

    private bool CanRecover(Exception exception)
    {
        // Don't try to recover from critical exceptions
        return exception switch
        {
            OutOfMemoryException => false,
            StackOverflowException => false,
            AccessViolationException => false,
            _ when exception.Message.Contains("fatal", StringComparison.OrdinalIgnoreCase) => false,
            _ => true // Try to recover from other exceptions
        };
    }

    /// <summary>
    /// Helper method to track navigation failures
    /// </summary>
    public async void LogNavigationFailure(string fromPage, string toPage, Exception exception, string trigger = "user_tap")
    {
        try
        {
            var context = new Dictionary<string, object>
            {
                ["from_page"] = fromPage,
                ["to_page"] = toPage,
                ["navigation_trigger"] = trigger,
                ["exception_type"] = exception.GetType().Name
            };

            await _errorService.LogCriticalErrorAsync(
                CriticalErrorTypes.NavigationFailure,
                exception,
                context);
            
            _logger.LogError(exception, "Navigation failed from {FromPage} to {ToPage}", fromPage, toPage);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log navigation failure");
        }
    }

    /// <summary>
    /// Helper method to track app crashes with context
    /// </summary>
    public async void LogAppCrash(Exception exception, string crashLocation, string userAction)
    {
        try
        {
            var context = new Dictionary<string, object>
            {
                ["crash_location"] = crashLocation,
                ["user_action"] = userAction,
                ["session_duration_minutes"] = (DateTime.UtcNow - _sessionStartTime).TotalMinutes,
                ["memory_usage_mb"] = GC.GetTotalMemory(false) / (1024 * 1024),
                ["platform"] = DeviceInfo.Platform.ToString()
            };

            await _errorService.LogCriticalErrorAsync(
                CriticalErrorTypes.AppCrash,
                exception,
                context);
            
            _logger.LogCritical(exception, "App crash at {CrashLocation} during {UserAction}", crashLocation, userAction);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log app crash");
        }
    }
}