using MemoApp.Localization.Services;
using Microsoft.Extensions.Logging;

namespace MemoApp.UI.MauiApp.Services;

/// <summary>
/// Provides user notification capabilities using MAUI's built-in dialogs and platform-specific toasts
/// </summary>
public class UserNotificationService : IUserNotificationService
{
    private readonly ILocalizationService _localizationService;
    private readonly ILogger<UserNotificationService> _logger;

    public UserNotificationService(
        ILocalizationService localizationService,
        ILogger<UserNotificationService> logger)
    {
        _localizationService = localizationService;
        _logger = logger;
    }

    public async Task ShowToastAsync(string message, int duration = 3000)
    {
        try
        {
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                // Use platform-specific toast if available, otherwise fallback to DisplayAlert
                if (Application.Current?.MainPage != null)
                {
                    // For now, use a simple approach - show as a brief alert
                    // This can be enhanced with platform-specific toast implementations
                    var page = GetCurrentPage();
                    if (page != null)
                    {
                        var task = page.DisplayAlert("", message, _localizationService.GetString("Dialog_OK") ?? "OK");
                        // Don't await - let it show briefly
                        _ = Task.Run(async () =>
                        {
                            await Task.Delay(Math.Min(duration, 1500)); // Cap at 1.5s for non-blocking experience
                        });
                    }
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to show toast message: {Message}", message);
        }
    }

    public async Task ShowWarningToastAsync(string message, int duration = 4000)
    {
        await ShowToastAsync($"⚠️ {message}", duration);
    }

    public async Task ShowErrorToastAsync(string message, int duration = 5000)
    {
        await ShowToastAsync($"❌ {message}", duration);
    }

    public async Task ShowErrorDialogAsync(string title, string message, string? buttonText = null)
    {
        try
        {
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                var page = GetCurrentPage();
                if (page != null)
                {
                    var okText = buttonText ?? _localizationService.GetString("Dialog_OK") ?? "OK";
                    await page.DisplayAlert(title, message, okText);
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to show error dialog: {Title} - {Message}", title, message);
        }
    }

    public async Task<bool> ShowConfirmationDialogAsync(string title, string message, string? acceptText = null, string? cancelText = null)
    {
        try
        {
            return await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                var page = GetCurrentPage();
                if (page != null)
                {
                    var yesText = acceptText ?? _localizationService.GetString("Dialog_Yes") ?? "Yes";
                    var noText = cancelText ?? _localizationService.GetString("Dialog_No") ?? "No";
                    return await page.DisplayAlert(title, message, yesText, noText);
                }
                return false;
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to show confirmation dialog: {Title} - {Message}", title, message);
            return false;
        }
    }

    private Page? GetCurrentPage()
    {
        try
        {
            // Get the current page from the application
            if (Application.Current?.MainPage is Shell shell)
            {
                return shell.CurrentPage;
            }
            
            if (Application.Current?.MainPage is NavigationPage navPage)
            {
                return navPage.CurrentPage;
            }

            return Application.Current?.MainPage;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get current page for notification");
            return null;
        }
    }
}