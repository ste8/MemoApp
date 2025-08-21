namespace MemoApp.Core.MajorSystem.Models;

/// <summary>
/// Represents a single token association with its word and image paths
/// </summary>
public class TokenAssociation
{
    /// <summary>
    /// The token (e.g., "01", "A", "@")
    /// </summary>
    public string Token { get; set; } = string.Empty;
    
    /// <summary>
    /// The associated word extracted from filename
    /// </summary>
    public string Word { get; set; } = string.Empty;
    
    /// <summary>
    /// Full path to the main image
    /// </summary>
    public string ImagePath { get; set; } = string.Empty;
    
    /// <summary>
    /// Full path to the thumbnail image
    /// </summary>
    public string ThumbnailPath { get; set; } = string.Empty;
    
    /// <summary>
    /// Whether this is the default association for the token
    /// </summary>
    public bool IsDefault { get; set; }
    
    /// <summary>
    /// The category this association belongs to
    /// </summary>
    public string Category { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets the filename from the image path
    /// </summary>
    public string FileName => string.IsNullOrEmpty(ImagePath) 
        ? string.Empty 
        : Path.GetFileName(ImagePath);
}