using MemoApp.Core.Packs;

namespace MemoApp.UI.MauiApp.Messages;

/// <summary>
/// Message sent when a pack is successfully updated (existing pack upgraded)
/// </summary>
/// <param name="PackId">The ID of the updated pack</param>
/// <param name="PackName">The display name of the updated pack</param>
/// <param name="InternationalName">The international name of the updated pack</param>
/// <param name="LanguageCode">The language code of the updated pack</param>
/// <param name="PreviousVersion">The previous version before update</param>
/// <param name="NewVersion">The new version after update</param>
/// <param name="FileSizeMB">The file size of the pack in MB</param>
/// <param name="UpdateDurationSeconds">How long the update took in seconds</param>
/// <param name="Timestamp">When the update completed</param>
public record PackUpdatedMessage(
    string PackId,
    string PackName,
    string InternationalName,
    string LanguageCode,
    string PreviousVersion,
    string NewVersion,
    double FileSizeMB,
    double UpdateDurationSeconds,
    DateTime Timestamp)
{
    /// <summary>
    /// Creates a PackUpdatedMessage from a Pack object
    /// </summary>
    /// <param name="pack">The updated pack</param>
    /// <param name="previousVersion">The version before the update</param>
    /// <param name="updateDurationSeconds">How long the update took</param>
    /// <returns>A new PackUpdatedMessage</returns>
    public static PackUpdatedMessage FromPack(Pack pack, string previousVersion, double updateDurationSeconds)
    {
        return new PackUpdatedMessage(
            pack.Id,
            pack.GetDisplayName(useNativeName: false), // Use international name for consistency
            pack.InternationalName,
            pack.LanguageCode,
            previousVersion,
            pack.Version,
            Math.Round(pack.FileSize / (1024.0 * 1024.0), 2),
            updateDurationSeconds,
            DateTime.UtcNow);
    }
}