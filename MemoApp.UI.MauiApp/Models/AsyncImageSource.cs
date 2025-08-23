using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using MemoApp.UI.MauiApp.Services;

namespace MemoApp.UI.MauiApp.Models;

/// <summary>
/// Bindable async image source that loads images asynchronously and notifies when complete
/// </summary>
public partial class AsyncImageSource : ObservableObject
{
    private readonly IImageCacheService _imageCacheService;
    private readonly string _imagePath;
    
    [ObservableProperty]
    private ImageSource? _imageSource;
    
    [ObservableProperty]
    private bool _isLoading = true;

    public AsyncImageSource(string imagePath, IImageCacheService imageCacheService)
    {
        _imagePath = imagePath;
        _imageCacheService = imageCacheService;
        
        // Start loading immediately
        _ = LoadImageAsync();
    }

    private async Task LoadImageAsync()
    {
        try
        {
            if (string.IsNullOrEmpty(_imagePath))
            {
                IsLoading = false;
                return;
            }

            // First try to get from cache or placeholder
            ImageSource = _imageCacheService.GetImageOrPlaceholder(_imagePath);
            
            // Then load the actual image asynchronously
            var loadedImage = await _imageCacheService.GetImageAsync(_imagePath);
            
            if (loadedImage != null)
            {
                ImageSource = loadedImage;
            }
        }
        catch (Exception)
        {
            // Silently fail - image will remain as placeholder or null
        }
        finally
        {
            IsLoading = false;
        }
    }
}