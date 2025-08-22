namespace MemoApp.Core.MajorSystem.Models;

/// <summary>
/// Defines the different sources for association images
/// </summary>
public enum ImageSourceType
{
    /// <summary>
    /// Image selected from device gallery or file system
    /// </summary>
    Device,
    
    /// <summary>
    /// Image generated using AI (future premium feature)
    /// </summary>
    AIGeneration
}