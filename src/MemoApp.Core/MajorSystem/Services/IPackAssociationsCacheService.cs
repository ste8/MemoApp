using System.Collections.Concurrent;

namespace MemoApp.Core.MajorSystem.Services;

/// <summary>
/// Service for caching pack associations data to improve performance
/// </summary>
public interface IPackAssociationsCacheService
{
    /// <summary>
    /// Gets cached pack defaults or loads them if not cached
    /// </summary>
    /// <param name="packId">The pack ID</param>
    /// <param name="loadFunc">Function to load the defaults if not cached</param>
    /// <returns>The pack defaults dictionary</returns>
    Task<Dictionary<string, Dictionary<string, string>>> GetCachedPackDefaultsAsync(
        string packId, 
        Func<Task<Dictionary<string, Dictionary<string, string>>>> loadFunc);
    
    /// <summary>
    /// Gets cached category files or loads them if not cached
    /// </summary>
    /// <param name="packId">The pack ID</param>
    /// <param name="category">The category key</param>
    /// <param name="loadFunc">Function to load the files if not cached</param>
    /// <returns>Array of file paths</returns>
    Task<string[]> GetCachedCategoryFilesAsync(
        string packId,
        string category,
        Func<Task<string[]>> loadFunc);
    
    /// <summary>
    /// Invalidates cache for a specific pack
    /// </summary>
    /// <param name="packId">The pack ID to invalidate</param>
    void InvalidatePackCache(string packId);
    
    /// <summary>
    /// Clears all cached data
    /// </summary>
    void ClearCache();
}