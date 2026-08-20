using MemoApp.Core.MajorSystem;
using MemoApp.Localization.Services;

namespace MemoApp.UI.MauiApp.Utilities;

/// <summary>
/// Adapts the Core number sequence API to the user's current display format.
///
/// This class used to carry its own copies of the sequence generator and the range parser,
/// one of them byte-identical to the Core version. Both are gone, along with a third,
/// never-called generator; what remains delegates to <see cref="NumberSequence"/> so there
/// is a single implementation to change.
/// </summary>
public static class NumberFormatHelper
{
    /// <summary>
    /// Formats a MajorNumber for display using the current number format setting.
    /// </summary>
    public static string FormatNumber(MajorNumber number, ILocalizationService localizationService)
    {
        return localizationService.CurrentNumberFormat switch
        {
            // Natural keeps the two representations distinct: "07" versus "7".
            NumberFormat.Natural => number.IsZeroPrefixed ? $"0{number.Value}" : number.Value.ToString(),

            // Padded renders everything as two digits. Padded sequences no longer contain
            // natural values below 10, so this cannot collide with a zero-prefixed token.
            NumberFormat.Padded => number.Value.ToString("00"),

            _ => number.Display
        };
    }

    /// <summary>
    /// All tokens for the current display format, in presentation order.
    /// </summary>
    public static IEnumerable<MajorNumber> GetAllNumbersInOrder(ILocalizationService localizationService)
        => NumberSequence.GetAllNumbersInOrder(localizationService.CurrentNumberFormat);
}
