namespace MemoApp.Core.Services.ImageGenerators;

/// <summary>
/// Represents the result of an image generation operation
/// </summary>
public class ImageGenerationResult
{
    /// <summary>
    /// Indicates whether the image generation was successful
    /// </summary>
    public bool IsSuccess { get; init; }
    
    /// <summary>
    /// The generated image as a byte array (PNG format)
    /// </summary>
    public byte[]? ImageData { get; init; }
    
    /// <summary>
    /// The URL of the generated image (if hosted remotely)
    /// </summary>
    public string? ImageUrl { get; init; }
    
    /// <summary>
    /// Error message if the generation failed
    /// </summary>
    public string? ErrorMessage { get; init; }
    
    /// <summary>
    /// The prompt that was used to generate the image
    /// </summary>
    public string? UsedPrompt { get; init; }
    
    /// <summary>
    /// Creates a successful result with image data
    /// </summary>
    public static ImageGenerationResult Success(byte[] imageData, string? imageUrl = null, string? usedPrompt = null)
        => new() { IsSuccess = true, ImageData = imageData, ImageUrl = imageUrl, UsedPrompt = usedPrompt };
    
    /// <summary>
    /// Creates a successful result with image URL only
    /// </summary>
    public static ImageGenerationResult Success(string imageUrl, string? usedPrompt = null)
        => new() { IsSuccess = true, ImageUrl = imageUrl, UsedPrompt = usedPrompt };
    
    /// <summary>
    /// Creates a failed result with error message
    /// </summary>
    public static ImageGenerationResult Failure(string errorMessage)
        => new() { IsSuccess = false, ErrorMessage = errorMessage };
}

/// <summary>
/// Configuration options for image generation
/// </summary>
public class ImageGenerationOptions
{
    /// <summary>
    /// Image width in pixels (default: 512)
    /// </summary>
    public int Width { get; init; } = 512;
    
    /// <summary>
    /// Image height in pixels (default: 512)
    /// </summary>
    public int Height { get; init; } = 512;
    
    /// <summary>
    /// Art style for the image (e.g., "cartoon", "realistic", "minimalist")
    /// </summary>
    public string Style { get; init; } = "cartoon";
    
    /// <summary>
    /// Additional context or requirements for the image
    /// </summary>
    public string? AdditionalContext { get; init; }
    
    /// <summary>
    /// Whether to download the image data or just return the URL
    /// </summary>
    public bool DownloadImageData { get; init; } = true;
}

/// <summary>
/// Service for generating images from text descriptions using AI
/// </summary>
public interface IImageGenerator
{
    /// <summary>
    /// Generates an image for a text description
    /// </summary>
    /// <param name="description">The text description to generate an image for</param>
    /// <param name="options">Generation options</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The generation result</returns>
    Task<ImageGenerationResult> GenerateImageAsync(
        string description, 
        ImageGenerationOptions? options = null, 
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Generates multiple images for a batch of text descriptions
    /// </summary>
    /// <param name="descriptions">Collection of text descriptions</param>
    /// <param name="options">Generation options</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of generation results in the same order as input</returns>
    Task<List<ImageGenerationResult>> GenerateBatchAsync(
        IEnumerable<string> descriptions,
        ImageGenerationOptions? options = null,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Checks if the service is available and properly configured
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if the service is available</returns>
    Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default);
}