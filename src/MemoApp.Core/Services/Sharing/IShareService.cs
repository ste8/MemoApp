namespace MemoApp.Core.Services.Sharing;

/// <summary>
/// Service for sharing content across different platforms
/// </summary>
public interface IShareService
{
    /// <summary>
    /// Shares text content using the platform's native sharing mechanism
    /// </summary>
    /// <param name="title">The title for the share dialog</param>
    /// <param name="text">The text content to share</param>
    /// <returns>True if sharing was successful or initiated, false if cancelled or failed</returns>
    Task<bool> ShareTextAsync(string title, string text);

    /// <summary>
    /// Checks if sharing functionality is available on the current platform
    /// </summary>
    /// <returns>True if sharing is supported, false otherwise</returns>
    bool IsShareAvailable();

    /// <summary>
    /// Copies text to the system clipboard as a fallback
    /// </summary>
    /// <param name="text">The text to copy to clipboard</param>
    /// <returns>True if successful, false otherwise</returns>
    Task<bool> CopyToClipboardAsync(string text);
}