using MemoApp.Core.MajorSystem.Models;

namespace MemoApp.Core.MajorSystem.Services;

/// <summary>
/// Service for managing user's custom default token associations
/// </summary>
public interface ITokenDefaultsService
{
    /// <summary>
    /// Event fired when a token default is changed
    /// </summary>
    event EventHandler<TokenDefaultChangedEventArgs>? TokenDefaultChanged;
    
    /// <summary>
    /// Gets the user's custom defaults for the current pack
    /// </summary>
    Task<UserDefaultPreferences> GetUserDefaultsAsync(string packId);
    
    /// <summary>
    /// Saves the user's custom defaults
    /// </summary>
    Task SaveUserDefaultsAsync(UserDefaultPreferences preferences);
    
    /// <summary>
    /// Sets a custom default for a specific token
    /// </summary>
    Task SetTokenDefaultAsync(string packId, string category, string token, string filename);
    
    /// <summary>
    /// Gets the effective default for a token (user custom or pack default)
    /// </summary>
    Task<string?> GetEffectiveDefaultAsync(string packId, string category, string token, Dictionary<string, Dictionary<string, string>>? packDefaults);
    
    /// <summary>
    /// Clears all custom defaults for a category
    /// </summary>
    Task ClearCategoryDefaultsAsync(string packId, string category);
    
    /// <summary>
    /// Clears all custom defaults for a pack
    /// </summary>
    Task ClearPackDefaultsAsync(string packId);
}