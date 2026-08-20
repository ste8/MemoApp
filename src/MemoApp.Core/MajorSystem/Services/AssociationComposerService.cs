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
    /// <param name="numberFormat">The current number format setting for proper token normalization</param>
    /// <returns>A list of all associations (pack + custom) with proper default status applied</returns>
    public async Task<List<TokenAssociation>> GetCompleteTokenAssociationsAsync(string categoryKey, string token, NumberFormat numberFormat)
    {
        try
        {
            _logger.LogDebug("Loading complete associations for token {Token} in category {Category} with format {NumberFormat}",
                token, categoryKey, numberFormat);

            // Only normalize token when using Padded format (00-99)
            // In Padded format, "00"-"09" should use associations from "0"-"9"
            // In Natural format, tokens are already in the correct format
            var normalizedToken = numberFormat == NumberFormat.Natural
                ? NormalizeTokenForPackLookup(token)
                : token;

            // Get pack associations using normalized token (preserves original token in returned associations)
            var associations = await _packAssociationsService.GetTokenAssociationsAsync(categoryKey, normalizedToken);

            // Ensure associations preserve the original token for display
            foreach (var association in associations)
            {
                association.Token = token; // Preserve original padded format for display
            }

            // Also get custom associations for this token to ensure we have the complete picture
            // Custom associations use the original token format as users might have created them with specific formatting
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

    /// <summary>
    /// Normalizes a token for pack association lookup by converting padded numbers (00-09)
    /// to their natural equivalents (0-9) to match how pack associations are stored.
    /// Display formatting is preserved in the UI layer, but pack lookups need natural tokens.
    /// </summary>
    /// <param name="token">The token to normalize (e.g., "00", "01", "10")</param>
    /// <returns>The normalized token for pack lookup (e.g., "0", "1", "10")</returns>
    private static string NormalizeTokenForPackLookup(string token)
    {
        // Pack associations are stored with natural format: single digits as "0", "1", "2"... not "00", "01", "02"
        // Convert zero-padded single digits (00-09) to natural format (0-9) for pack lookup
        if (token.Length == 2 && token.StartsWith("0") && char.IsDigit(token[1]))
        {
            return token[1].ToString();
        }

        // For all other tokens (single digits, 10+), return as-is
        return token;
    }
    public TokenAssociation? ResolveDefault(IReadOnlyList<TokenAssociation> associations)
        => AssociationDefaults.Resolve(associations);
}
