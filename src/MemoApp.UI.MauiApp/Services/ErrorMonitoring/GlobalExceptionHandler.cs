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
    private static readonly TimeSpan ReportFlushTimeout = TimeSpan.FromSeconds(2);
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

    private void OnUnhandledException(object? sender, UnhandledExceptionEventArgs e)
    {
        try
        {
            if (e.ExceptionObject is not Exception exception)
            {
                return;
            }

            var context = new Dictionary<string, object>
            {
                ["is_terminating"] = e.IsTerminating,
                ["session_duration_minutes"] = (DateTime.UtcNow - _sessionStartTime).TotalMinutes,
                ["crash_location"] = _currentPage ?? "unknown",
                ["exception_type"] = exception.GetType().Name,
                ["platform"] = DeviceInfo.Platform.ToString()
            };

            var reporting = _errorService.LogCriticalErrorAsync(
                CriticalErrorTypes.UnhandledException,
                exception,
                context);

            _logger.LogCritical(exception, "Unhandled exception captured. Terminating: {IsTerminating}", e.IsTerminating);

            if (e.IsTerminating)
            {
                // Deliberate sync-over-async, and the one place it is warranted: the process
                // is about to exit, so awaiting would hand control back and the report would
                // never leave the device. Bounded so a hung network cannot stall shutdown.
                reporting.Wait(ReportFlushTimeout);
            }
            else
            {
                // Keep a fire-and-forget report from becoming an unobserved task exception.
                _ = reporting.ContinueWith(
                    t => _logger.LogError(t.Exception, "Failed to report unhandled exception"),
                    CancellationToken.None,
                    TaskContinuationOptions.OnlyOnFaulted,
                    TaskScheduler.Default);
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
            "FlashNumbersPage" => "flash_numbers_exercise",
            "AssociationsPage" => "viewing_associations",
            "PackDownloadPage" => "downloading_pack",
            "SettingsPage" => "changing_settings",
            "BigNumberPage" => "big_number_exercise",
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

}