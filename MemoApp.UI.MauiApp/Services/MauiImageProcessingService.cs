using MemoApp.Core.MajorSystem.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Graphics.Platform;

namespace MemoApp.UI.MauiApp.Services;

/// <summary>
/// MAUI implementation of image processing service using Microsoft.Maui.Graphics
/// </summary>
public class MauiImageProcessingService : IImageProcessingService
{
    private readonly ILogger<MauiImageProcessingService> _logger;

    public MauiImageProcessingService(ILogger<MauiImageProcessingService> logger)
    {
        _logger = logger;
    }

    public async Task<bool> GenerateThumbnailAsync(string sourceImagePath, string thumbnailPath, int maxWidth = 200, int maxHeight = 200)
    {
        try
        {
            _logger.LogInformation("Generating thumbnail from {Source} to {Target} (max: {Width}x{Height})", 
                sourceImagePath, thumbnailPath, maxWidth, maxHeight);

            if (!File.Exists(sourceImagePath))
            {
                _logger.LogWarning("Source image file does not exist: {Path}", sourceImagePath);
                return false;
            }

            // Ensure the output directory exists
            var outputDir = Path.GetDirectoryName(thumbnailPath);
            if (!string.IsNullOrEmpty(outputDir) && !Directory.Exists(outputDir))
            {
                Directory.CreateDirectory(outputDir);
            }

            // Use Microsoft.Maui.Graphics for proper image processing with WebP support
            await Task.Run(() =>
            {
                using var sourceStream = File.OpenRead(sourceImagePath);
                var sourceImage = PlatformImage.FromStream(sourceStream);
                
                if (sourceImage == null)
                {
                    throw new InvalidOperationException($"Failed to load image from {sourceImagePath}");
                }

                // Calculate thumbnail dimensions while maintaining aspect ratio
                var aspectRatio = (float)sourceImage.Width / sourceImage.Height;
                int thumbWidth, thumbHeight;
                
                if (aspectRatio > 1) // Wider than tall
                {
                    thumbWidth = maxWidth;
                    thumbHeight = (int)(maxWidth / aspectRatio);
                }
                else // Taller than wide or square
                {
                    thumbHeight = maxHeight;
                    thumbWidth = (int)(maxHeight * aspectRatio);
                }

                // Generate thumbnail
                var thumbnail = sourceImage.Resize(thumbWidth, thumbHeight, ResizeMode.Stretch, true);
                
                if (thumbnail == null)
                {
                    sourceImage.Dispose();
                    throw new InvalidOperationException("Failed to resize image");
                }

                // Save thumbnail in preferred format
                using var destStream = File.Create(thumbnailPath);
                var outputFormat = GetImageFormatFromPath(thumbnailPath);
                thumbnail.Save(destStream, outputFormat, 0.85f); // 85% quality
                
                thumbnail.Dispose();
                sourceImage.Dispose();
            });

            _logger.LogInformation("Successfully generated thumbnail: {Path}", thumbnailPath);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating thumbnail from {Source} to {Target}", sourceImagePath, thumbnailPath);
            return false;
        }
    }

