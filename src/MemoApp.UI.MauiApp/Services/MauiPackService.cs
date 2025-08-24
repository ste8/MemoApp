using MemoApp.Core.Services.Packs;
using Microsoft.Extensions.Logging;

namespace MemoApp.UI.MauiApp.Services;

/// <summary>
/// MAUI-specific pack service that handles thumbnail generation
/// </summary>
public class MauiPackService
{
    private readonly IThumbnailService _thumbnailService;
    private readonly IPackManagerService _packManager;
    private readonly ILogger<MauiPackService> _logger;

    public MauiPackService(
        IThumbnailService thumbnailService,
        IPackManagerService packManager,
        ILogger<MauiPackService> logger)
    {
        _thumbnailService = thumbnailService;
        _packManager = packManager;
        _logger = logger;
    }

    public async Task<bool> GenerateThumbnailsForPackAsync(
        string packId,
        IProgress<(int processed, int total, string currentFile)>? progress = null)
    {
        try
        {
            var packPath = await _packManager.GetPackDirectoryAsync(packId);
            var imagesPath = Path.Combine(packPath, "major_system", "images");
            var thumbnailsPath = Path.Combine(packPath, "major_system", "thumbnails");

            if (!Directory.Exists(imagesPath))
            {
                _logger.LogWarning("Images directory not found for pack {PackId}", packId);
                return false;
            }

            // Get optimal thumbnail size for device
            var (width, height) = _thumbnailService.GetDeviceOptimalThumbnailSize();

            // Generate thumbnails
            var count = await _thumbnailService.GenerateThumbnailsForDirectoryAsync(
                imagesPath, thumbnailsPath, width, height,
                maintainAspectRatio: true, quality: 85, progress);

            if (count > 0)
            {
                // Let pack manager know thumbnails are generated
                await _packManager.GenerateThumbnailsAsync(packId, progress);
            }

            _logger.LogInformation("Generated {Count} thumbnails for pack {PackId}", count, packId);
            return count > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate thumbnails for pack {PackId}", packId);
            return false;
        }
    }
}