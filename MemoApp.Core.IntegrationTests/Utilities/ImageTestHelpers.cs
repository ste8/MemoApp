namespace MemoApp.Core.IntegrationTests.Utilities;

/// <summary>
/// Helper utilities for image-related testing
/// </summary>
public static class ImageTestHelpers
{
    /// <summary>
    /// PNG file signature bytes
    /// </summary>
    public static readonly byte[] PngSignature = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };

    /// <summary>
    /// JPEG file signature bytes
    /// </summary>
    public static readonly byte[] JpegSignature = { 0xFF, 0xD8, 0xFF };

    /// <summary>
    /// Validates if the byte array represents a valid PNG image
    /// </summary>
    /// <param name="imageData">Image data to validate</param>
    /// <returns>True if valid PNG, false otherwise</returns>
    public static bool IsValidPng(byte[]? imageData)
    {
        if (imageData == null || imageData.Length < PngSignature.Length)
            return false;

        return imageData.Take(PngSignature.Length).SequenceEqual(PngSignature);
    }

    /// <summary>
    /// Validates if the byte array represents a valid JPEG image
    /// </summary>
    /// <param name="imageData">Image data to validate</param>
    /// <returns>True if valid JPEG, false otherwise</returns>
    public static bool IsValidJpeg(byte[]? imageData)
    {
        if (imageData == null || imageData.Length < JpegSignature.Length)
            return false;

        return imageData.Take(JpegSignature.Length).SequenceEqual(JpegSignature);
    }

    /// <summary>
    /// Validates if the byte array represents a valid image (PNG or JPEG)
    /// </summary>
    /// <param name="imageData">Image data to validate</param>
    /// <returns>True if valid image format, false otherwise</returns>
    public static bool IsValidImage(byte[]? imageData)
    {
        return IsValidPng(imageData) || IsValidJpeg(imageData);
    }

    /// <summary>
    /// Gets the estimated image format based on the byte signature
    /// </summary>
    /// <param name="imageData">Image data to analyze</param>
    /// <returns>Image format name or "Unknown"</returns>
    public static string GetImageFormat(byte[]? imageData)
    {
        if (IsValidPng(imageData))
            return "PNG";
        
        if (IsValidJpeg(imageData))
            return "JPEG";
        
        return "Unknown";
    }

    /// <summary>
    /// Validates that an image meets minimum size requirements
    /// </summary>
    /// <param name="imageData">Image data to validate</param>
    /// <param name="minSizeBytes">Minimum size in bytes (default: 1KB)</param>
    /// <returns>True if image meets size requirements</returns>
    public static bool MeetsSizeRequirements(byte[]? imageData, int minSizeBytes = 1024)
    {
        return imageData != null && imageData.Length >= minSizeBytes;
    }

    /// <summary>
    /// Creates a summary of image data for logging/testing purposes
    /// </summary>
    /// <param name="imageData">Image data to summarize</param>
    /// <returns>Summary string with format and size information</returns>
    public static string GetImageSummary(byte[]? imageData)
    {
        if (imageData == null)
            return "No image data";

        var format = GetImageFormat(imageData);
        var sizeKb = Math.Round(imageData.Length / 1024.0, 2);
        
        return $"{format} image ({sizeKb} KB, {imageData.Length} bytes)";
    }

    /// <summary>
    /// Validates URL format for image URLs
    /// </summary>
    /// <param name="imageUrl">URL to validate</param>
    /// <returns>True if URL appears to be a valid image URL</returns>
    public static bool IsValidImageUrl(string? imageUrl)
    {
        if (string.IsNullOrWhiteSpace(imageUrl))
            return false;

        if (!Uri.TryCreate(imageUrl, UriKind.Absolute, out var uri))
            return false;

        // Check if it's HTTP/HTTPS
        return uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps;
    }
}