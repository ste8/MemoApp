namespace MemoApp.Core.Services.ImageGenerators;

/// <summary>
/// Configuration options for OpenAI image generation service
/// </summary>
public class OpenAIImageGeneratorOptions
{
    /// <summary>
    /// OpenAI API key
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;
    
    /// <summary>
    /// OpenAI organization ID (optional)
    /// </summary>
    public string? OrganizationId { get; set; }
    
    /// <summary>
    /// Base URL for OpenAI API (optional, uses default if not specified)
    /// </summary>
    public string? BaseUrl { get; set; }
    
    /// <summary>
    /// HTTP timeout for API requests in seconds (default: 60)
    /// </summary>
    public int TimeoutSeconds { get; set; } = 60;
    
    /// <summary>
    /// Maximum number of concurrent requests (default: 3)
    /// </summary>
    public int MaxConcurrentRequests { get; set; } = 3;
    
    /// <summary>
    /// Default image generation options
    /// </summary>
    public ImageGenerationOptions DefaultGenerationOptions { get; set; } = new()
    {
        Width = 1024,
        Height = 1024,
        Style = "vivid",
        DownloadImageData = true,
        AdditionalContext = "Create a clear, high-quality image with good contrast and vibrant colors."
    };
}