namespace MemoApp.Core.MajorSystem;

public static class NumberSequence
{
    public static IEnumerable<MajorNumber> GenerateSequence(string rangeStart, string rangeEnd)
    {
        var startNumber = ParseRangeValue(rangeStart);
        var endNumber = ParseRangeValue(rangeEnd);
        
        return GenerateSequence(startNumber, endNumber);
    }

    public static IEnumerable<MajorNumber> GenerateSequence(MajorNumber start, MajorNumber end)
    {
        var allNumbers = GetAllNumbersInOrder().ToList();
        
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

    private static IEnumerable<MajorNumber> GetAllNumbersInOrder()
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
}