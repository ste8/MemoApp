using MemoApp.Core.Packs;

namespace MemoApp.Core.Services.Packs;

public interface IPackManagerService
{
    Task<List<Pack>> GetDownloadedPacksAsync();
    Task<Pack?> GetCurrentPackAsync();
    Task<bool> SetCurrentPackAsync(string packId);
    Task<string> GetPackDirectoryAsync(string packId);
    Task<bool> RegisterDownloadedPackAsync(Pack pack, string localPath);
    Task<bool> UnregisterPackAsync(string packId);
    Task<bool> GenerateThumbnailsAsync(string packId, IProgress<(int processed, int total, string currentFile)>? progress = null);
    Task<string?> BackupPackAsync(string packId);
    Task<bool> RestorePackFromBackupAsync(string packId, string backupPath);
    Task<PackManifest?> GetPackManifestAsync(string packId);
    Task<bool> HasAnyPackDownloadedAsync();
    Task<PackSettings> GetPackSettingsAsync();
    Task SavePackSettingsAsync(PackSettings settings);
}