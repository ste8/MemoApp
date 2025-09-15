using MemoApp.Core.Models;

namespace MemoApp.UI.MauiApp.Services;

/// <summary>
/// Service for submitting user feedback to the backend system
/// </summary>
public interface IFeedbackService
{
    /// <summary>
    /// Submits feedback to the backend system
    /// </summary>
    /// <param name="feedback">The feedback model to submit</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if submission was successful, false otherwise</returns>
    Task<bool> SubmitFeedbackAsync(FeedbackModel feedback, CancellationToken cancellationToken = default);

    /// <summary>
    /// Captures a screenshot of the current screen and compresses it
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Compressed screenshot data and filename, or null if capture failed</returns>
    Task<(byte[] Data, string FileName)?> CaptureScreenshotAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Collects current system information for debugging purposes
    /// </summary>
    /// <returns>Dictionary containing system information</returns>
    Dictionary<string, string> GetSystemInfo();
}