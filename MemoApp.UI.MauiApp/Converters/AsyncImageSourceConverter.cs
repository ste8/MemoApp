using System.Globalization;
using MemoApp.UI.MauiApp.Services;

namespace MemoApp.UI.MauiApp.Converters;

/// <summary>
/// High-performance async image converter that uses caching and placeholder loading
/// to prevent UI thread blocking during image loading
/// </summary>
public class AsyncImageSourceConverter : IValueConverter
{
    private readonly IImageCacheService _imageCacheService;

    public AsyncImageSourceConverter(IImageCacheService imageCacheService)
    {
        _imageCacheService = imageCacheService;
    }

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not string filePath || string.IsNullOrEmpty(filePath))
        {
            return null;
        }

        try
        {
            // Get cached image or placeholder immediately - never block UI thread
            return _imageCacheService.GetImageOrPlaceholder(filePath);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"AsyncImageSourceConverter: Exception: {ex.Message}");
            return null;
        }
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}