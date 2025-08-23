using System.IO.Compression;
using System.Text.Json;
using MemoApp.Core.Packs;
using Microsoft.Extensions.Logging;

namespace MemoApp.Core.Services.Packs;

public class PackDownloadService : IPackDownloadService
{
    private readonly HttpClient _httpClient;
    private readonly IPackManagerService _packManager;
    private readonly ILogger<PackDownloadService> _logger;
    private readonly string _packsIndexUrl;
    private readonly string _dataDirectory;
    private readonly int _cacheMinutes;
    private PackIndex? _cachedIndex;

    public PackDownloadService(
        HttpClient httpClient,
        IPackManagerService packManager,
        ILogger<PackDownloadService> logger,
        string packsIndexUrl,
        string dataDirectory,
        int cacheMinutes = 30)
    {
        _httpClient = httpClient;
        _packManager = packManager;
        _logger = logger;
        _packsIndexUrl = packsIndexUrl;
        _dataDirectory = dataDirectory;
        _cacheMinutes = cacheMinutes;
    }

    public async Task<PackIndex?> GetAvailablePacksAsync(bool forceRefresh = false)
    {
        try
        {
            if (!forceRefresh && _cachedIndex != null && !_cachedIndex.IsCacheExpired(_cacheMinutes))
            {
                _logger.LogDebug("Returning cached pack index");
                return _cachedIndex;
            }

            _logger.LogInformation("Fetching pack index from {Url}", _packsIndexUrl);
            var response = await _httpClient.GetAsync(_packsIndexUrl);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var index = JsonSerializer.Deserialize<PackIndex>(json);
            
            if (index != null)
            {
                index.CachedAt = DateTime.UtcNow;
                _cachedIndex = index;
                
                // Mark downloaded packs and check for updates
                var downloadedPacks = await _packManager.GetDownloadedPacksAsync();
                _logger.LogDebug("Found {DownloadedCount} downloaded packs", downloadedPacks.Count);
                
                foreach (var downloadedPack in downloadedPacks)
                {
                    _logger.LogDebug("Downloaded pack: {PackId} v{Version}", downloadedPack.Id, downloadedPack.Version);
                }
                
                foreach (var pack in index.Packs)
                {
                    var downloaded = downloadedPacks.FirstOrDefault(d => d.Id == pack.Id);
                    if (downloaded != null)
                    {
                        pack.IsDownloaded = true;
                        pack.LocalPath = downloaded.LocalPath;
                        pack.DownloadedAt = downloaded.DownloadedAt;
                        pack.ThumbnailsGenerated = downloaded.ThumbnailsGenerated;
                        
                        // Check if update is available
                        var hasUpdate = IsNewerVersion(pack.Version, downloaded.Version);
                        pack.HasUpdate = hasUpdate;
                        
                        _logger.LogDebug("Pack {PackId}: Remote v{RemoteVersion} vs Local v{LocalVersion} = HasUpdate: {HasUpdate}",
                            pack.Id, pack.Version, downloaded.Version, hasUpdate);
                        
                        if (hasUpdate)
                        {
                            _logger.LogInformation("Update available for pack {PackId}: {OldVersion} -> {NewVersion}",
                                pack.Id, downloaded.Version, pack.Version);
                        }
                    }
                    else
                    {
                        _logger.LogDebug("Pack {PackId} v{Version} is not downloaded", pack.Id, pack.Version);
                    }
                }
            }

            return index;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch pack index");
            return null;
        }
    }

