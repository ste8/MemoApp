using MemoApp.Core.MajorSystem.Models;
using MemoApp.Core.Packs;

namespace MemoApp.Core.MajorSystem.Services;

/// <summary>
/// Service for loading and managing pack associations
/// </summary>
public interface IPackAssociationsService
{
    /// <summary>
    /// Event raised when the current pack changes
    /// </summary>
    event EventHandler? PackChanged;
    
    /// <summary>
    /// Gets the current pack
    /// </summary>
    Pack? CurrentPack { get; }
    
    /// <summary>
    /// Loads associations for a specific category from the current pack
    /// </summary>
    Task<List<TokenAssociation>> LoadCategoryAssociationsAsync(string categoryKey);
    
    /// <summary>
    /// Loads associations for a specific category with pagination support
    /// </summary>
    Task<List<TokenAssociation>> LoadCategoryAssociationsAsync(string categoryKey, int skip, int take);
    
    /// <summary>
    /// Loads associations for a specific category with pagination and number format support
    /// </summary>
    Task<List<TokenAssociation>> LoadCategoryAssociationsAsync(string categoryKey, int skip, int take, NumberFormat numberFormat);
    
    /// <summary>
    /// Gets all associations for a specific token across all available images
    /// </summary>
    Task<List<TokenAssociation>> GetTokenAssociationsAsync(string categoryKey, string token);
    
    /// <summary>
    /// Loads the default associations from the pack's defaults.json file
    /// </summary>
    Task<Dictionary<string, Dictionary<string, string>>> LoadPackDefaultsAsync();
    
    /// <summary>
    /// Checks if a pack is available
    /// </summary>
    Task<bool> HasPackAvailableAsync();
    
    /// <summary>
    /// Gets the path to the current pack directory asynchronously
    /// </summary>
    Task<string?> GetCurrentPackPathAsync();
    
    /// <summary>
    /// Refreshes the current pack data
    /// </summary>
    Task RefreshPackDataAsync();
}