using MemoApp.Core.MajorSystem.Models;

namespace MemoApp.Core.MajorSystem.Services;

/// <summary>
/// Service for handling different image sources (device, AI generation, etc.)
/// </summary>
public interface ICustomImageSourceService
{
    /// <summary>
    /// Opens the device image picker and returns the selected image stream
    /// </summary>
    /// <returns>Image stream if selected, null if cancelled</returns>
    Task<Stream?> PickImageFromDeviceAsync();
    
    /// <summary>
    /// Generates an image using AI based on the provided prompt
    /// </summary>
    /// <param name="word">The word to generate an image for</param>
    /// <param name="style">The generation style (realistic, cartoon, etc.)</param>
    /// <param name="additionalPrompt">Additional prompt details</param>
    /// <returns>Generated image stream if successful, null if failed</returns>
    Task<Stream?> GenerateImageWithAIAsync(string word, string style = "realistic", string? additionalPrompt = null);
    
    /// <summary>
    /// Checks if AI generation is available and user has sufficient credits
    /// </summary>
    /// <returns>True if AI generation is available</returns>
    Task<bool> IsAIGenerationAvailableAsync();
    
    /// <summary>
    /// Gets the user's remaining AI generation credits
    /// </summary>
    /// <returns>Number of remaining credits</returns>
    Task<int> GetRemainingCreditsAsync();
    
    /// <summary>
    /// Gets the cost of a single AI generation in credits
    /// </summary>
    /// <returns>Cost in credits</returns>
    int GetAIGenerationCost();
    
    /// <summary>
    /// Gets the available AI generation styles
    /// </summary>
    /// <returns>List of available styles</returns>
    List<string> GetAvailableStyles();
}