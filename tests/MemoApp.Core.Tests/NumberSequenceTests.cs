using MemoApp.Core.MajorSystem;

namespace MemoApp.Core.Tests;

/// <summary>
/// NumberSequence exposes two distinct sets of Major System numbers.
///
/// <see cref="NumberSequence.GetAllNumbersInOrder"/> is the presentation sequence: what a
/// training session shows. Every entry in it renders to a different string under the given
/// format, so a session never shows the same label twice.
///
/// <see cref="NumberSequence.GetAllTokens"/> is the addressable vocabulary: what associations
/// are keyed against, via <see cref="MajorNumber.Display"/>. It is a superset under Padded,
/// because 07 and 7 are distinct tokens with distinct Display strings even though that format
/// renders both as "07" on screen.
/// </summary>
public class NumberSequenceTests
{
    // ---------------------------------------------------------------
    // Presentation sequence
    // ---------------------------------------------------------------

    [Fact]
    public void GetAllNumbersInOrder_EitherFormat_Yields100Numbers()
    {
        Assert.Equal(100, NumberSequence.GetAllNumbersInOrder(NumberFormat.Padded).Count());
        Assert.Equal(100, NumberSequence.GetAllNumbersInOrder(NumberFormat.Natural).Count());
    }

    [Fact]
    public void GetAllNumbersInOrder_Padded_UsesZeroPrefixedBelowTenAndNaturalAbove()
    {
        var all = NumberSequence.GetAllNumbersInOrder(NumberFormat.Padded).ToList();

        Assert.All(all.Take(10), n => Assert.True(n.IsZeroPrefixed));
        Assert.All(all.Skip(10), n => Assert.False(n.IsZeroPrefixed));
        Assert.Equal(new[] { "00", "01", "02" }, all.Take(3).Select(n => n.Display));
        Assert.Equal(new[] { "10", "11" }, all.Skip(10).Take(2).Select(n => n.Display));
    }

    [Fact]
    public void GetAllNumbersInOrder_Natural_ContainsNoZeroPrefixedNumbers()
    {
        Assert.All(NumberSequence.GetAllNumbersInOrder(NumberFormat.Natural),
                   n => Assert.False(n.IsZeroPrefixed));
    }

    [Theory]
    [InlineData(NumberFormat.Padded)]
    [InlineData(NumberFormat.Natural)]
    public void GetAllNumbersInOrder_EveryNumberRendersToADifferentLabel(NumberFormat format)
    {
        // Padded prints two digits for everything, so it must not contain both 07 and 7.
        var labels = NumberSequence.GetAllNumbersInOrder(format).Select(n => Render(n, format)).ToList();

        Assert.Equal(labels.Count, labels.Distinct().Count());
    }

    // ---------------------------------------------------------------
    // Addressable token vocabulary
    // ---------------------------------------------------------------

    [Fact]
    public void GetAllTokens_Padded_ContainsBothSpellingsOfTheSingleDigits()
    {
        // 07 and 7 are different mnemonics (S-K versus K) and are keyed separately.
        var tokens = NumberSequence.GetAllTokens(NumberFormat.Padded).Select(n => n.Display).ToList();

        Assert.Equal(110, tokens.Count);
        Assert.Equal(110, tokens.Distinct().Count());
        Assert.Contains("07", tokens);
        Assert.Contains("7", tokens);
    }

    [Fact]
    public void GetAllTokens_Natural_ContainsOnlyTheUnpaddedSpellings()
    {
        var tokens = NumberSequence.GetAllTokens(NumberFormat.Natural).Select(n => n.Display).ToList();

        Assert.Equal(100, tokens.Count);
        Assert.Contains("7", tokens);
        Assert.DoesNotContain("07", tokens);
    }

    [Fact]
    public void GetAllTokens_Padded_IsAStrictSupersetOfThePresentationSequence()
    {
        Assert.Equal(110, NumberSequence.GetAllTokens(NumberFormat.Padded).Count());
        Assert.Equal(100, NumberSequence.GetAllNumbersInOrder(NumberFormat.Padded).Count());
    }

    // ---------------------------------------------------------------
    // Range endpoints
    // ---------------------------------------------------------------

    [Theory]
    [InlineData("0")]
    [InlineData("00")]
    public void ParseRangeValue_Padded_ResolvesBothSpellingsToTheZeroPrefixedNumber(string input)
    {
        Assert.Equal(MajorNumber.FromZeroPrefixed(0),
                     NumberSequence.ParseRangeValue(input, NumberFormat.Padded));
    }

    [Fact]
    public void ParseRangeValue_Natural_NeverResolvesToAZeroPrefixedNumber()
    {
        Assert.Equal(MajorNumber.FromValue(0), NumberSequence.ParseRangeValue("00", NumberFormat.Natural));
        Assert.Equal(MajorNumber.FromValue(7), NumberSequence.ParseRangeValue("07", NumberFormat.Natural));
    }

    // ---------------------------------------------------------------
    // Range generation
    // ---------------------------------------------------------------

