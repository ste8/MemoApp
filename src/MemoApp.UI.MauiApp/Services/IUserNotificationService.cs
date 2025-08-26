namespace MemoApp.UI.MauiApp.Services;

/// <summary>
/// Provides consistent user notification capabilities across the application
/// </summary>
public interface IUserNotificationService
{
    /// <summary>
    /// Shows a brief toast notification to the user
    /// </summary>
    /// <param name="message">The message to display</param>
    /// <param name="duration">Duration in milliseconds (default: 3000ms)</param>
    Task ShowToastAsync(string message, int duration = 3000);

    /// <summary>
    /// Shows a warning toast notification to the user
    /// </summary>
    /// <param name="message">The warning message to display</param>
    /// <param name="duration">Duration in milliseconds (default: 4000ms)</param>
    Task ShowWarningToastAsync(string message, int duration = 4000);

    /// <summary>
    /// Shows an error toast notification to the user
    /// </summary>
    /// <param name="message">The error message to display</param>
    /// <param name="duration">Duration in milliseconds (default: 5000ms)</param>
    Task ShowErrorToastAsync(string message, int duration = 5000);

    /// <summary>
    /// Shows a blocking error dialog to the user
    /// </summary>
    /// <param name="title">Dialog title</param>
    /// <param name="message">Error message</param>
    /// <param name="buttonText">Button text (default: "OK")</param>
    Task ShowErrorDialogAsync(string title, string message, string? buttonText = null);

    /// <summary>
    /// Shows a confirmation dialog with Yes/No options
    /// </summary>
    /// <param name="title">Dialog title</param>
    /// <param name="message">Dialog message</param>
    /// <param name="acceptText">Accept button text (default: "Yes")</param>
    /// <param name="cancelText">Cancel button text (default: "No")</param>
    /// <returns>True if user confirmed, false if cancelled</returns>
    Task<bool> ShowConfirmationDialogAsync(string title, string message, string? acceptText = null, string? cancelText = null);
}