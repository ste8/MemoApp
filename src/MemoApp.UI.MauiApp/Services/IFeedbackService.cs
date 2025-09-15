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
    /// Allows user to pick images from their device gallery or camera
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of selected images with data and metadata, or empty list if no selection made</returns>
    Task<List<AttachedImage>> PickImagesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Collects current system information for debugging purposes
    /// </summary>
    /// <returns>Dictionary containing system information</returns>
    Dictionary<string, string> GetSystemInfo();
}