    [Fact]
    public void GenerateSequence_ZeroPrefixedRange_ReturnsCorrectSequence()
    {
        var sequence = NumberSequence.GenerateSequence("00", "05", NumberFormat.Padded).ToList();

        Assert.Equal(6, sequence.Count);
        Assert.Equal(new[] { "00", "01", "02", "03", "04", "05" }, sequence.Select(n => n.Display));
        Assert.All(sequence, n => Assert.True(n.IsZeroPrefixed));
    }

    [Fact]
    public void GenerateSequence_RegularRange_ReturnsCorrectSequence()
    {
        var sequence = NumberSequence.GenerateSequence("10", "12", NumberFormat.Padded).ToList();

        Assert.Equal(new[] { "10", "11", "12" }, sequence.Select(n => n.Display));
        Assert.All(sequence, n => Assert.False(n.IsZeroPrefixed));
    }

    [Fact]
    public void GenerateSequence_PaddedSpanningTheTenBoundary_IsContiguous()
    {
        var sequence = NumberSequence.GenerateSequence("08", "11", NumberFormat.Padded).ToList();

        Assert.Equal(new[] { "08", "09", "10", "11" }, sequence.Select(n => n.Display));
    }

    [Fact]
    public void GenerateSequence_NaturalFormat_UsesUnpaddedTokens()
    {
        var sequence = NumberSequence.GenerateSequence("0", "3", NumberFormat.Natural).ToList();

        Assert.Equal(new[] { "0", "1", "2", "3" }, sequence.Select(n => n.Display));
        Assert.All(sequence, n => Assert.False(n.IsZeroPrefixed));
    }

    [Fact]
    public void GenerateSequence_Padded_TreatsBareAndPaddedEndpointsAlike()
    {
        var bare = NumberSequence.GenerateSequence("2", "8", NumberFormat.Padded).ToList();
        var padded = NumberSequence.GenerateSequence("02", "08", NumberFormat.Padded).ToList();

        Assert.Equal(7, bare.Count);
        Assert.Equal(padded, bare);
    }

    [Theory]
    [InlineData("00", "99", 100)]
    [InlineData("00", "69", 70)]
    [InlineData("00", "09", 10)]
    [InlineData("00", "20", 21)]
    [InlineData("08", "12", 5)]
    [InlineData("10", "99", 90)]
    public void GenerateSequence_Padded_ReturnsTheExpectedCountWithNoRepeatedLabel(
        string start, string end, int expected)
    {
        var labels = NumberSequence.GenerateSequence(start, end, NumberFormat.Padded)
                                   .Select(n => Render(n, NumberFormat.Padded))
                                   .ToList();

        Assert.Equal(expected, labels.Count);
        Assert.Equal(expected, labels.Distinct().Count());
    }

    [Theory]
    [InlineData("0", "99", 100)]
    [InlineData("0", "9", 10)]
    [InlineData("10", "99", 90)]
    public void GenerateSequence_Natural_ReturnsTheExpectedCountWithNoRepeatedLabel(
        string start, string end, int expected)
    {
        var labels = NumberSequence.GenerateSequence(start, end, NumberFormat.Natural)
                                   .Select(n => n.Display)
                                   .ToList();

        Assert.Equal(expected, labels.Count);
        Assert.Equal(expected, labels.Distinct().Count());
    }

    [Fact]
    public void GenerateSequence_FullRange_CoversEveryLabelFrom00To99()
    {
        var labels = NumberSequence.GenerateSequence("00", "99", NumberFormat.Padded)
                                   .Select(n => Render(n, NumberFormat.Padded));

        Assert.Equal(Enumerable.Range(0, 100).Select(i => i.ToString("00")), labels);
    }

    // ---------------------------------------------------------------
    // Rejected input
    // ---------------------------------------------------------------

    [Fact]
    public void GenerateSequence_DescendingRange_ThrowsException()
    {
        Assert.Throws<ArgumentException>(() => NumberSequence.GenerateSequence("08", "02", NumberFormat.Padded));
        Assert.Throws<ArgumentException>(() => NumberSequence.GenerateSequence("50", "10", NumberFormat.Padded));
        Assert.Throws<ArgumentException>(() => NumberSequence.GenerateSequence("9", "1", NumberFormat.Natural));
    }

    [Fact]
    public void GenerateSequence_InvalidInput_ThrowsException()
    {
        Assert.Throws<ArgumentException>(() => NumberSequence.GenerateSequence("", "10", NumberFormat.Padded));
        Assert.Throws<ArgumentException>(() => NumberSequence.GenerateSequence("abc", "10", NumberFormat.Padded));
        Assert.Throws<ArgumentException>(() => NumberSequence.GenerateSequence("100", "10", NumberFormat.Padded));
    }

    /// <summary>Mirrors how the UI renders a number under each format.</summary>
    private static string Render(MajorNumber number, NumberFormat format)
        => format == NumberFormat.Padded ? number.Value.ToString("00") : number.Display;
}
