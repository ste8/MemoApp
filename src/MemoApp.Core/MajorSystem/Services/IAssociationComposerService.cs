using MemoApp.Core.MajorSystem.Models;

namespace MemoApp.Core.MajorSystem.Services;

/// <summary>
/// Service for composing complete association lists by combining pack associations with custom associations
/// and applying proper default resolution. This service encapsulates the logic for loading all associations
/// for a given token from multiple sources.
/// </summary>
public interface IAssociationComposerService
{
    /// <summary>
    /// Gets complete associations for a token by combining pack associations with custom associations
    /// and applying proper default status resolution.
    /// </summary>
    /// <param name="categoryKey">The category key (e.g., "numbers")</param>
    /// <param name="token">The token to get associations for (e.g., "01", "2")</param>
    /// <param name="numberFormat">The current number format setting for proper token normalization</param>
    /// <returns>A list of all associations (pack + custom) with proper default status applied</returns>
    Task<List<TokenAssociation>> GetCompleteTokenAssociationsAsync(string categoryKey, string token, NumberFormat numberFormat);
}