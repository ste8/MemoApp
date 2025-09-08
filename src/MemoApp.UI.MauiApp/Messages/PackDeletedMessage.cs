using MemoApp.Core.Packs;

namespace MemoApp.UI.MauiApp.Messages;

/// <summary>
/// Message sent when a pack is successfully deleted
/// </summary>
/// <param name="PackId">The ID of the deleted pack</param>
/// <param name="PackName">The display name of the deleted pack</param>
/// <param name="InternationalName">The international name of the deleted pack</param>
/// <param name="LanguageCode">The language code of the deleted pack</param>
/// <param name="Version">The version of the deleted pack</param>
/// <param name="Timestamp">When the deletion completed</param>
/// <param name="WasLastPack">Whether this was the last available pack</param>
public record PackDeletedMessage(
    string PackId,
    string PackName,
    string InternationalName,
    string LanguageCode,
    string Version,
    DateTime Timestamp,
    bool WasLastPack)
{
    /// <summary>
    /// Creates a PackDeletedMessage from a Pack object
    /// </summary>
    /// <param name="pack">The deleted pack</param>
    /// <param name="wasLastPack">Whether this was the last available pack</param>
    /// <returns>A new PackDeletedMessage</returns>
    public static PackDeletedMessage FromPack(Pack pack, bool wasLastPack)
    {
        return new PackDeletedMessage(
            pack.Id,
            pack.GetDisplayName(useNativeName: false), // Use international name for consistency
            pack.InternationalName,
            pack.LanguageCode,
            pack.Version,
            DateTime.UtcNow,
            wasLastPack);
    }
}