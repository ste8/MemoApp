using System.Text.Json;
using MemoApp.Core.Packs;
using Microsoft.Extensions.Logging;

namespace MemoApp.Core.Services.Packs;

public class PackManagerService : IPackManagerService
{
    private readonly ILogger<PackManagerService> _logger;
    private readonly string _packsRootDirectory;
    private readonly string _settingsFilePath;
    private PackSettings? _settings;
    private readonly SemaphoreSlim _settingsLock = new(1, 1);

    public PackManagerService(ILogger<PackManagerService> logger, string dataDirectory)
    {
        _logger = logger;
        _packsRootDirectory = Path.Combine(dataDirectory, "packs");
        _settingsFilePath = Path.Combine(dataDirectory, "pack_settings.json");
        
        // Ensure packs directory exists
        Directory.CreateDirectory(_packsRootDirectory);
    }

    public async Task<List<Pack>> GetDownloadedPacksAsync()
    {
        var settings = await GetPackSettingsAsync();
        var packs = new List<Pack>();

        foreach (var downloadedPack in settings.DownloadedPacks)
        {
            var manifest = await GetPackManifestAsync(downloadedPack.Id);
            if (manifest != null)
            {
                packs.Add(new Pack
                {
                    Id = manifest.Id,
                    InternationalName = manifest.InternationalName,
                    NativeName = manifest.NativeName,
                    Description = manifest.Description,
                    NativeDescription = manifest.NativeDescription,
                    Version = manifest.Version,
                    LanguageCode = manifest.LanguageCode,
                    Author = manifest.Author,
                    IsDownloaded = true,
                    DownloadedAt = downloadedPack.DownloadedAt,
                    LocalPath = downloadedPack.Path,
                    ThumbnailsGenerated = downloadedPack.ThumbnailsGenerated
                });
            }
        }

        return packs;
    }

    public async Task<Pack?> GetCurrentPackAsync()
    {
        var settings = await GetPackSettingsAsync();
        
        if (string.IsNullOrEmpty(settings.CurrentPackId))
            return null;

        var downloadedPacks = await GetDownloadedPacksAsync();
        return downloadedPacks.FirstOrDefault(p => p.Id == settings.CurrentPackId);
    }