    public async Task<bool> ResizeImageAsync(string sourceImagePath, string outputPath, int maxWidth = 1024, int maxHeight = 1024)
    {
        try
        {
            _logger.LogInformation("Resizing image from {Source} to {Target} (max: {Width}x{Height})", 
                sourceImagePath, outputPath, maxWidth, maxHeight);

            if (!File.Exists(sourceImagePath))
            {
                _logger.LogWarning("Source image file does not exist: {Path}", sourceImagePath);
                return false;
            }

            // Ensure the output directory exists
            var outputDir = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(outputDir) && !Directory.Exists(outputDir))
            {
                Directory.CreateDirectory(outputDir);
            }

            // Use Microsoft.Maui.Graphics for proper image resizing with WebP support
            await Task.Run(() =>
            {
                using var sourceStream = File.OpenRead(sourceImagePath);
                var sourceImage = PlatformImage.FromStream(sourceStream);
                
                if (sourceImage == null)
                {
                    throw new InvalidOperationException($"Failed to load image from {sourceImagePath}");
                }

                var needsResize = sourceImage.Width > maxWidth || sourceImage.Height > maxHeight;
                Microsoft.Maui.Graphics.IImage outputImage;
                
                if (needsResize)
                {
                    // Calculate new dimensions while maintaining aspect ratio
                    var aspectRatio = (float)sourceImage.Width / sourceImage.Height;
                    int newWidth, newHeight;
                    
                    if (aspectRatio > 1) // Wider than tall
                    {
                        newWidth = Math.Min(maxWidth, (int)sourceImage.Width);
                        newHeight = (int)Math.Round(newWidth / aspectRatio);
                    }
                    else // Taller than wide or square
                    {
                        newHeight = Math.Min(maxHeight, (int)sourceImage.Height);
                        newWidth = (int)Math.Round(newHeight * aspectRatio);
                    }

                    outputImage = sourceImage.Resize(newWidth, newHeight, ResizeMode.Stretch, true);
                    if (outputImage == null)
                    {
                        sourceImage.Dispose();
                        throw new InvalidOperationException("Failed to resize image");
                    }
                }
                else
                {
                    // No resize needed, but we might need to convert format
                    outputImage = sourceImage;
                }

                // Save in preferred format
                using var destStream = File.Create(outputPath);
                var outputFormat = GetImageFormatFromPath(outputPath);
                outputImage.Save(destStream, outputFormat, 0.95f); // High quality for main images
                
                if (outputImage != sourceImage)
                {
                    outputImage.Dispose();
                }
                sourceImage.Dispose();
            });

            _logger.LogInformation("Successfully processed image: {Path}", outputPath);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resizing image from {Source} to {Target}", sourceImagePath, outputPath);
            return false;
        }
    }

    public async Task<(int width, int height)> GetImageDimensionsAsync(string imagePath)
    {
        try
        {
            if (!File.Exists(imagePath))
            {
                return (0, 0);
            }

            // TODO: Implement actual dimension reading using Microsoft.Maui.Graphics or SkiaSharp
            // For now, return default dimensions
            await Task.Delay(10); // Simulate async operation

            // Default placeholder dimensions
            return (800, 600);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting image dimensions for {Path}", imagePath);
            return (0, 0);
        }
    }

    public async Task<bool> IsValidImageAsync(string imagePath)
    {
        try
        {
            if (!File.Exists(imagePath))
            {
                return false;
            }

            // Check file extension
            var extension = Path.GetExtension(imagePath).ToLowerInvariant();
            if (!new[] { ".png", ".jpg", ".jpeg", ".gif", ".bmp", ".webp" }.Contains(extension))
            {
                return false;
            }

            // TODO: Implement actual image validation by trying to load the image
            // For now, just check if file exists and has correct extension
            await Task.Delay(10); // Simulate async operation

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating image at {Path}", imagePath);
            return false;
        }
    }

    public long GetImageFileSize(string imagePath)
    {
        try
        {
            if (!File.Exists(imagePath))
            {
                return 0;
            }

            var fileInfo = new FileInfo(imagePath);
            return fileInfo.Length;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting file size for {Path}", imagePath);
            return 0;
        }
    }

    private ImageFormat GetImageFormatFromPath(string filePath)
    {
        var extension = Path.GetExtension(filePath).ToLowerInvariant();
        return extension switch
        {
            ".webp" => ImageFormat.Png, // Use PNG as fallback for WebP until native support
            ".png" => ImageFormat.Png,
            ".jpg" or ".jpeg" => ImageFormat.Jpeg,
            ".bmp" => ImageFormat.Bmp,
            _ => ImageFormat.Png // Default to PNG for compatibility
        };
    }
}