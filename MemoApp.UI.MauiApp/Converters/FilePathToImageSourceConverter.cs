using System.Collections.Concurrent;
using System.Globalization;
using MemoApp.UI.MauiApp.Services;

namespace MemoApp.UI.MauiApp.Converters;

/// <summary>
/// High-performance image converter that uses caching and async loading to prevent UI thread blocking.
/// Uses service locator pattern to access ImageCacheService from static XAML context.
/// </summary>
public class FilePathToImageSourceConverter : IValueConverter
{
    private static IImageCacheService? _imageCacheService;
    private static readonly object _lockObject = new();

    // Fallback in-memory cache for when service is not available
    private static readonly ConcurrentDictionary<string, WeakReference<ImageSource>> _fallbackCache = new();
    
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not string filePath || string.IsNullOrEmpty(filePath))
        {
            return null;
        }

        try
        {
            // Get image cache service (lazy initialization)
            var cacheService = GetImageCacheService();
            
            if (cacheService != null)
            {
                // Use the high-performance cache service
                return cacheService.GetImageOrPlaceholder(filePath);
            }

            // Fallback to optimized local caching if service is not available
            return GetImageWithFallbackCache(filePath);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"FilePathToImageSourceConverter: Exception: {ex.Message}");
            return null;
        }
    }

    private static IImageCacheService? GetImageCacheService()
    {
        if (_imageCacheService != null)
            return _imageCacheService;

        lock (_lockObject)
        {
            if (_imageCacheService != null)
                return _imageCacheService;

            try
            {
                // Try to get the service from the current application's service provider
                if (Application.Current?.Handler?.MauiContext?.Services is IServiceProvider services)
                {
                    _imageCacheService = services.GetService<IImageCacheService>();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"FilePathToImageSourceConverter: Could not resolve ImageCacheService: {ex.Message}");
            }
        }

        return _imageCacheService;
    }

    private static ImageSource? GetImageWithFallbackCache(string filePath)
    {
        // Check fallback cache first
        if (_fallbackCache.TryGetValue(filePath, out var weakRef) && 
            weakRef.TryGetTarget(out var cachedImage))
        {
            return cachedImage;
        }

        // Load asynchronously but return null immediately to avoid blocking UI
        // In a real scenario, you might want to return a placeholder here
        _ = Task.Run(async () =>
        {
            try
            {
                if (File.Exists(filePath))
                {
                    // Load image on background thread
                    var imageSource = ImageSource.FromFile(filePath);
                    _fallbackCache[filePath] = new WeakReference<ImageSource>(imageSource);
                    
                    // Trigger UI refresh by setting the binding again (would require more complex implementation)
                    // This is why using the proper service is better
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"FilePathToImageSourceConverter fallback loading failed: {ex.Message}");
            }
        });

        return null; // Return null to avoid blocking UI thread
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}