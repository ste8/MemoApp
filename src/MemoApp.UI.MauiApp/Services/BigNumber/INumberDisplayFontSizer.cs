using MemoApp.Core.BigNumber;

namespace MemoApp.UI.MauiApp.Services.BigNumber;

/// <summary>
/// Chooses the font size for the memorised number.
///
/// Two concerns: the size implied by the user's preference, and an adjustment that avoids
/// splitting the number across lines in a way that breaks digit pairs — reading a Major
/// System number depends on pairs staying visually intact.
/// </summary>
public interface INumberDisplayFontSizer
{
    /// <summary>Size implied by the preference alone, ignoring how the number wraps.</summary>
    double CalculateBaseFontSize(string displayedNumber, FontSizePreference preference);

    /// <summary>
    /// Size that keeps an even number of digits on each line where possible, falling back to
    /// the base size when no such size exists within the device's limits.
    /// </summary>
    double CalculateFontSizeForEvenLineBreaks(string displayedNumber, bool showSeparated, FontSizePreference preference);
}
