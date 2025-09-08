using MemoApp.Core.Packs;

namespace MemoApp.UI.MauiApp.Messages;

/// <summary>
/// Message sent when a pack is successfully downloaded (new download)
/// </summary>
/// <param name="PackId">The ID of the downloaded pack</param>
/// <param name="PackName">The display name of the downloaded pack</param>
/// <param name="InternationalName">The international name of the downloaded pack</param>
/// <param name="LanguageCode">The language code of the downloaded pack</param>
/// <param name="Version">The version of the downloaded pack</param>
/// <param name="FileSizeMB">The file size of the pack in MB</param>
/// <param name="DownloadDurationSeconds">How long the download took in seconds</param>
/// <param name="Timestamp">When the download completed</param>
public record PackDownloadedMessage(
    string PackId,
    string PackName,
    string InternationalName,
    string LanguageCode,
    string Version,
    double FileSizeMB,
    double DownloadDurationSeconds,
    DateTime Timestamp)
{
    /// <summary>
    /// Creates a PackDownloadedMessage from a Pack object
    /// </summary>
    /// <param name="pack">The downloaded pack</param>
    /// <param name="downloadDurationSeconds">How long the download took</param>
    /// <returns>A new PackDownloadedMessage</returns>
    public static PackDownloadedMessage FromPack(Pack pack, double downloadDurationSeconds)
    {
        return new PackDownloadedMessage(
            pack.Id,
            pack.GetDisplayName(useNativeName: false), // Use international name for consistency
            pack.InternationalName,
            pack.LanguageCode,
            pack.Version,
            Math.Round(pack.FileSize / (1024.0 * 1024.0), 2),
            downloadDurationSeconds,
            DateTime.UtcNow);
    }
}