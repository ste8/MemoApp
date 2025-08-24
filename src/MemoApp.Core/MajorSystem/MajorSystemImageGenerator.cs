using MemoApp.Core.Services.ImageGenerators;
using Microsoft.Extensions.Logging;

namespace MemoApp.Core.MajorSystem;

/// <summary>
/// Major System-specific wrapper for image generation that adds mnemonic context
/// </summary>
public class MajorSystemImageGenerator
{
    private readonly IImageGenerator _imageGenerator;
    private readonly ILogger<MajorSystemImageGenerator> _logger;

    public MajorSystemImageGenerator(IImageGenerator imageGenerator, ILogger<MajorSystemImageGenerator> logger)
    {
        _imageGenerator = imageGenerator ?? throw new ArgumentNullException(nameof(imageGenerator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Generates an image for a Major System word with mnemonic context
    /// </summary>
    /// <param name="word">The Major System word</param>
    /// <param name="number">The number this word represents</param>
    /// <param name="options">Base generation options (will be enhanced with Major System context)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The generation result</returns>
    public async Task<ImageGenerationResult> GenerateImageForWordAsync(
        string word, 
        int number, 
        ImageGenerationOptions? options = null, 
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(word))
            return ImageGenerationResult.Failure("Word cannot be null or empty");

        _logger.LogInformation("Generating Major System image for word '{Word}' (number {Number})", word, number);

        // Build Major System specific description
        var description = BuildMajorSystemDescription(word, number);
        
        // Enhance options with Major System context
        var enhancedOptions = EnhanceOptionsForMajorSystem(options, word, number);

        return await _imageGenerator.GenerateImageAsync(description, enhancedOptions, cancellationToken);
    }

    /// <summary>
    /// Generates images for multiple Major System words
    /// </summary>
    /// <param name="wordNumberPairs">Collection of word-number pairs</param>
    /// <param name="options">Base generation options</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Dictionary mapping numbers to their generation results</returns>
    public async Task<Dictionary<int, ImageGenerationResult>> GenerateBatchAsync(
        IEnumerable<(string word, int number)> wordNumberPairs,
        ImageGenerationOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var pairs = wordNumberPairs.ToArray();
        _logger.LogInformation("Generating Major System images for {Count} word-number pairs", pairs.Length);

        // Convert to descriptions with Major System context
        var descriptions = pairs.Select(pair => BuildMajorSystemDescription(pair.word, pair.number)).ToArray();
        
        // Use first pair to enhance options (they should be similar for all)
        var enhancedOptions = pairs.Length > 0 
            ? EnhanceOptionsForMajorSystem(options, pairs[0].word, pairs[0].number)
            : EnhanceOptionsForMajorSystem(options, "", 0);

        // Generate images
        var results = await _imageGenerator.GenerateBatchAsync(descriptions, enhancedOptions, cancellationToken);

        // Map results back to numbers
        var mappedResults = new Dictionary<int, ImageGenerationResult>();
        for (int i = 0; i < pairs.Length && i < results.Count; i++)
        {
            mappedResults[pairs[i].number] = results[i];
        }

        var successCount = mappedResults.Values.Count(r => r.IsSuccess);
        _logger.LogInformation("Major System batch generation completed. Success: {Success}/{Total}", 
            successCount, mappedResults.Count);

        return mappedResults;
    }

    /// <summary>
    /// Checks if the underlying image generation service is available
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if the service is available</returns>
    public Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default)
    {
        return _imageGenerator.IsAvailableAsync(cancellationToken);
    }

    private string BuildMajorSystemDescription(string word, int number)
    {
        // Create a clear, memorable description that incorporates the word and its mnemonic purpose
        return $"a clear, memorable illustration of '{word}' that would help someone remember the number {number} using mnemonic techniques";
    }

    private ImageGenerationOptions EnhanceOptionsForMajorSystem(ImageGenerationOptions? baseOptions, string word, int number)
    {
        var options = baseOptions ?? new ImageGenerationOptions();
        
        // Build Major System specific context
        var majorSystemContext = BuildMajorSystemContext(word, number);
        
        // Combine existing context with Major System context
        var combinedContext = string.IsNullOrWhiteSpace(options.AdditionalContext)
            ? majorSystemContext
            : $"{options.AdditionalContext}. {majorSystemContext}";

        return new ImageGenerationOptions
        {
            Width = options.Width,
            Height = options.Height,
            Style = options.Style,
            DownloadImageData = options.DownloadImageData,
            AdditionalContext = combinedContext
        };
    }

    private string BuildMajorSystemContext(string word, int number)
    {
        return $"The image should be simple, distinctive, and easy to visualize mentally for memory training purposes. " +
               $"Use bright, contrasting colors and avoid text or numbers in the image. " +
               $"Focus on creating a memorable visual that clearly represents '{word}' in a way that helps remember the number {number}.";
    }
}