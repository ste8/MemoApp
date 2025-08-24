namespace MemoApp.Core.MajorSystem.Services;

/// <summary>
/// Helper class for image format handling and file extensions
/// </summary>
public static class ImageFormatHelper
{
    /// <summary>
    /// Supported image file extensions
    /// </summary>
    public static readonly string[] SupportedExtensions = { ".png", ".webp" };
    
    /// <summary>
    /// Default image extension for new images (PNG preferred for MAUI compatibility)
    /// </summary>
    public const string DefaultExtension = ".png";
    
    /// <summary>
    /// WebP extension for future compatibility when native support is available
    /// </summary>
    public const string WebPExtension = ".webp";
    
    /// <summary>
    /// Gets all supported file patterns for Directory.GetFiles()
    /// </summary>
    /// <returns>Array of file patterns like "*.png", "*.webp"</returns>
    public static string[] GetFilePatterns()
    {
        return SupportedExtensions.Select(ext => $"*{ext}").ToArray();
    }
    
    /// <summary>
    /// Gets files in a directory with supported image extensions
    /// </summary>
    /// <param name="directoryPath">Directory to search</param>
    /// <param name="searchOption">Search option (default: TopDirectoryOnly)</param>
    /// <returns>Array of file paths with supported image extensions</returns>
    public static string[] GetImageFiles(string directoryPath, SearchOption searchOption = SearchOption.TopDirectoryOnly)
    {
        if (!Directory.Exists(directoryPath))
            return Array.Empty<string>();
            
        var allFiles = new List<string>();
        
        foreach (var pattern in GetFilePatterns())
        {
            try
            {
                var files = Directory.GetFiles(directoryPath, pattern, searchOption);
                allFiles.AddRange(files);
            }
            catch (Exception)
            {
                // Continue with other patterns if one fails
            }
        }
        
        return allFiles.ToArray();
    }
    
    /// <summary>
    /// Gets files in a directory matching a token pattern with supported extensions
    /// </summary>
    /// <param name="directoryPath">Directory to search</param>
    /// <param name="tokenPattern">Token pattern like "token_*"</param>
    /// <returns>Array of file paths matching the pattern with supported extensions</returns>
    public static string[] GetImageFilesWithPattern(string directoryPath, string tokenPattern)
    {
        if (!Directory.Exists(directoryPath))
            return Array.Empty<string>();
            
        var allFiles = new List<string>();
        
        foreach (var extension in SupportedExtensions)
        {
            try
            {
                var pattern = $"{tokenPattern}{extension}";
                var files = Directory.GetFiles(directoryPath, pattern);
                allFiles.AddRange(files);
            }
            catch (Exception)
            {
                // Continue with other patterns if one fails
            }
        }
        
        return allFiles.ToArray();
    }
    
    /// <summary>
    /// Determines the best extension to use for a new image file
    /// Currently returns PNG for MAUI compatibility, WebP support planned for future
    /// </summary>
    /// <param name="preferWebP">Whether to prefer WebP format (currently ignored)</param>
    /// <returns>File extension to use</returns>
    public static string GetPreferredExtension(bool preferWebP = true)
    {
        // Return PNG for now, WebP support planned when native MAUI support is available
        return DefaultExtension;
    }
    
    /// <summary>
    /// Checks if a file extension is supported
    /// </summary>
    /// <param name="extension">File extension (with or without dot)</param>
    /// <returns>True if the extension is supported</returns>
    public static bool IsSupportedExtension(string extension)
    {
        if (string.IsNullOrEmpty(extension))
            return false;
            
        var normalizedExtension = extension.StartsWith(".") ? extension : $".{extension}";
        return SupportedExtensions.Contains(normalizedExtension, StringComparer.OrdinalIgnoreCase);
    }
}