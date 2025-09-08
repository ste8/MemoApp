using MemoApp.Core.Services.Sharing;
using Microsoft.Extensions.Logging;

namespace MemoApp.UI.MauiApp.Services.Sharing;

/// <summary>
/// MAUI implementation of share service using Microsoft.Maui.Essentials APIs
/// </summary>
public class MauiShareService : IShareService
{
    private readonly ILogger<MauiShareService> _logger;

    public MauiShareService(ILogger<MauiShareService> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<bool> ShareTextAsync(string title, string text)
    {
        try
        {
            _logger.LogDebug("Attempting to share text: {Title}", title);

            var shareRequest = new ShareTextRequest
            {
                Title = title,
                Text = text
            };

            await Share.Default.RequestAsync(shareRequest);
            
            _logger.LogDebug("Share request completed successfully");
            return true;
        }
        catch (FeatureNotSupportedException ex)
        {
            _logger.LogWarning("Share feature not supported on this platform: {Error}", ex.Message);
            
            // Fallback to clipboard
            return await CopyToClipboardAsync(text);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to share text: {Title}", title);
            
            // Fallback to clipboard
            return await CopyToClipboardAsync(text);
        }
    }

    /// <inheritdoc/>
    public bool IsShareAvailable()
    {
        try
        {
            // Check if share is supported on current platform
            return Share.Default != null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to check if share is available");
            return false;
        }
    }

    /// <inheritdoc/>
    public async Task<bool> CopyToClipboardAsync(string text)
    {
        try
        {
            _logger.LogDebug("Copying text to clipboard as fallback");
            
            await Clipboard.Default.SetTextAsync(text);
            
            _logger.LogDebug("Text copied to clipboard successfully");
            return true;
        }
        catch (FeatureNotSupportedException ex)
        {
            _logger.LogWarning("Clipboard feature not supported: {Error}", ex.Message);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to copy text to clipboard");
            return false;
        }
    }
}