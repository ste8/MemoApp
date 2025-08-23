using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace MemoApp.UI.MauiApp.Services;

/// <summary>
/// High-performance image caching service using WeakReference for automatic memory management
/// </summary>
public class ImageCacheService : IImageCacheService, IDisposable
{
    private readonly ConcurrentDictionary<string, WeakReference<ImageSource>> _imageCache = new();
    private readonly ConcurrentDictionary<string, bool> _fileExistsCache = new();
    private readonly SemaphoreSlim _loadingSemaphore = new(Environment.ProcessorCount * 2);
    private readonly ILogger<ImageCacheService> _logger;
    private readonly Timer _cleanupTimer;
    
    // Statistics
    private int _totalRequests;
    private int _cacheHits;
    private int _cacheCleanups;

    // Placeholder image source for immediate display
    private ImageSource? _placeholderImage;
    
    public ImageCacheService(ILogger<ImageCacheService> logger)
    {
        _logger = logger;
        
        // Initialize placeholder
        try
        {
            _placeholderImage = ImageSource.FromResource("placeholder.png");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not load placeholder image, using transparent placeholder");
            _placeholderImage = null;
        }
        
        // Setup automatic cache cleanup every 5 minutes
        _cleanupTimer = new Timer(CleanupExpiredImages, null, 
            TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5));
            
        _logger.LogInformation("ImageCacheService initialized with placeholder: {HasPlaceholder}", _placeholderImage != null);
    }

    public ImageSource? GetImageOrPlaceholder(string path)
    {
        if (string.IsNullOrEmpty(path))
            return null;

        Interlocked.Increment(ref _totalRequests);

        // Check cache first
        if (_imageCache.TryGetValue(path, out var weakRef) && 
            weakRef.TryGetTarget(out var cachedImage))
        {
            Interlocked.Increment(ref _cacheHits);
            return cachedImage;
        }

        // Check if we know the file doesn't exist
        if (_fileExistsCache.TryGetValue(path, out var exists) && !exists)
        {
            return null;
        }

        // For immediate display, try synchronous loading first
        // This ensures images show on first load
        try
        {
            if (File.Exists(path))
            {
                _fileExistsCache[path] = true;
                var imageSource = ImageSource.FromFile(path);
                
                // Cache for future use
                _imageCache[path] = new WeakReference<ImageSource>(imageSource);
                _logger.LogDebug("Loaded and cached image synchronously: {Path}", path);
                
                return imageSource;
            }
            else
            {
                _fileExistsCache[path] = false;
                _logger.LogDebug("File does not exist: {Path}", path);
                return null;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load image synchronously from {Path}, trying async", path);
            
            // If sync fails, start async loading
            _ = Task.Run(async () =>
            {
                try
                {
                    await GetImageAsync(path);
                }
                catch (Exception asyncEx)
                {
                    _logger.LogDebug(asyncEx, "Background image loading also failed for {Path}", path);
                }
            });

            // Return placeholder if we have one
            return _placeholderImage;
        }
    }

    public async Task<ImageSource?> GetImageAsync(string path, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(path))
            return null;

        Interlocked.Increment(ref _totalRequests);

        // Check cache first
        if (_imageCache.TryGetValue(path, out var weakRef) && 
            weakRef.TryGetTarget(out var cachedImage))
        {
            Interlocked.Increment(ref _cacheHits);
            return cachedImage;
        }

        await _loadingSemaphore.WaitAsync(cancellationToken);
        try
        {
            // Double-check cache after acquiring semaphore
            if (_imageCache.TryGetValue(path, out weakRef) && 
                weakRef.TryGetTarget(out cachedImage))
            {
                Interlocked.Increment(ref _cacheHits);
                return cachedImage;
            }

            // Check if we already know the file doesn't exist
            if (_fileExistsCache.TryGetValue(path, out var exists) && !exists)
            {
                return null;
            }

            // Load image on background thread
            var imageSource = await Task.Run(() =>
            {
                try
                {
                    if (!File.Exists(path))
                    {
                        _fileExistsCache[path] = false;
                        return null;
                    }

                    _fileExistsCache[path] = true;
                    return ImageSource.FromFile(path);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to load image from {Path}", path);
                    _fileExistsCache[path] = false;
                    return null;
                }
            }, cancellationToken);

            if (imageSource != null)
            {
                // Store in cache using WeakReference for automatic memory management
                _imageCache[path] = new WeakReference<ImageSource>(imageSource);
                _logger.LogDebug("Cached image: {Path}", path);
                return imageSource;
            }

            return null;
        }
        finally
        {
            _loadingSemaphore.Release();
        }
    }

    public async Task PreloadImagesAsync(IEnumerable<string> paths, CancellationToken cancellationToken = default)
    {
        var pathsList = paths.ToList();
        _logger.LogInformation("Preloading {Count} images", pathsList.Count);

        // Process images in batches to avoid overwhelming the system
        const int batchSize = 10;
        var batches = pathsList.Chunk(batchSize);

        foreach (var batch in batches)
        {
            if (cancellationToken.IsCancellationRequested)
                break;

            var loadTasks = batch.Select(path => GetImageAsync(path, cancellationToken));
            await Task.WhenAll(loadTasks);

            // Small delay between batches to not overwhelm UI
            await Task.Delay(50, cancellationToken);
        }

        _logger.LogInformation("Completed preloading {Count} images", pathsList.Count);
    }

    public void ClearCache()
    {
        var cacheCount = _imageCache.Count;
        var fileExistsCount = _fileExistsCache.Count;
        
        _imageCache.Clear();
        _fileExistsCache.Clear();
        
        // Reset statistics
        _totalRequests = 0;
        _cacheHits = 0;
        
        _logger.LogInformation("Cleared image cache: {ImageCacheCount} images, {FileExistsCacheCount} file existence entries", 
            cacheCount, fileExistsCount);
    }

    public (int CachedCount, int TotalRequests, double HitRate) GetCacheStats()
    {
        var totalRequests = _totalRequests;
        var cacheHits = _cacheHits;
        var hitRate = totalRequests > 0 ? (double)cacheHits / totalRequests * 100 : 0;
        
        return (_imageCache.Count, totalRequests, hitRate);
    }

    private void CleanupExpiredImages(object? state)
    {
        try
        {
            var expiredImageKeys = _imageCache
                .Where(kvp => !kvp.Value.TryGetTarget(out _))
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var key in expiredImageKeys)
            {
                _imageCache.TryRemove(key, out _);
            }

            if (expiredImageKeys.Count > 0)
            {
                Interlocked.Add(ref _cacheCleanups, expiredImageKeys.Count);
                _logger.LogDebug("Cleaned up {Count} expired image cache entries", expiredImageKeys.Count);
            }

            // Also cleanup old file existence cache entries (keep for 30 minutes)
            // This is a simple cleanup - in production you might want timestamp-based cleanup
            if (_fileExistsCache.Count > 10000)
            {
                _logger.LogInformation("File exists cache is large ({Count} entries), clearing to prevent memory issues", _fileExistsCache.Count);
                _fileExistsCache.Clear();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during image cache cleanup");
        }
    }

    public void Dispose()
    {
        _cleanupTimer?.Dispose();
        _loadingSemaphore?.Dispose();
        ClearCache();
        _placeholderImage = null;
    }
}