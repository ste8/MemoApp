using MemoApp.Core.MajorSystem;
using MemoApp.Localization.Services;

namespace MemoApp.UI.MauiApp.Utilities;

/// <summary>
/// Helper class for formatting MajorNumber objects based on the current number format setting.
/// </summary>
public static class NumberFormatHelper
{
    /// <summary>
    /// Formats a MajorNumber based on the current number format setting.
    /// </summary>
    /// <param name="number">The MajorNumber to format</param>
    /// <param name="localizationService">The localization service containing format preferences</param>
    /// <returns>Formatted number string</returns>
    public static string FormatNumber(MajorNumber number, ILocalizationService localizationService)
    {
        var numberFormat = localizationService.CurrentNumberFormat;
        
        return numberFormat switch
        {
            NumberFormat.Natural => number.IsZeroPrefixed ? $"0{number.Value}" : number.Value.ToString(),
            NumberFormat.Padded => number.IsZeroPrefixed ? number.Value.ToString("00") : number.Value.ToString("00"),
            _ => number.Display // Fallback to default display
        };
    }

    /// <summary>
    /// Generates number sequences based on the current number format setting.
    /// </summary>
    /// <param name="localizationService">The localization service containing format preferences</param>
    /// <returns>Sequence of MajorNumbers appropriate for the current format</returns>
    public static IEnumerable<MajorNumber> GetAllNumbersInOrder(ILocalizationService localizationService)
    {
        var numberFormat = localizationService.CurrentNumberFormat;
        
        return numberFormat switch
        {
            NumberFormat.Natural => GetNaturalFormatNumbers(),
            NumberFormat.Padded => GetPaddedFormatNumbers(),
            _ => GetPaddedFormatNumbers()
        };
    }

    /// <summary>
    /// Generates a sequence of numbers for a specific range based on the current format setting.
    /// This prevents duplicates by respecting the format when generating the sequence.
    /// </summary>
    /// <param name="rangeStart">Start of range (e.g., "00", "0")</param>
    /// <param name="rangeEnd">End of range (e.g., "09", "9")</param>
    /// <param name="localizationService">The localization service containing format preferences</param>
    /// <returns>Sequence of MajorNumbers for training</returns>
    public static IEnumerable<MajorNumber> GenerateTrainingSequence(string rangeStart, string rangeEnd, ILocalizationService localizationService)
    {
        var numberFormat = localizationService.CurrentNumberFormat;
        
        // Parse the range values
        var startNumber = ParseRangeValue(rangeStart);
        var endNumber = ParseRangeValue(rangeEnd);
        
        // For padded format, if we're in the 00-09 range, only return zero-prefixed numbers
        if (numberFormat == NumberFormat.Padded && 
            startNumber.IsZeroPrefixed && startNumber.Value <= 9 &&
            endNumber.IsZeroPrefixed && endNumber.Value <= 9)
        {
            // Return only zero-prefixed numbers for 00-09 range
            for (int i = startNumber.Value; i <= endNumber.Value; i++)
            {
                yield return MajorNumber.FromZeroPrefixed(i);
            }
        }
        else
        {
            // Use the standard NumberSequence logic for other ranges
            foreach (var number in NumberSequence.GenerateSequence(startNumber, endNumber))
            {
                yield return number;
            }
        }
    }

    private static MajorNumber ParseRangeValue(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Range value cannot be null or empty", nameof(value));
        
        value = value.Trim();
        
        if (!int.TryParse(value, out int numericValue))
            throw new ArgumentException($"Invalid numeric value: {value}", nameof(value));
        
        if (numericValue < 0 || numericValue > 99)
            throw new ArgumentException($"Value must be between 0 and 99: {numericValue}", nameof(value));
        
        // Determine if it's zero-prefixed based on string format
        bool isZeroPrefixed = value.Length == 2 && value.StartsWith('0') && numericValue <= 9;
        
        return new MajorNumber(numericValue, isZeroPrefixed);
    }

    private static IEnumerable<MajorNumber> GetPaddedFormatNumbers()
    {
        // First: 00-09 (zero-prefixed)
        for (int i = 0; i <= 9; i++)
        {
            yield return MajorNumber.FromZeroPrefixed(i);
        }
        
        // Then: 0-99 (regular numbers)
        for (int i = 0; i <= 99; i++)
        {
            yield return MajorNumber.FromValue(i);
        }
    }

    private static IEnumerable<MajorNumber> GetNaturalFormatNumbers()
    {
        // Natural format: 0-99 (no zero-prefixed numbers)
        for (int i = 0; i <= 99; i++)
        {
            yield return MajorNumber.FromValue(i);
        }
    }
}