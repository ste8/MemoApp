namespace MemoApp.UI.MauiApp.Services;

public interface IThumbnailService
{
    Task<bool> GenerateThumbnailAsync(
        string sourcePath, 
        string destinationPath, 
        int width, 
        int height, 
        bool maintainAspectRatio = true,
        int quality = 85);
    
    Task<int> GenerateThumbnailsForDirectoryAsync(
        string sourceDirectory,
        string thumbnailDirectory,
        int width,
        int height,
        bool maintainAspectRatio = true,
        int quality = 85,
        IProgress<(int processed, int total, string currentFile)>? progress = null,
        CancellationToken cancellationToken = default);
    
    (int width, int height) GetDeviceOptimalThumbnailSize();
    
    Task<bool> IsThumbnailGeneratedAsync(string originalPath, string thumbnailPath);
}