using MemoApp.Core.MajorSystem.Models;

namespace MemoApp.Core.MajorSystem.Services;

/// <summary>
/// Service for managing Major System categories and their tokens
/// </summary>
public interface IMajorSystemCategoryService
{
    /// <summary>
    /// Gets all available categories in the correct display order
    /// </summary>
    List<CategoryTokens> GetAllCategories();
    
    /// <summary>
    /// Gets tokens for a specific category
    /// </summary>
    List<string> GetCategoryTokens(string categoryKey);
    
    /// <summary>
    /// Gets the localized display name for a category
    /// </summary>
    string GetCategoryDisplayName(string categoryKey);
    
    /// <summary>
    /// Discovers dynamic tokens from pack directory (used for symbols category)
    /// </summary>
    Task<List<string>> DiscoverDynamicTokensAsync(string packPath, string category);
}