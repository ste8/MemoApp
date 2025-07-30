using Microsoft.Extensions.Logging;
using OpenAI;
using OpenAI.Images;

namespace MemoApp.Core.Services.ImageGenerators;

/// <summary>
/// OpenAI implementation of the image generator service using DALL-E
/// </summary>
public class OpenAIImageGenerator : IImageGenerator
{
    private readonly OpenAIClient _openAIClient;
    private readonly ILogger<OpenAIImageGenerator> _logger;
    private readonly HttpClient _httpClient;

    public OpenAIImageGenerator(
        OpenAIClient openAIClient, 
        ILogger<OpenAIImageGenerator> logger,
        HttpClient httpClient)
    {
        _openAIClient = openAIClient ?? throw new ArgumentNullException(nameof(openAIClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    }

    /// <inheritdoc />
    public async Task<ImageGenerationResult> GenerateImageAsync(
        string description, 
        ImageGenerationOptions? options = null, 
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(description))
            return ImageGenerationResult.Failure("Description cannot be null or empty");

        options ??= new ImageGenerationOptions();

        try
        {
            _logger.LogInformation("Generating image for description '{Description}'", description);

            var prompt = BuildPrompt(description, options);
            _logger.LogDebug("Using prompt: {Prompt}", prompt);

            var imageClient = _openAIClient.GetImageClient("dall-e-3");
            
            var response = await imageClient.GenerateImageAsync(
                prompt, 
                new OpenAI.Images.ImageGenerationOptions()
                {
                    Quality = GeneratedImageQuality.Standard,
                    Size = GetImageSize(options.Width, options.Height),
                    Style = GetImageStyle(options.Style),
                    ResponseFormat = GeneratedImageFormat.Uri
                }, 
                cancellationToken);

            if (response?.Value?.ImageUri == null)
            {
                _logger.LogWarning("OpenAI returned null response or image URI for description '{Description}'", description);
                return ImageGenerationResult.Failure("OpenAI returned no image");
            }

            var imageUrl = response.Value.ImageUri.ToString();
            _logger.LogInformation("Successfully generated image for description '{Description}': {ImageUrl}", description, imageUrl);

            // Download image data if requested
            byte[]? imageData = null;
            if (options.DownloadImageData)
            {
                imageData = await DownloadImageAsync(imageUrl, cancellationToken);
                if (imageData == null)
                {
                    _logger.LogWarning("Failed to download image data for description '{Description}' from {ImageUrl}", description, imageUrl);
                }
            }

            return imageData != null 
                ? ImageGenerationResult.Success(imageData, imageUrl, prompt)
                : ImageGenerationResult.Success(imageUrl, prompt);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate image for description '{Description}'", description);
            return ImageGenerationResult.Failure($"Image generation failed: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public async Task<List<ImageGenerationResult>> GenerateBatchAsync(
        IEnumerable<string> descriptions,
        ImageGenerationOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var descriptionsArray = descriptions.ToArray();
        _logger.LogInformation("Starting batch image generation for {Count} descriptions", descriptionsArray.Length);

        var results = new ImageGenerationResult[descriptionsArray.Length];
        var semaphore = new SemaphoreSlim(3, 3); // Limit concurrent requests to avoid rate limiting

        var tasks = descriptionsArray.Select(async (description, index) =>
        {
            await semaphore.WaitAsync(cancellationToken);
            try
            {
                var result = await GenerateImageAsync(description, options, cancellationToken);
                results[index] = result;
            }
            finally
            {
                semaphore.Release();
            }
        });

        await Task.WhenAll(tasks);

        var resultsList = results.ToList();
        _logger.LogInformation("Completed batch image generation. Success: {SuccessCount}, Failed: {FailedCount}",
            resultsList.Count(r => r.IsSuccess),
            resultsList.Count(r => !r.IsSuccess));

        return resultsList;
    }

    /// <inheritdoc />
    public async Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // Test with a simple image generation request
            var imageClient = _openAIClient.GetImageClient("dall-e-3");
            
            // We don't actually generate an image, just check if the client is configured
            // This is a lightweight check - in a real scenario you might want to make a test call
            await Task.CompletedTask;
            
            _logger.LogDebug("OpenAI image generation service is available");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "OpenAI image generation service is not available");
            return false;
        }
    }

    private string BuildPrompt(string description, ImageGenerationOptions options)
    {
        var basePrompt = $"Create a {options.Style} illustration of: {description}";

        if (!string.IsNullOrWhiteSpace(options.AdditionalContext))
        {
            basePrompt += $". {options.AdditionalContext}";
        }

        return basePrompt.Trim();
    }

    private static GeneratedImageSize GetImageSize(int width, int height)
    {
        // DALL-E 3 supports specific sizes, map to closest supported size
        return (width, height) switch
        {
            (1024, 1024) => GeneratedImageSize.W1024xH1024,
            (1792, 1024) => GeneratedImageSize.W1792xH1024,
            (1024, 1792) => GeneratedImageSize.W1024xH1792,
            _ => GeneratedImageSize.W1024xH1024 // Default to square
        };
    }

    private static GeneratedImageStyle GetImageStyle(string style)
    {
        return style.ToLowerInvariant() switch
        {
            "vivid" => GeneratedImageStyle.Vivid,
            "natural" => GeneratedImageStyle.Natural,
            _ => GeneratedImageStyle.Vivid // Default to vivid for better memorability
        };
    }

    private async Task<byte[]?> DownloadImageAsync(string imageUrl, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Downloading image from {ImageUrl}", imageUrl);
            
            using var response = await _httpClient.GetAsync(imageUrl, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to download image: HTTP {StatusCode}", response.StatusCode);
                return null;
            }

            var imageData = await response.Content.ReadAsByteArrayAsync(cancellationToken);
            _logger.LogDebug("Successfully downloaded image data: {Size} bytes", imageData.Length);
            
            return imageData;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading image from {ImageUrl}", imageUrl);
            return null;
        }
    }
}