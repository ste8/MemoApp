namespace MemoApp.UI.MauiApp.Services;

/// <summary>
/// Service for resizing and optimizing images to meet size constraints
/// </summary>
public interface IImageResizingService
{
    /// <summary>
    /// Processes an image to ensure it fits within the specified size constraints.
    /// If the image is already within limits, returns it unchanged.
    /// Otherwise, resizes and/or compresses the image to meet the requirements.
    /// </summary>
    /// <param name="imageData">The original image data as byte array</param>
    /// <param name="maxSizeInBytes">Maximum allowed size in bytes</param>
    /// <param name="cancellationToken">Cancellation token for async operations</param>
    /// <returns>Processed image data, or null if processing fails</returns>
    Task<byte[]?> OptimizeImageForSizeAsync(byte[] imageData, int maxSizeInBytes, CancellationToken cancellationToken = default);
}