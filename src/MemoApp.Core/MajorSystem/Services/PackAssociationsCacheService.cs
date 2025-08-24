using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace MemoApp.Core.MajorSystem.Services;

/// <summary>
/// Implementation of pack associations caching service
/// </summary>
public class PackAssociationsCacheService : IPackAssociationsCacheService
{
    private readonly ConcurrentDictionary<string, CachedPackDefaults> _packsDefaultsCache = new();
    private readonly ConcurrentDictionary<string, CachedCategoryFiles> _categoryFilesCache = new();
    private readonly TimeSpan _cacheExpiration = TimeSpan.FromMinutes(30);
    private readonly ILogger<PackAssociationsCacheService> _logger;

    public PackAssociationsCacheService(ILogger<PackAssociationsCacheService> logger)
    {
        _logger = logger;
    }

    public async Task<Dictionary<string, Dictionary<string, string>>> GetCachedPackDefaultsAsync(
        string packId, 
        Func<Task<Dictionary<string, Dictionary<string, string>>>> loadFunc)
    {
        if (string.IsNullOrEmpty(packId))
            throw new ArgumentException("Pack ID cannot be null or empty", nameof(packId));

        var cached = _packsDefaultsCache.GetOrAdd(packId, _ => new CachedPackDefaults());
        
        if (cached.IsExpired(_cacheExpiration))
        {
            _logger.LogInformation("Pack defaults cache expired for {PackId}, reloading", packId);
            
            try
            {
                var freshData = await loadFunc().ConfigureAwait(false);
                cached.Update(freshData);
                _logger.LogInformation("Cached pack defaults for {PackId} with {CategoryCount} categories", 
                    packId, freshData.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading pack defaults for {PackId}", packId);
                throw;
            }
        }
        else
        {
            _logger.LogDebug("Using cached pack defaults for {PackId}", packId);
        }

        return cached.Data ?? new Dictionary<string, Dictionary<string, string>>();
    }

    public async Task<string[]> GetCachedCategoryFilesAsync(
        string packId,
        string category,
        Func<Task<string[]>> loadFunc)
    {
        if (string.IsNullOrEmpty(packId))
            throw new ArgumentException("Pack ID cannot be null or empty", nameof(packId));
        if (string.IsNullOrEmpty(category))
            throw new ArgumentException("Category cannot be null or empty", nameof(category));

        var cacheKey = $"{packId}|{category}";
        var cached = _categoryFilesCache.GetOrAdd(cacheKey, _ => new CachedCategoryFiles());

        if (cached.IsExpired(_cacheExpiration))
        {
            _logger.LogInformation("Category files cache expired for {PackId}/{Category}, reloading", packId, category);
            
            try
            {
                var freshData = await loadFunc().ConfigureAwait(false);
                cached.Update(freshData);
                _logger.LogInformation("Cached {FileCount} files for {PackId}/{Category}", 
                    freshData.Length, packId, category);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading category files for {PackId}/{Category}", packId, category);
                throw;
            }
        }
        else
        {
            _logger.LogDebug("Using cached category files for {PackId}/{Category}", packId, category);
        }

        return cached.Data ?? Array.Empty<string>();
    }

    public void InvalidatePackCache(string packId)
    {
        if (string.IsNullOrEmpty(packId))
            return;

        _packsDefaultsCache.TryRemove(packId, out _);
        
        // Also remove related category file caches
        var keysToRemove = _categoryFilesCache.Keys.Where(k => k.StartsWith($"{packId}|")).ToList();
        foreach (var key in keysToRemove)
        {
            _categoryFilesCache.TryRemove(key, out _);
        }

        _logger.LogInformation("Invalidated cache for pack {PackId}", packId);
    }

    public void ClearCache()
    {
        _packsDefaultsCache.Clear();
        _categoryFilesCache.Clear();
        _logger.LogInformation("Cleared all pack associations cache");
    }

    private class CachedPackDefaults
    {
        public Dictionary<string, Dictionary<string, string>>? Data { get; private set; }
        public DateTime LastUpdated { get; private set; } = DateTime.MinValue;

        public void Update(Dictionary<string, Dictionary<string, string>> data)
        {
            Data = data;
            LastUpdated = DateTime.UtcNow;
        }

        public bool IsExpired(TimeSpan expiration)
        {
            return Data == null || DateTime.UtcNow - LastUpdated > expiration;
        }
    }

    private class CachedCategoryFiles
    {
        public string[]? Data { get; private set; }
        public DateTime LastUpdated { get; private set; } = DateTime.MinValue;

        public void Update(string[] data)
        {
            Data = data;
            LastUpdated = DateTime.UtcNow;
        }

        public bool IsExpired(TimeSpan expiration)
        {
            return Data == null || DateTime.UtcNow - LastUpdated > expiration;
        }
    }
}