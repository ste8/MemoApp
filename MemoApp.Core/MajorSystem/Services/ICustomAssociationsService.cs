using MemoApp.Core.MajorSystem.Models;

namespace MemoApp.Core.MajorSystem.Services;

/// <summary>
/// Service for managing custom user-created token associations
/// </summary>
public interface ICustomAssociationsService
{
    /// <summary>
    /// Creates a new custom association for the specified token
    /// </summary>
    /// <param name="packId">The pack ID</param>
    /// <param name="categoryKey">The category key</param>
    /// <param name="token">The token</param>
    /// <param name="word">The association word</param>
    /// <param name="imageStream">The image data stream</param>
    /// <param name="setAsDefault">Whether to set this as the default association</param>
    /// <returns>The created custom association</returns>
    Task<TokenAssociation> CreateCustomAssociationAsync(
        string packId, 
        string categoryKey, 
        string token, 
        string word, 
        Stream imageStream, 
        bool setAsDefault = true);
    
    /// <summary>
    /// Gets all custom associations for the specified category
    /// </summary>
    /// <param name="packId">The pack ID</param>
    /// <param name="categoryKey">The category key</param>
    /// <returns>List of custom associations</returns>
    Task<List<TokenAssociation>> GetCustomAssociationsAsync(string packId, string categoryKey);
    
    /// <summary>
    /// Gets custom associations for a specific token
    /// </summary>
    /// <param name="packId">The pack ID</param>
    /// <param name="categoryKey">The category key</param>
    /// <param name="token">The token</param>
    /// <returns>List of custom associations for the token</returns>
    Task<List<TokenAssociation>> GetCustomAssociationsForTokenAsync(string packId, string categoryKey, string token);
    
    /// <summary>
    /// Deletes a custom association
    /// </summary>
    /// <param name="association">The association to delete</param>
    /// <returns>True if deleted successfully</returns>
    Task<bool> DeleteCustomAssociationAsync(TokenAssociation association);
    
    /// <summary>
    /// Checks if the custom directories exist and creates them if needed
    /// </summary>
    /// <param name="packId">The pack ID</param>
    /// <param name="categoryKey">The category key</param>
    /// <returns>True if directories are ready</returns>
    Task<bool> EnsureCustomDirectoriesAsync(string packId, string categoryKey);
    
    /// <summary>
    /// Gets the custom images directory path for a pack and category
    /// </summary>
    /// <param name="packId">The pack ID</param>
    /// <param name="categoryKey">The category key</param>
    /// <returns>The custom images directory path</returns>
    Task<string> GetCustomImagesPathAsync(string packId, string categoryKey);
    
    /// <summary>
    /// Gets the custom thumbnails directory path for a pack and category
    /// </summary>
    /// <param name="packId">The pack ID</param>
    /// <param name="categoryKey">The category key</param>
    /// <returns>The custom thumbnails directory path</returns>
    Task<string> GetCustomThumbnailsPathAsync(string packId, string categoryKey);
}