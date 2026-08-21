using System.Net;
using System.Net.Sockets;
using System.Security;
using MemoApp.Core.Services.ErrorMonitoring;
using MemoApp.Localization.Services;
using Microsoft.Extensions.Logging;

namespace MemoApp.UI.MauiApp.Services.ErrorHandling;

/// <summary>
/// Centralized exception handler that provides consistent error processing,
/// user messaging, and telemetry integration across the app
/// </summary>
public class ExceptionHandler : IExceptionHandler
{
    private readonly ILogger<ExceptionHandler> _logger;
    private readonly ILocalizationService _localizationService;
    private readonly IErrorMonitoringService _errorMonitoring;

    public ExceptionHandler(
        ILogger<ExceptionHandler> logger,
        ILocalizationService localizationService,
        IErrorMonitoringService errorMonitoring)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _localizationService = localizationService ?? throw new ArgumentNullException(nameof(localizationService));
        _errorMonitoring = errorMonitoring ?? throw new ArgumentNullException(nameof(errorMonitoring));
    }

    public async Task HandleExceptionAsync(Exception exception, string? context = null,
        Dictionary<string, object>? extras = null, bool showUserMessage = true)
    {
        var result = await HandleExceptionWithResultAsync(exception, context, extras);

        if (showUserMessage && !result.IsSuccess)
        {
            await ShowErrorAlertAsync(result.UserMessage);
        }
    }

    public async Task<ErrorResult> HandleExceptionWithResultAsync(Exception exception, string? context = null,
        Dictionary<string, object>? extras = null)
    {
        try
        {
            // Log the exception
            _logger.LogError(exception, "Exception in context: {Context}", context ?? "Unknown");

            // Categorize the exception
            var category = CategorizeException(exception);
            var errorCode = GenerateErrorCode(category, exception);

            // Get user-friendly message
            var userMessage = GetUserFriendlyMessage(exception, category);

            // Prepare context for error monitoring
            var errorContext = new ErrorContext
            {
                Operation = context ?? "unknown_operation",
                FeatureArea = ExtractFeatureArea(context),
                Properties = extras ?? new Dictionary<string, object>()
            };

            // Add exception categorization to context
            errorContext.Properties["exception_category"] = category.ToString();
            errorContext.Properties["exception_type"] = exception.GetType().Name;
            errorContext.Properties["error_code"] = errorCode;

            // Report to monitoring service (Sentry integration)
            await _errorMonitoring.LogCriticalErrorAsync(errorContext.Operation, exception, errorContext.Properties);

            return ErrorResult.Failure(userMessage, exception.Message, errorCode);
        }
        catch (Exception handlingException)
        {
            // If error handling itself fails, log and return basic error
            _logger.LogCritical(handlingException, "Failed to handle exception properly");

            var fallbackMessage = _localizationService.GetString("Error_General");
            return ErrorResult.Failure(fallbackMessage, handlingException.Message, "ERR_HANDLER_FAILURE");
        }
    }

    public async Task<bool> ExecuteWithRetryAsync(Func<Task> operation, string? context = null,
        int maxRetries = 3, int retryDelayMs = 1000)
    {
        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                await operation();
                return true;
            }
            catch (Exception ex) when (IsTransientException(ex) && attempt < maxRetries)
            {
                _logger.LogWarning(ex, "Transient failure in {Context}, attempt {Attempt}/{MaxRetries}",
                    context, attempt, maxRetries);

                AddBreadcrumb($"Retry attempt {attempt} for {context}", "retry", BreadcrumbLevel.Warning);
                await Task.Delay(retryDelayMs * attempt); // Exponential backoff
            }
            catch (Exception ex)
            {
                // Final attempt failed or non-transient exception
                await HandleExceptionAsync(ex, context, new Dictionary<string, object>
                {
                    { "retry_attempts", attempt },
                    { "max_retries", maxRetries }
                });
                return false;
            }
        }

        return false;
    }

    public async Task<T?> ExecuteWithHandlingAsync<T>(Func<Task<T>> operation, string? context = null,
        T? defaultValue = default)
    {
        try
        {
            return await operation();
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(ex, context);
            return defaultValue;
        }
    }

    public async Task ShowErrorAlertAsync(string message, string? title = null)
    {
        try
        {
            var alertTitle = title ?? _localizationService.GetString("Error_Title");
            var okButton = _localizationService.GetString("Common_OK");

            if (Application.Current?.MainPage != null)
            {
                await Application.Current.MainPage.DisplayAlert(alertTitle, message, okButton);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to show error alert: {Message}", message);
        }
    }

    public void AddBreadcrumb(string message, string? category = null, BreadcrumbLevel level = BreadcrumbLevel.Info)
    {
        try
        {
            // Breadcrumb tracking disabled - error monitoring service doesn't support it
            // _errorMonitoring.AddBreadcrumb(message, category ?? "general");

            // Also log locally for debugging
            var logLevel = level switch
            {
                BreadcrumbLevel.Debug => LogLevel.Debug,
                BreadcrumbLevel.Info => LogLevel.Information,
                BreadcrumbLevel.Warning => LogLevel.Warning,
                BreadcrumbLevel.Error => LogLevel.Error,
                BreadcrumbLevel.Fatal => LogLevel.Critical,
                _ => LogLevel.Information
            };

            _logger.Log(logLevel, "[{Category}] {Message}", category, message);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to add breadcrumb: {Message}", message);
        }
    }

    #region Private Helper Methods

    private static ExceptionCategory CategorizeException(Exception exception)
    {
        return exception switch
        {
            HttpRequestException => ExceptionCategory.Network,
            WebException => ExceptionCategory.Network,
            TaskCanceledException => ExceptionCategory.Network,
            TimeoutException => ExceptionCategory.Network,
            SocketException => ExceptionCategory.Network,

            UnauthorizedAccessException => ExceptionCategory.Authorization,
            SecurityException => ExceptionCategory.Authorization,

            FileNotFoundException => ExceptionCategory.Storage,
            DirectoryNotFoundException => ExceptionCategory.Storage,
            IOException => ExceptionCategory.Storage,

            ArgumentNullException => ExceptionCategory.Validation,
            ArgumentException => ExceptionCategory.Validation,
            FormatException => ExceptionCategory.Validation,

            InvalidOperationException => ExceptionCategory.Business,
            PlatformNotSupportedException => ExceptionCategory.System,
            NotSupportedException => ExceptionCategory.Business,
            OutOfMemoryException => ExceptionCategory.System,

            _ => ExceptionCategory.Unknown
        };
    }

    private string GetUserFriendlyMessage(Exception exception, ExceptionCategory category)
    {
        // Get localized message based on exception category and type
        var messageKey = category switch
        {
            ExceptionCategory.Network => GetNetworkErrorKey(exception),
            ExceptionCategory.Authorization => "Error_Authorization",
            ExceptionCategory.Storage => "Error_Storage",
            ExceptionCategory.Validation => "Error_Validation",
            ExceptionCategory.Business => "Error_Business",
            ExceptionCategory.External => "Error_External",
            ExceptionCategory.System => "Error_System",
            _ => "Error_General"
        };

        return _localizationService.GetString(messageKey);
    }

    private static string GetNetworkErrorKey(Exception exception)
    {
        return exception switch
        {
            TaskCanceledException => "Error_Network_Timeout",
            HttpRequestException httpEx when httpEx.Message.Contains("timeout") => "Error_Network_Timeout",
            HttpRequestException => "Error_Network_Connection",
            _ => "Error_Network_General"
        };
    }

    private static string GenerateErrorCode(ExceptionCategory category, Exception exception)
    {
        var categoryCode = category switch
        {
            ExceptionCategory.Network => "NET",
            ExceptionCategory.Authorization => "AUTH",
            ExceptionCategory.Storage => "STOR",
            ExceptionCategory.Validation => "VAL",
            ExceptionCategory.Business => "BIZ",
            ExceptionCategory.External => "EXT",
            ExceptionCategory.System => "SYS",
            _ => "UNK"
        };

        var hash = exception.GetType().Name.GetHashCode().ToString("X")[^4..];
        return $"ERR_{categoryCode}_{hash}";
    }

    private static string ExtractFeatureArea(string? context)
    {
        if (string.IsNullOrEmpty(context))
            return "unknown";

        // Extract feature area from context (e.g., "flash_numbers_load" -> "flash_numbers")
        var parts = context.Split('_');
        return parts.Length >= 2 ? $"{parts[0]}_{parts[1]}" : parts[0];
    }

    private static bool IsTransientException(Exception exception)
    {
        return exception switch
        {
            HttpRequestException => true,
            TaskCanceledException => true,
            TimeoutException => true,
            SocketException => true,
            IOException ioEx when ioEx.Message.Contains("timeout") => true,
            _ => false
        };
    }

    #endregion
    /// <inheritdoc />
    public void HandleException(Exception exception, string context = null,
        Dictionary<string, object>? extras = null, bool showUserMessage = false)
    {
        var reporting = HandleExceptionAsync(exception, context, extras, showUserMessage);

        if (reporting.IsCompleted)
        {
            // Observe a synchronous fault so it cannot resurface as an unobserved task exception.
            _ = reporting.Exception;
            return;
        }

        _ = reporting.ContinueWith(
            t => _logger.LogError(t.Exception, "Failed to report exception for context {Context}", context),
            CancellationToken.None,
            TaskContinuationOptions.OnlyOnFaulted,
            TaskScheduler.Default);
    }
}
