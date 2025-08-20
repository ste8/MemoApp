using Microsoft.Extensions.Logging;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Graphics.Platform;

namespace MemoApp.UI.MauiApp.Services;

public class ThumbnailService : IThumbnailService
{
    private readonly ILogger<ThumbnailService> _logger;
    private readonly string[] _supportedExtensions = { ".png", ".jpg", ".jpeg", ".bmp" };

    public ThumbnailService(ILogger<ThumbnailService> logger)
    {
        _logger = logger;
    }

    public async Task<bool> GenerateThumbnailAsync(
        string sourcePath,
        string destinationPath,
        int width,
        int height,
        bool maintainAspectRatio = true,
        int quality = 85)
    {
        try
        {
            if (!File.Exists(sourcePath))
            {
                _logger.LogWarning("Source image not found: {Path}", sourcePath);
                return false;
            }

            // Ensure destination directory exists
            var destDir = Path.GetDirectoryName(destinationPath);
            if (!string.IsNullOrEmpty(destDir))
            {
                Directory.CreateDirectory(destDir);
            }

            await Task.Run(() =>
            {
                using var sourceStream = File.OpenRead(sourcePath);
                var image = PlatformImage.FromStream(sourceStream);
                
                if (image == null)
                {
                    throw new InvalidOperationException($"Failed to load image from {sourcePath}");
                }

                // Calculate thumbnail dimensions
                var (targetWidth, targetHeight) = CalculateThumbnailDimensions(
                    image.Width, image.Height, width, height, maintainAspectRatio);

                // Create thumbnail
                var resized = image.Resize(targetWidth, targetHeight, ResizeMode.Stretch, true);
                
                if (resized == null)
                {
                    throw new InvalidOperationException("Failed to resize image");
                }

                // Save thumbnail
                using var destStream = File.Create(destinationPath);
                resized.Save(destStream, ImageFormat.Png, quality / 100f);
                
                resized.Dispose();
                image.Dispose();
            });

            _logger.LogDebug("Generated thumbnail for {Source} -> {Dest}", sourcePath, destinationPath);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate thumbnail for {Source}", sourcePath);
            return false;
        }
    }

    public async Task<int> GenerateThumbnailsForDirectoryAsync(
        string sourceDirectory,
        string thumbnailDirectory,
        int width,
        int height,
        bool maintainAspectRatio = true,
        int quality = 85,
        IProgress<(int processed, int total, string currentFile)>? progress = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (!Directory.Exists(sourceDirectory))
            {
                _logger.LogWarning("Source directory not found: {Path}", sourceDirectory);
                return 0;
            }

            // Get all image files recursively
            var imageFiles = Directory.GetFiles(sourceDirectory, "*.*", SearchOption.AllDirectories)
                .Where(f => _supportedExtensions.Contains(Path.GetExtension(f).ToLowerInvariant()))
                .ToList();

            if (imageFiles.Count == 0)
            {
                _logger.LogInformation("No images found in {Path}", sourceDirectory);
                return 0;
            }

            _logger.LogInformation("Found {Count} images to process", imageFiles.Count);

            int processedCount = 0;
            int successCount = 0;

            foreach (var sourceFile in imageFiles)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    _logger.LogInformation("Thumbnail generation cancelled");
                    break;
                }

                // Calculate relative path and destination
                var relativePath = Path.GetRelativePath(sourceDirectory, sourceFile);
                var destPath = Path.Combine(thumbnailDirectory, relativePath);

                progress?.Report((processedCount, imageFiles.Count, Path.GetFileName(sourceFile)));

                // Skip if thumbnail already exists and is newer than source
                if (await IsThumbnailGeneratedAsync(sourceFile, destPath))
                {
                    _logger.LogDebug("Thumbnail already exists for {File}", relativePath);
                    processedCount++;
                    successCount++;
                    continue;
                }

                var success = await GenerateThumbnailAsync(
                    sourceFile, destPath, width, height, maintainAspectRatio, quality);

                if (success)
                {
                    successCount++;
                }

                processedCount++;
                progress?.Report((processedCount, imageFiles.Count, Path.GetFileName(sourceFile)));
            }

            _logger.LogInformation("Generated {Success}/{Total} thumbnails successfully", 
                successCount, processedCount);

            return successCount;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate thumbnails for directory {Path}", sourceDirectory);
            return 0;
        }
    }

    public (int width, int height) GetDeviceOptimalThumbnailSize()
    {
        try
        {
            var displayInfo = DeviceDisplay.Current.MainDisplayInfo;
            var density = displayInfo.Density;

            // Base size in device-independent pixels
            const int baseSize = 150;

            // Adjust for screen density
            var optimalSize = (int)(baseSize * density);

            // Cap at reasonable maximum
            const int maxSize = 300;
            optimalSize = Math.Min(optimalSize, maxSize);

            _logger.LogDebug("Optimal thumbnail size for device: {Size}x{Size} (density: {Density})",
                optimalSize, optimalSize, density);

            return (optimalSize, optimalSize);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get device display info, using default thumbnail size");
            return (150, 150); // Default fallback
        }
    }

    public async Task<bool> IsThumbnailGeneratedAsync(string originalPath, string thumbnailPath)
    {
        try
        {
            if (!File.Exists(thumbnailPath))
                return false;

            var sourceInfo = new FileInfo(originalPath);
            var thumbInfo = new FileInfo(thumbnailPath);

            // Check if thumbnail is newer than source
            return await Task.FromResult(thumbInfo.LastWriteTimeUtc >= sourceInfo.LastWriteTimeUtc);
        }
        catch
        {
            return false;
        }
    }

    private (int width, int height) CalculateThumbnailDimensions(
        float sourceWidth, float sourceHeight,
        int targetWidth, int targetHeight,
        bool maintainAspectRatio)
    {
        if (!maintainAspectRatio)
        {
            return (targetWidth, targetHeight);
        }

        var sourceAspect = sourceWidth / sourceHeight;
        var targetAspect = (float)targetWidth / targetHeight;

        int finalWidth, finalHeight;

        if (sourceAspect > targetAspect)
        {
            // Source is wider - fit to width
            finalWidth = targetWidth;
            finalHeight = (int)(targetWidth / sourceAspect);
        }
        else
        {
            // Source is taller - fit to height
            finalHeight = targetHeight;
            finalWidth = (int)(targetHeight * sourceAspect);
        }

        return (finalWidth, finalHeight);
    }
}