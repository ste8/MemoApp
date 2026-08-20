namespace MemoApp.Core.MajorSystem;

/// <summary>
/// Single source of truth for Major System number sequences.
///
/// Sequences are format-aware. Previously the ordering was fixed at
/// "00-09 zero-prefixed, then 0-99 natural" (110 entries) regardless of the user's
/// display format. Under <see cref="NumberFormat.Padded"/> every token renders as two
/// digits, so the zero-prefixed 07 and the natural 7 both displayed as "07" - the
/// "Complete Major System" preset showed 110 numbers with ten of them repeated.
/// Each format now yields exactly 100 tokens, all of which render distinctly.
/// </summary>
public static class NumberSequence
{
    /// <summary>
    /// The presentation sequence: what a training session shows, in order.
    /// Every entry renders distinctly under the given format's display rules.
    /// For the authoring/browsing vocabulary see <see cref="GetAllTokens"/>.
    /// Padded: 00-09 zero-prefixed, then 10-99 natural.
    /// Natural: 0-99 natural (zero-prefixed tokens are unreachable in this format).
    /// </summary>
    public static IEnumerable<MajorNumber> GetAllNumbersInOrder(NumberFormat format)
    {
        if (format == NumberFormat.Padded)
        {
            for (int i = 0; i <= 9; i++)
            {
                yield return MajorNumber.FromZeroPrefixed(i);
            }

            for (int i = 10; i <= 99; i++)
            {
                yield return MajorNumber.FromValue(i);
            }
        }
        else
        {
            for (int i = 0; i <= 99; i++)
            {
                yield return MajorNumber.FromValue(i);
            }
        }
    }

    /// <summary>
    /// The full addressable token vocabulary for authoring and browsing associations.
    ///
    /// This is deliberately NOT the same as <see cref="GetAllNumbersInOrder"/>. Association
    /// tokens are keyed by <see cref="MajorNumber.Display"/>, under which "07" and "7" are
    /// distinct strings and both are legitimate Major System mnemonics (07 = S-K, 7 = K).
    /// The presentation sequence drops the natural single digits under Padded only because
    /// that format renders them identically to the zero-prefixed forms on screen.
    /// </summary>
    public static IEnumerable<MajorNumber> GetAllTokens(NumberFormat format)
    {
        if (format == NumberFormat.Padded)
        {
            for (int i = 0; i <= 9; i++)
            {
                yield return MajorNumber.FromZeroPrefixed(i);
            }
        }

        for (int i = 0; i <= 99; i++)
        {
            yield return MajorNumber.FromValue(i);
        }
    }

    public static IEnumerable<MajorNumber> GenerateSequence(string rangeStart, string rangeEnd, NumberFormat format)
    {
        var startNumber = ParseRangeValue(rangeStart, format);
        var endNumber = ParseRangeValue(rangeEnd, format);

        return GenerateSequence(startNumber, endNumber, format);
    }

    public static IEnumerable<MajorNumber> GenerateSequence(MajorNumber start, MajorNumber end, NumberFormat format)
    {
        var allNumbers = GetAllNumbersInOrder(format).ToList();

        var startIndex = allNumbers.FindIndex(n => n.Equals(start));
        var endIndex = allNumbers.FindIndex(n => n.Equals(end));

        if (startIndex == -1)
            throw new ArgumentException($"Start number {start} not found in sequence", nameof(start));
        if (endIndex == -1)
            throw new ArgumentException($"End number {end} not found in sequence", nameof(end));
        if (startIndex > endIndex)
            throw new ArgumentException("Start number must come before end number in sequence");

        return allNumbers.Skip(startIndex).Take(endIndex - startIndex + 1);
    }

    /// <summary>
    /// Parses a range endpoint and normalises it to the representation this format
    /// actually contains, so that "0" and "00" select the same token under Padded
    /// and neither can address a token outside the sequence.
    /// </summary>
    public static MajorNumber ParseRangeValue(string value, NumberFormat format)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Range value cannot be null or empty", nameof(value));

        value = value.Trim();

        if (!int.TryParse(value, out int numericValue))
            throw new ArgumentException($"Invalid numeric value: {value}", nameof(value));

        if (numericValue < 0 || numericValue > 99)
            throw new ArgumentException($"Value must be between 0 and 99: {numericValue}", nameof(value));

        // Padded represents everything below 10 with the zero-prefixed token;
        // Natural has no zero-prefixed tokens at all.
        bool isZeroPrefixed = format == NumberFormat.Padded && numericValue <= 9;

        return new MajorNumber(numericValue, isZeroPrefixed);
    }
}
