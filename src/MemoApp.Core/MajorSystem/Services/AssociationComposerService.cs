using MemoApp.Core.MajorSystem.Models;
using Microsoft.Extensions.Logging;

namespace MemoApp.Core.MajorSystem.Services;

/// <summary>
/// Service for composing complete association lists by combining pack associations with custom associations
/// and applying proper default resolution. This service encapsulates the logic for loading all associations
/// for a given token from multiple sources.
/// </summary>
public class AssociationComposerService : IAssociationComposerService
{
    private readonly IPackAssociationsService _packAssociationsService;
    private readonly ICustomAssociationsService _customAssociationsService;
    private readonly ITokenDefaultsService _tokenDefaultsService;
    private readonly ILogger<AssociationComposerService> _logger;

    public AssociationComposerService(
        IPackAssociationsService packAssociationsService,
        ICustomAssociationsService customAssociationsService,
        ITokenDefaultsService tokenDefaultsService,
        ILogger<AssociationComposerService> logger)
    {
        _packAssociationsService = packAssociationsService ?? throw new ArgumentNullException(nameof(packAssociationsService));
        _customAssociationsService = customAssociationsService ?? throw new ArgumentNullException(nameof(customAssociationsService));
        _tokenDefaultsService = tokenDefaultsService ?? throw new ArgumentNullException(nameof(tokenDefaultsService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Gets complete associations for a token by combining pack associations with custom associations
    /// and applying proper default status resolution.
    /// </summary>
    /// <param name="categoryKey">The category key (e.g., "numbers")</param>
    /// <param name="token">The token to get associations for (e.g., "01", "2")</param>
    /// <returns>A list of all associations (pack + custom) with proper default status applied</returns>
    public async Task<List<TokenAssociation>> GetCompleteTokenAssociationsAsync(string categoryKey, string token)
    {
        try
        {
            _logger.LogDebug("Loading complete associations for token {Token} in category {Category}", token, categoryKey);

            // Get pack associations for this token
            var associations = await _packAssociationsService.GetTokenAssociationsAsync(categoryKey, token);

            // Also get custom associations for this token to ensure we have the complete picture
            var currentPack = _packAssociationsService.CurrentPack;
            if (currentPack != null)
            {
                var customAssociations = await _customAssociationsService.GetCustomAssociationsForTokenAsync(
                    currentPack.Id, categoryKey, token);

                // Apply default status to custom associations
                if (customAssociations.Count > 0)
                {
                    var currentDefault = await _tokenDefaultsService.GetEffectiveDefaultAsync(
                        currentPack.Id, categoryKey, token, null);

                    foreach (var customAssoc in customAssociations)
                    {
                        // Check if this custom association is the default based on filename
                        var customFilename = Path.GetFileName(customAssoc.ImagePath);
                        customAssoc.IsDefault = !string.IsNullOrEmpty(currentDefault) &&
                            currentDefault.EndsWith(customFilename);
                    }

                    // Add custom associations to the list
                    associations.AddRange(customAssociations);

                    _logger.LogDebug("Added {CustomAssociationCount} custom associations for token {Token}",
                        customAssociations.Count, token);
                }
            }

            _logger.LogDebug("Loaded {TotalAssociationCount} total associations for token {Token} in category {Category}",
                associations.Count, token, categoryKey);

            return associations;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading complete associations for token {Token} in category {Category}", token, categoryKey);
            throw;
        }
    }
}