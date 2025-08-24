namespace MemoApp.Core.MajorSystem.Services;

/// <summary>
/// Service for image processing operations (resize, thumbnail generation, etc.)
/// </summary>
public interface IImageProcessingService
{
    /// <summary>
    /// Generates a thumbnail from the source image
    /// </summary>
    /// <param name="sourceImagePath">Path to the source image</param>
    /// <param name="thumbnailPath">Path where the thumbnail should be saved</param>
    /// <param name="maxWidth">Maximum width for the thumbnail (default: 200)</param>
    /// <param name="maxHeight">Maximum height for the thumbnail (default: 200)</param>
    /// <returns>True if thumbnail was generated successfully</returns>
    Task<bool> GenerateThumbnailAsync(string sourceImagePath, string thumbnailPath, int maxWidth = 200, int maxHeight = 200);
    
    /// <summary>
    /// Resizes an image if it exceeds the maximum dimensions
    /// </summary>
    /// <param name="sourceImagePath">Path to the source image</param>
    /// <param name="outputPath">Path where the resized image should be saved</param>
    /// <param name="maxWidth">Maximum width (default: 1024)</param>
    /// <param name="maxHeight">Maximum height (default: 1024)</param>
    /// <returns>True if image was processed successfully</returns>
    Task<bool> ResizeImageAsync(string sourceImagePath, string outputPath, int maxWidth = 1024, int maxHeight = 1024);
    
    /// <summary>
    /// Gets the dimensions of an image
    /// </summary>
    /// <param name="imagePath">Path to the image</param>
    /// <returns>Tuple containing width and height, or (0, 0) if failed</returns>
    Task<(int width, int height)> GetImageDimensionsAsync(string imagePath);
    
    /// <summary>
    /// Validates that a file is a valid image
    /// </summary>
    /// <param name="imagePath">Path to the image file</param>
    /// <returns>True if the file is a valid image</returns>
    Task<bool> IsValidImageAsync(string imagePath);
    
    /// <summary>
    /// Gets the file size of an image in bytes
    /// </summary>
    /// <param name="imagePath">Path to the image file</param>
    /// <returns>File size in bytes, or 0 if file doesn't exist</returns>
    long GetImageFileSize(string imagePath);
}