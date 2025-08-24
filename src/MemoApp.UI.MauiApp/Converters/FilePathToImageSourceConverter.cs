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
                var result = cacheService.GetImageOrPlaceholder(filePath);
                System.Diagnostics.Debug.WriteLine($"FilePathToImageSourceConverter: Using cache service for {filePath}, result: {result != null}");
                return result;
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
                    System.Diagnostics.Debug.WriteLine($"FilePathToImageSourceConverter: Service resolution result: {_imageCacheService != null}");
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

        // The service is not available, use simple synchronous loading as fallback
        // This ensures images display in TokenDetailPage
        try
        {
            if (File.Exists(filePath))
            {
                System.Diagnostics.Debug.WriteLine($"FilePathToImageSourceConverter: Service not available, loading {filePath} directly");
                var imageSource = ImageSource.FromFile(filePath);
                _fallbackCache[filePath] = new WeakReference<ImageSource>(imageSource);
                return imageSource;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"FilePathToImageSourceConverter: Direct loading failed: {ex.Message}");
        }

        // Don't do async loading in fallback - just return null
        return null;
    }


    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}