    public async Task<bool> SetCurrentPackAsync(string packId)
    {
        try
        {
            var settings = await GetPackSettingsAsync();
            
            // Verify pack exists
            var packExists = settings.DownloadedPacks.Any(p => p.Id == packId);
            if (!packExists)
            {
                _logger.LogWarning("Cannot set current pack to {PackId} - pack not found", packId);
                return false;
            }

            settings.CurrentPackId = packId;
            await SavePackSettingsAsync(settings);
            
            _logger.LogInformation("Set current pack to {PackId}", packId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to set current pack to {PackId}", packId);
            return false;
        }
    }

    public Task<string> GetPackDirectoryAsync(string packId)
    {
        var path = Path.Combine(_packsRootDirectory, packId);
        return Task.FromResult(path);
    }

    public async Task<bool> RegisterDownloadedPackAsync(Pack pack, string localPath)
    {
        try
        {
            var settings = await GetPackSettingsAsync();
            
            // Remove existing entry if updating
            settings.DownloadedPacks.RemoveAll(p => p.Id == pack.Id);
            
            // Add new entry
            settings.DownloadedPacks.Add(new DownloadedPackInfo
            {
                Id = pack.Id,
                Version = pack.Version,
                DownloadedAt = DateTime.UtcNow,
                Path = localPath,
                ThumbnailsGenerated = false
            });

            // Set as current if it's the first pack
            if (settings.DownloadedPacks.Count == 1)
            {
                settings.CurrentPackId = pack.Id;
            }

            await SavePackSettingsAsync(settings);
            
            _logger.LogInformation("Registered downloaded pack {PackId} v{Version}", pack.Id, pack.Version);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to register downloaded pack {PackId}", pack.Id);
            return false;
        }
    }

    public async Task<bool> UnregisterPackAsync(string packId)
    {
        try
        {
            var settings = await GetPackSettingsAsync();
            
            settings.DownloadedPacks.RemoveAll(p => p.Id == packId);
            
            // Clear current pack if it was the deleted one
            if (settings.CurrentPackId == packId)
            {
                settings.CurrentPackId = settings.DownloadedPacks.FirstOrDefault()?.Id;
            }

            await SavePackSettingsAsync(settings);
            
            _logger.LogInformation("Unregistered pack {PackId}", packId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to unregister pack {PackId}", packId);
            return false;
        }
    }

    public async Task<bool> GenerateThumbnailsAsync(
        string packId,
        IProgress<(int processed, int total, string currentFile)>? progress = null)
    {
        // This method is implemented in the MAUI layer
        // For now, just mark thumbnails as generated
        try
        {
            var settings = await GetPackSettingsAsync();
            var packInfo = settings.DownloadedPacks.FirstOrDefault(p => p.Id == packId);
            if (packInfo != null)
            {
                packInfo.ThumbnailsGenerated = true;
                await SavePackSettingsAsync(settings);
            }

            _logger.LogInformation("Thumbnails marked as generated for pack {PackId}", packId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to mark thumbnails as generated for pack {PackId}", packId);
            return false;
        }
    }

    public async Task<string?> BackupPackAsync(string packId)
    {
        try
        {
            var packPath = await GetPackDirectoryAsync(packId);
            if (!Directory.Exists(packPath))
            {
                _logger.LogWarning("Pack directory not found for backup: {PackId}", packId);
                return null;
            }

            var backupPath = $"{packPath}_backup_{DateTime.UtcNow:yyyyMMddHHmmss}";
            
            await Task.Run(() =>
            {
                CopyDirectory(packPath, backupPath, true);
            });

            _logger.LogInformation("Created backup of pack {PackId} at {Path}", packId, backupPath);
            return backupPath;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to backup pack {PackId}", packId);
            return null;
        }
    }

    public async Task<bool> RestorePackFromBackupAsync(string packId, string backupPath)
    {
        try
        {
            if (!Directory.Exists(backupPath))
            {
                _logger.LogWarning("Backup directory not found: {Path}", backupPath);
                return false;
            }

            var packPath = await GetPackDirectoryAsync(packId);
            
            // Delete current pack directory if exists
            if (Directory.Exists(packPath))
            {
                Directory.Delete(packPath, recursive: true);
            }

            // Restore from backup
            await Task.Run(() =>
            {
                CopyDirectory(backupPath, packPath, true);
            });

            _logger.LogInformation("Restored pack {PackId} from backup {Path}", packId, backupPath);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to restore pack {PackId} from backup", packId);
            return false;
        }
    }

    public async Task<PackManifest?> GetPackManifestAsync(string packId)
    {
        try
        {
            var packPath = await GetPackDirectoryAsync(packId);
            var manifestPath = Path.Combine(packPath, "manifest.json");

            if (!File.Exists(manifestPath))
            {
                _logger.LogWarning("Manifest not found for pack {PackId}", packId);
                return null;
            }

            var json = await File.ReadAllTextAsync(manifestPath);
            return JsonSerializer.Deserialize<PackManifest>(json);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to read manifest for pack {PackId}", packId);
            return null;
        }
    }

    public async Task<bool> HasAnyPackDownloadedAsync()
    {
        var settings = await GetPackSettingsAsync();
        return settings.DownloadedPacks.Count > 0;
    }

    public async Task<PackSettings> GetPackSettingsAsync()
    {
        await _settingsLock.WaitAsync();
        try
        {
            if (_settings != null)
                return _settings;

            if (File.Exists(_settingsFilePath))
            {
                try
                {
                    var json = await File.ReadAllTextAsync(_settingsFilePath);
                    _settings = JsonSerializer.Deserialize<PackSettings>(json) ?? new PackSettings();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to load pack settings, using defaults");
                    _settings = new PackSettings();
                }
            }
            else
            {
                _settings = new PackSettings();
            }

            return _settings;
        }
        finally
        {
            _settingsLock.Release();
        }
    }

    public async Task SavePackSettingsAsync(PackSettings settings)
    {
        await _settingsLock.WaitAsync();
        try
        {
            _settings = settings;
            var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions 
            { 
                WriteIndented = true 
            });
            await File.WriteAllTextAsync(_settingsFilePath, json);
            
            _logger.LogDebug("Saved pack settings");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save pack settings");
            throw;
        }
        finally
        {
            _settingsLock.Release();
        }
    }

    private static void CopyDirectory(string sourceDir, string destDir, bool recursive)
    {
        var dir = new DirectoryInfo(sourceDir);

        if (!dir.Exists)
            throw new DirectoryNotFoundException($"Source directory not found: {dir.FullName}");

        DirectoryInfo[] dirs = dir.GetDirectories();
        Directory.CreateDirectory(destDir);

        foreach (FileInfo file in dir.GetFiles())
        {
            string targetFilePath = Path.Combine(destDir, file.Name);
            file.CopyTo(targetFilePath, true);
        }

        if (recursive)
        {
            foreach (DirectoryInfo subDir in dirs)
            {
                string newDestDir = Path.Combine(destDir, subDir.Name);
                CopyDirectory(subDir.FullName, newDestDir, true);
            }
        }
    }
}