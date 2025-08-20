using MemoApp.Core.Packs;

namespace MemoApp.Core.Services.Packs;

public interface IPackDownloadService
{
    Task<PackIndex?> GetAvailablePacksAsync(bool forceRefresh = false);
    Task<DownloadProgress> DownloadPackAsync(Pack pack, IProgress<DownloadProgress>? progress = null, CancellationToken cancellationToken = default);
    Task<bool> ExtractPackAsync(string zipPath, string destinationPath, IProgress<string>? progress = null);
    Task<bool> ValidatePackIntegrityAsync(string packPath);
    Task<bool> DeletePackAsync(string packId);
    Task<bool> UpdatePackAsync(Pack pack, IProgress<DownloadProgress>? progress = null, CancellationToken cancellationToken = default);
    Task<long> GetAvailableStorageSpaceAsync();
    Task<bool> HasSufficientStorageAsync(long requiredBytes);
    void ClearCache();
}