using MemoApp.Core.Services.ImageGenerators;
using Microsoft.Extensions.Logging;

namespace MemoApp.Core.Examples;

/// <summary>
/// Example usage of the image generation service
/// </summary>
public class ImageGenerationExample
{
    private readonly IImageGenerator _imageGenerator;
    private readonly ILogger<ImageGenerationExample> _logger;

    public ImageGenerationExample(IImageGenerator imageGenerator, ILogger<ImageGenerationExample> logger)
    {
        _imageGenerator = imageGenerator;
        _logger = logger;
    }

    /// <summary>
    /// Demonstrates generating a single image from a description
    /// </summary>
    public async Task<ImageGenerationResult> GenerateSingleImageAsync(string description)
    {
        _logger.LogInformation("Generating image for description '{Description}'", description);

        var options = new ImageGenerationOptions
        {
            Width = 1024,
            Height = 1024,
            Style = "vivid",
            AdditionalContext = "Make it colorful and visually appealing",
            DownloadImageData = true
        };

        var result = await _imageGenerator.GenerateImageAsync(description, options);

        if (result.IsSuccess)
        {
            _logger.LogInformation("Successfully generated image for '{Description}'. Image size: {Size} bytes", 
                description, result.ImageData?.Length ?? 0);
        }
        else
        {
            _logger.LogError("Failed to generate image for '{Description}': {Error}", description, result.ErrorMessage);
        }

        return result;
    }

    /// <summary>
    /// Demonstrates generating images for multiple descriptions
    /// </summary>
    public async Task<List<ImageGenerationResult>> GenerateBatchImagesAsync()
    {
        var descriptions = new[]
        {
            "a cup of tea on a wooden table",
            "an old wise man with a long beard", 
            "a caring mother holding a child",
            "a field of golden rye grain",
            "a judge's gavel on a law book",
            "a red leather shoe",
            "a black and white cow in a meadow",
            "green ivy climbing up a brick wall",
            "a slice of apple pie with steam",
            "ten painted toes on sandy beach"
        };

        _logger.LogInformation("Generating images for {Count} descriptions", descriptions.Length);

        var options = new ImageGenerationOptions
        {
            Width = 1024,
            Height = 1024,
            Style = "cartoon",
            AdditionalContext = "Create simple, iconic representations with clear details",
            DownloadImageData = false // Only get URLs for batch processing
        };

        var results = await _imageGenerator.GenerateBatchAsync(descriptions, options);

        var successCount = results.Count(r => r.IsSuccess);
        _logger.LogInformation("Batch generation completed. Success: {Success}/{Total}", successCount, results.Count);

        return results;
    }

    /// <summary>
    /// Demonstrates checking service availability
    /// </summary>
    public async Task<bool> CheckServiceAsync()
    {
        _logger.LogInformation("Checking image generation service availability...");
        
        var isAvailable = await _imageGenerator.IsAvailableAsync();
        
        if (isAvailable)
        {
            _logger.LogInformation("Image generation service is available and ready");
        }
        else
        {
            _logger.LogWarning("Image generation service is not available");
        }

        return isAvailable;
    }

    /// <summary>
    /// Demonstrates generating images for custom descriptions
    /// </summary>
    public async Task<List<ImageGenerationResult>> GenerateCustomDescriptionsAsync(
        IEnumerable<string> descriptions)
    {
        var descriptionsArray = descriptions.ToArray();
        _logger.LogInformation("Generating images for {Count} custom descriptions", descriptionsArray.Length);

        var options = new ImageGenerationOptions
        {
            Width = 512,
            Height = 512,
            Style = "natural",
            AdditionalContext = "Focus on clarity and visual appeal",
            DownloadImageData = true
        };

        return await _imageGenerator.GenerateBatchAsync(descriptionsArray, options);
    }
}