    public async Task<DownloadProgress> DownloadPackAsync(
        Pack pack, 
        IProgress<DownloadProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var downloadProgress = new DownloadProgress
        {
            PackId = pack.Id,
            State = DownloadState.Pending,
            StartTime = DateTime.UtcNow,
            TotalBytes = pack.FileSize
        };

        try
        {
            if (string.IsNullOrEmpty(pack.DownloadUrl))
            {
                throw new InvalidOperationException("Pack download URL is not available");
            }

            // Check storage space
            if (!await HasSufficientStorageAsync(pack.FileSize * 2)) // Need space for zip + extracted
            {
                throw new InvalidOperationException("Insufficient storage space");
            }

            // Download the pack
            downloadProgress.State = DownloadState.Downloading;
            downloadProgress.StatusMessage = "Downloading pack...";
            progress?.Report(downloadProgress);

            var tempFile = Path.Combine(Path.GetTempPath(), $"{pack.Id}_{pack.Version}.zip");
            
            // Download the file and ensure all streams are properly closed
            using (var response = await _httpClient.GetAsync(pack.DownloadUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken))
            {
                response.EnsureSuccessStatusCode();

                var totalBytes = response.Content.Headers.ContentLength ?? pack.FileSize;
                downloadProgress.TotalBytes = totalBytes;

                using (var stream = await response.Content.ReadAsStreamAsync(cancellationToken))
                using (var fileStream = new FileStream(tempFile, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    var buffer = new byte[8192];
                    int bytesRead;
                    
                    while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, cancellationToken)) > 0)
                    {
                        await fileStream.WriteAsync(buffer, 0, bytesRead, cancellationToken);
                        downloadProgress.BytesReceived += bytesRead;
                        progress?.Report(downloadProgress);
                    }
                    
                    // Ensure the file is fully written and closed
                    await fileStream.FlushAsync(cancellationToken);
                }
            }

            // Add a small delay to ensure file handles are released
            await Task.Delay(100, cancellationToken);

            // Extract the pack
            downloadProgress.State = DownloadState.Extracting;
            downloadProgress.StatusMessage = "Extracting pack...";
            progress?.Report(downloadProgress);

            var packPath = await _packManager.GetPackDirectoryAsync(pack.Id);
            var extractionSuccess = await ExtractPackAsync(tempFile, packPath, null);
            
            if (!extractionSuccess)
            {
                throw new InvalidOperationException("Failed to extract pack contents");
            }

            // Validate pack integrity after extraction
            if (!await ValidatePackIntegrityAsync(packPath))
            {
                throw new InvalidOperationException("Pack validation failed - corrupted or missing content");
            }

            // Save pack info
            await _packManager.RegisterDownloadedPackAsync(pack, packPath);

            // Note: Thumbnail generation will be handled by the MAUI layer

