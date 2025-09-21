using Microsoft.Extensions.Logging;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Graphics.Platform;

namespace MemoApp.UI.MauiApp.Services;

/// <summary>
/// Service for resizing and optimizing images to meet size constraints
/// </summary>
public class ImageResizingService : IImageResizingService
{
    private readonly ILogger<ImageResizingService> _logger;

    // Configuration constants
    private const int DefaultJpegQuality = 80;
    private const int MinimumJpegQuality = 30;
    private const int QualityReductionStep = 15;
    private const int MinimumImageDimension = 100;
    private const double DimensionReductionFactor = 0.8;
    private const double InitialSafetyMargin = 0.9;
    private const int MaxOptimizationAttempts = 5;

    public ImageResizingService(ILogger<ImageResizingService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<byte[]?> OptimizeImageForSizeAsync(byte[] imageData, int maxSizeInBytes, CancellationToken cancellationToken = default)
    {
        return await Task.Run(() => OptimizeImageCore(imageData, maxSizeInBytes), cancellationToken);
    }

    private byte[]? OptimizeImageCore(byte[] originalData, int maxSizeInBytes)
    {
        try
        {
            // Return original if already within limits
            if (originalData.Length <= maxSizeInBytes)
            {
                _logger.LogInformation("Image size acceptable: {Size}KB", originalData.Length / 1024);
                return originalData;
            }

            _logger.LogInformation("Optimizing image from {Size}KB to fit within {MaxSize}KB",
                originalData.Length / 1024, maxSizeInBytes / 1024);

            // Load the image
            var image = LoadImage(originalData);
            if (image == null)
            {
                return null;
            }

            // Calculate initial parameters
            var strategy = new OptimizationStrategy(originalData.Length, maxSizeInBytes, image.Width, image.Height);

            // Attempt optimization with progressive strategies
            return OptimizeWithProgressiveStrategies(image, strategy, maxSizeInBytes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error optimizing image");
            return null;
        }
    }

    private Microsoft.Maui.Graphics.IImage? LoadImage(byte[] imageData)
    {
        try
        {
            using var stream = new MemoryStream(imageData);
            var image = PlatformImage.FromStream(stream);

            if (image == null)
            {
                _logger.LogError("Failed to load image from data");
            }

            return image;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading image");
            return null;
        }
    }

    private byte[]? OptimizeWithProgressiveStrategies(Microsoft.Maui.Graphics.IImage image, OptimizationStrategy strategy, int maxSizeInBytes)
    {
        byte[]? optimizedData = null;

        for (int attempt = 0; attempt < MaxOptimizationAttempts; attempt++)
        {
            var parameters = strategy.GetParametersForAttempt(attempt);

            _logger.LogDebug("Optimization attempt {Attempt}: {Width}x{Height} @ {Quality}% quality",
                attempt + 1, parameters.Width, parameters.Height, parameters.Quality);

            optimizedData = ResizeImage(image, parameters);

            if (optimizedData == null)
            {
                _logger.LogError("Failed to resize image on attempt {Attempt}", attempt + 1);
                return null;
            }

            // Check if target size achieved
            if (optimizedData.Length <= maxSizeInBytes)
            {
                _logger.LogInformation("Successfully optimized to {Size}KB ({Reduction}% reduction)",
                    optimizedData.Length / 1024,
                    strategy.OriginalSize > 0
                        ? (int)((1 - (double)optimizedData.Length / strategy.OriginalSize) * 100)
                        : 0);
                break;
            }

            _logger.LogDebug("Image still {Size}KB, continuing optimization",
                optimizedData.Length / 1024);
        }

        if (optimizedData != null && optimizedData.Length > maxSizeInBytes)
        {
            _logger.LogWarning("Could not optimize to target size. Final: {Size}KB (target: {Target}KB)",
                optimizedData.Length / 1024, maxSizeInBytes / 1024);
        }

        return optimizedData;
    }

    private byte[]? ResizeImage(Microsoft.Maui.Graphics.IImage originalImage, OptimizationParameters parameters)
    {
        try
        {
            using var resizedImage = originalImage.Downsize(parameters.Width, parameters.Height, true);

            if (resizedImage == null)
            {
                _logger.LogError("Failed to downsize image");
                return null;
            }

            using var outputStream = new MemoryStream();
            resizedImage.Save(outputStream, ImageFormat.Jpeg, quality: parameters.Quality / 100f);

            return outputStream.ToArray();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resizing image");
            return null;
        }
    }

    /// <summary>
    /// Encapsulates the optimization strategy and parameters calculation
    /// </summary>
    private sealed class OptimizationStrategy
    {
        private readonly float _originalWidth;
        private readonly float _originalHeight;
        private double _currentScaleFactor;

        public int OriginalSize { get; }

        public OptimizationStrategy(int originalSize, int targetSize, float width, float height)
        {
            OriginalSize = originalSize;
            _originalWidth = width;
            _originalHeight = height;
            _currentScaleFactor = CalculateInitialScale(originalSize, targetSize);
        }

        public OptimizationParameters GetParametersForAttempt(int attemptNumber)
        {
            // Strategy: First reduce quality, then dimensions
            var quality = attemptNumber switch
            {
                0 => DefaultJpegQuality,
                1 => DefaultJpegQuality - QualityReductionStep,
                2 => DefaultJpegQuality - (2 * QualityReductionStep),
                _ => MinimumJpegQuality
            };

            // After quality reduction attempts, start reducing dimensions
            if (attemptNumber >= 3)
            {
                _currentScaleFactor *= DimensionReductionFactor;
            }

            var (width, height) = CalculateDimensions(_currentScaleFactor);

            return new OptimizationParameters
            {
                Width = width,
                Height = height,
                Quality = quality
            };
        }

        private double CalculateInitialScale(int originalSize, int targetSize)
        {
            // Estimate required scaling based on file size to area ratio
            var sizeRatio = (double)targetSize / originalSize;
            var scaleFactor = Math.Sqrt(sizeRatio) * InitialSafetyMargin;
            return Math.Min(1.0, scaleFactor);
        }

        private (int width, int height) CalculateDimensions(double scaleFactor)
        {
            var width = (int)(_originalWidth * scaleFactor);
            var height = (int)(_originalHeight * scaleFactor);

            // Enforce minimum dimensions
            if (width < MinimumImageDimension || height < MinimumImageDimension)
            {
                var minScale = Math.Max(
                    (double)MinimumImageDimension / _originalWidth,
                    (double)MinimumImageDimension / _originalHeight);

                width = (int)(_originalWidth * minScale);
                height = (int)(_originalHeight * minScale);
            }

            return (width, height);
        }
    }

    /// <summary>
    /// Parameters for a single optimization attempt
    /// </summary>
    private sealed class OptimizationParameters
    {
        public int Width { get; init; }
        public int Height { get; init; }
        public int Quality { get; init; }
    }
}