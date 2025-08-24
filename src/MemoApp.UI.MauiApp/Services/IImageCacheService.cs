namespace MemoApp.UI.MauiApp.Services;

/// <summary>
/// Service for caching and loading images asynchronously to improve UI performance
/// </summary>
public interface IImageCacheService
{
    /// <summary>
    /// Gets an image asynchronously with caching support
    /// </summary>
    /// <param name="path">The file path to the image</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The loaded image source or null if not found</returns>
    Task<ImageSource?> GetImageAsync(string path, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets an image synchronously from cache or returns placeholder
    /// </summary>
    /// <param name="path">The file path to the image</param>
    /// <returns>Cached image source, placeholder, or null</returns>
    ImageSource? GetImageOrPlaceholder(string path);
    
    /// <summary>
    /// Pre-loads images into cache to improve perceived performance
    /// </summary>
    /// <param name="paths">The file paths to preload</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task PreloadImagesAsync(IEnumerable<string> paths, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Clears all cached images to free memory
    /// </summary>
    void ClearCache();
    
    /// <summary>
    /// Gets cache statistics for monitoring
    /// </summary>
    (int CachedCount, int TotalRequests, double HitRate) GetCacheStats();
}