            // Clean up temp file
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }

            downloadProgress.State = DownloadState.Completed;
            downloadProgress.StatusMessage = "Pack downloaded successfully";
            downloadProgress.EndTime = DateTime.UtcNow;
            progress?.Report(downloadProgress);

            _logger.LogInformation("Successfully downloaded pack {PackId} v{Version}", pack.Id, pack.Version);
        }
        catch (OperationCanceledException)
        {
            downloadProgress.State = DownloadState.Cancelled;
            downloadProgress.ErrorMessage = "Download cancelled";
            downloadProgress.EndTime = DateTime.UtcNow;
            _logger.LogWarning("Download cancelled for pack {PackId}", pack.Id);
        }
        catch (Exception ex)
        {
            downloadProgress.State = DownloadState.Failed;
            downloadProgress.ErrorMessage = ex.Message;
            downloadProgress.EndTime = DateTime.UtcNow;
            _logger.LogError(ex, "Failed to download pack {PackId}", pack.Id);
        }

        return downloadProgress;
    }

    public async Task<bool> ExtractPackAsync(string zipPath, string destinationPath, IProgress<string>? progress = null)
    {
        try
        {
            if (!File.Exists(zipPath))
            {
                _logger.LogError("Pack file not found: {ZipPath}", zipPath);
                throw new FileNotFoundException("Pack file not found", zipPath);
            }

            // Ensure destination directory exists
            Directory.CreateDirectory(destinationPath);
            _logger.LogInformation("Created destination directory: {DestPath}", destinationPath);

            progress?.Report("Extracting files...");
            
            await Task.Run(() =>
            {
                // Use a more robust extraction approach
                try
                {
                    using var archive = ZipFile.OpenRead(zipPath);
                    
                    _logger.LogInformation("Opened ZIP archive with {EntryCount} entries", archive.Entries.Count);
                    
                    if (archive.Entries.Count == 0)
                    {
                        _logger.LogWarning("ZIP archive is empty: {ZipPath}", zipPath);
                        return;
                    }

                    // Log first few entries for debugging
                    foreach (var entry in archive.Entries.Take(5))
                    {
                        _logger.LogDebug("ZIP entry: {EntryName} (Size: {Size})", entry.FullName, entry.Length);
                    }
                    
                    // Extract each entry individually for better error handling
                    foreach (var entry in archive.Entries)
                    {
                        if (string.IsNullOrEmpty(entry.Name))
                        {
                            // Directory entry, create directory
                            var dirPath = Path.Combine(destinationPath, entry.FullName);
                            Directory.CreateDirectory(dirPath);
                        }
                        else
                        {
                            // File entry, extract file
                            var filePath = Path.Combine(destinationPath, entry.FullName);
                            var fileDir = Path.GetDirectoryName(filePath);
                            if (!string.IsNullOrEmpty(fileDir))
                            {
                                Directory.CreateDirectory(fileDir);
                            }
                            
                            entry.ExtractToFile(filePath, overwrite: true);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during ZIP extraction");
                    throw;
                }
            });

            // Verify extraction by checking directory contents
            var extractedFiles = Directory.GetFiles(destinationPath, "*", SearchOption.AllDirectories);
            var extractedDirs = Directory.GetDirectories(destinationPath, "*", SearchOption.AllDirectories);
            
            _logger.LogInformation("Extraction completed. Found {FileCount} files and {DirCount} directories in {Path}",
                extractedFiles.Length, extractedDirs.Length, destinationPath);

            if (extractedFiles.Length == 0 && extractedDirs.Length == 0)
            {
                _logger.LogError("Extraction completed but destination directory is empty: {Path}", destinationPath);
                return false;
            }

            // Log some extracted files for debugging
            foreach (var file in extractedFiles.Take(3))
            {
                _logger.LogDebug("Extracted file: {FilePath}", file);
            }

            progress?.Report("Extraction completed");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to extract pack from {ZipPath} to {DestPath}", zipPath, destinationPath);
            return false;
        }
    }

    public async Task<bool> ValidatePackIntegrityAsync(string packPath)
    {
        try
        {
            // Check for manifest file
            var manifestPath = Path.Combine(packPath, "manifest.json");
            if (!File.Exists(manifestPath))
            {
                _logger.LogWarning("Manifest file not found in pack at {Path}", packPath);
                return false;
            }

            // Validate manifest can be parsed
            var manifestJson = await File.ReadAllTextAsync(manifestPath);
            var manifest = JsonSerializer.Deserialize<PackManifest>(manifestJson);
            
            if (manifest == null || string.IsNullOrEmpty(manifest.Id))
            {
                _logger.LogWarning("Invalid manifest in pack at {Path}", packPath);
                return false;
            }

            // Check for major_system directory
            var majorSystemPath = Path.Combine(packPath, "major_system");
            if (!Directory.Exists(majorSystemPath))
            {
                _logger.LogWarning("Major system directory not found in pack at {Path}", packPath);
                return false;
            }

            // Check for images directory
            var imagesPath = Path.Combine(majorSystemPath, "images");
            if (!Directory.Exists(imagesPath))
            {
                _logger.LogWarning("Images directory not found in pack at {Path}", packPath);
                return false;
            }

            _logger.LogInformation("Pack integrity validated successfully at {Path}", packPath);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to validate pack integrity at {Path}", packPath);
            return false;
        }
    }

    public async Task<bool> DeletePackAsync(string packId)
    {
        try
        {
            var packPath = await _packManager.GetPackDirectoryAsync(packId);
            
            if (Directory.Exists(packPath))
            {
                Directory.Delete(packPath, recursive: true);
                _logger.LogInformation("Deleted pack {PackId} from {Path}", packId, packPath);
            }

            await _packManager.UnregisterPackAsync(packId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete pack {PackId}", packId);
            return false;
        }
    }

    public async Task<bool> UpdatePackAsync(
        Pack pack, 
        IProgress<DownloadProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Updating pack {PackId} to v{NewVersion}",
                pack.Id, pack.Version);

            // Backup current pack
            var backupPath = await _packManager.BackupPackAsync(pack.Id);
            
            try
            {
                // Clean up current pack directory to remove old images and thumbnails
                var packPath = await _packManager.GetPackDirectoryAsync(pack.Id);
                if (Directory.Exists(packPath))
                {
                    _logger.LogInformation("Cleaning up existing pack directory before update: {PackPath}", packPath);
                    Directory.Delete(packPath, recursive: true);
                }
                
                // Download and install new version
                var result = await DownloadPackAsync(pack, progress, cancellationToken);
                
                if (result.State == DownloadState.Completed)
                {
                    // Delete backup
                    if (!string.IsNullOrEmpty(backupPath) && Directory.Exists(backupPath))
                    {
                        Directory.Delete(backupPath, recursive: true);
                    }
                    return true;
                }
                else
                {
                    // Restore from backup if download failed
                    if (!string.IsNullOrEmpty(backupPath))
                    {
                        await _packManager.RestorePackFromBackupAsync(pack.Id, backupPath);
                    }
                    return false;
                }
            }
            catch
            {
                // Restore from backup on any error
                if (!string.IsNullOrEmpty(backupPath))
                {
                    await _packManager.RestorePackFromBackupAsync(pack.Id, backupPath);
                }
                throw;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update pack {PackId}", pack.Id);
            return false;
        }
    }

    public async Task<long> GetAvailableStorageSpaceAsync()
    {
        try
        {
            return await Task.Run(() =>
            {
                var driveInfo = new DriveInfo(Path.GetPathRoot(_dataDirectory) ?? "/");
                return driveInfo.AvailableFreeSpace;
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get available storage space");
            return 0;
        }
    }

    public async Task<bool> HasSufficientStorageAsync(long requiredBytes)
    {
        try
        {
            var available = await GetAvailableStorageSpaceAsync();
            // Require at least 100MB buffer
            const long buffer = 100 * 1024 * 1024;
            return available > (requiredBytes + buffer);
        }
        catch
        {
            return false;
        }
    }

    private bool IsNewerVersion(string newVersion, string currentVersion)
    {
        try
        {
            _logger.LogDebug("Comparing versions: new='{NewVersion}' vs current='{CurrentVersion}'", newVersion, currentVersion);
            
            var newParts = newVersion.Split('.').Select(int.Parse).ToArray();
            var currentParts = currentVersion.Split('.').Select(int.Parse).ToArray();

            for (int i = 0; i < Math.Max(newParts.Length, currentParts.Length); i++)
            {
                var newPart = i < newParts.Length ? newParts[i] : 0;
                var currentPart = i < currentParts.Length ? currentParts[i] : 0;

                if (newPart > currentPart) 
                {
                    _logger.LogDebug("Version comparison result: {NewVersion} > {CurrentVersion} = true", newVersion, currentVersion);
                    return true;
                }
                if (newPart < currentPart) 
                {
                    _logger.LogDebug("Version comparison result: {NewVersion} < {CurrentVersion} = false", newVersion, currentVersion);
                    return false;
                }
            }

            _logger.LogDebug("Version comparison result: {NewVersion} == {CurrentVersion} = false", newVersion, currentVersion);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse versions, falling back to string comparison: {NewVersion} vs {CurrentVersion}", newVersion, currentVersion);
            var result = string.Compare(newVersion, currentVersion, StringComparison.Ordinal) > 0;
            _logger.LogDebug("String comparison result: {NewVersion} vs {CurrentVersion} = {Result}", newVersion, currentVersion, result);
            return result;
        }
    }

    public void ClearCache()
    {
        _cachedIndex = null;
        _logger.LogDebug("Pack index cache cleared");
    }
}