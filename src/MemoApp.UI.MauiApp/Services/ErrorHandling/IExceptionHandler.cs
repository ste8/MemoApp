using MemoApp.Core.Models;

namespace MemoApp.UI.MauiApp.Services.ErrorHandling;

/// <summary>
/// Centralized exception handling service for consistent error processing,
/// user messaging, and telemetry integration across the app
/// </summary>
public interface IExceptionHandler
{
    /// <summary>
    /// Handle an exception with context and show appropriate user message
    /// </summary>
    /// <param name="exception">The exception to handle</param>
    /// <param name="context">Context about what operation failed</param>
    /// <param name="extras">Additional context data for debugging</param>
    /// <param name="showUserMessage">Whether to show a user-facing error message</param>
    Task HandleExceptionAsync(Exception exception, string context = null,
        Dictionary<string, object>? extras = null, bool showUserMessage = true);

    /// <summary>
    /// Handle an exception and return a result indicating success/failure
    /// </summary>
    /// <param name="exception">The exception to handle</param>
    /// <param name="context">Context about what operation failed</param>
    /// <param name="extras">Additional context data for debugging</param>
    /// <returns>ErrorResult with user message and technical details</returns>
    Task<ErrorResult> HandleExceptionWithResultAsync(Exception exception, string context = null,
        Dictionary<string, object>? extras = null);

    /// <summary>
    /// Execute an operation with automatic exception handling and retry logic
    /// </summary>
    /// <param name="operation">The operation to execute</param>
    /// <param name="context">Context for error reporting</param>
    /// <param name="maxRetries">Maximum retry attempts for transient failures</param>
    /// <param name="retryDelayMs">Delay between retries in milliseconds</param>
    Task<bool> ExecuteWithRetryAsync(Func<Task> operation, string context = null,
        int maxRetries = 3, int retryDelayMs = 1000);

    /// <summary>
    /// Execute an operation with automatic exception handling and return result
    /// </summary>
    /// <typeparam name="T">Return type</typeparam>
    /// <param name="operation">The operation to execute</param>
    /// <param name="context">Context for error reporting</param>
    /// <param name="defaultValue">Default value to return on error</param>
    Task<T?> ExecuteWithHandlingAsync<T>(Func<Task<T>> operation, string context = null,
        T? defaultValue = default);

    /// <summary>
    /// Show an error alert to the user
    /// </summary>
    /// <param name="message">The message to display</param>
    /// <param name="title">Alert title</param>
    Task ShowErrorAlertAsync(string message, string? title = null);

    /// <summary>
    /// Add a breadcrumb for debugging context
    /// </summary>
    /// <param name="message">Breadcrumb message</param>
    /// <param name="category">Category for organization</param>
    /// <param name="level">Severity level</param>
    void AddBreadcrumb(string message, string? category = null, BreadcrumbLevel level = BreadcrumbLevel.Info);
}

/// <summary>
/// Result of exception handling with user and technical information
/// </summary>
public class ErrorResult
{
    public bool IsSuccess { get; init; }
    public string UserMessage { get; init; } = string.Empty;
    public string TechnicalDetails { get; init; } = string.Empty;
    public string ErrorCode { get; init; } = string.Empty;
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;

    public static ErrorResult Success() => new() { IsSuccess = true };

    public static ErrorResult Failure(string userMessage, string technicalDetails = "", string errorCode = "")
        => new()
        {
            IsSuccess = false,
            UserMessage = userMessage,
            TechnicalDetails = technicalDetails,
            ErrorCode = errorCode
        };
}

/// <summary>
/// Context information for exception handling
/// </summary>
public class ErrorContext
{
    public string Operation { get; init; } = string.Empty;
    public string FeatureArea { get; init; } = string.Empty;
    public string UserId { get; init; } = string.Empty;
    public Dictionary<string, object> Properties { get; init; } = new();
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Breadcrumb severity levels for debugging context
/// </summary>
public enum BreadcrumbLevel
{
    Debug,
    Info,
    Warning,
    Error,
    Fatal
}

/// <summary>
/// Exception types for categorization and handling strategy
/// </summary>
public enum ExceptionCategory
{
    Unknown,
    Network,
    Authentication,
    Authorization,
    Storage,
    Validation,
    Business,
    External,
    System
}