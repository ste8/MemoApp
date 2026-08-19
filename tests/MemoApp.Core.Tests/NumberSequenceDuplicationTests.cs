using MemoApp.Core.MajorSystem;

namespace MemoApp.Core.Tests;

/// <summary>
/// Characterisation tests for the duplicate-number defect described in
/// docs/specs/CODEBASE_AUDIT_AND_IMPROVEMENT_PLAN.md §1.
///
/// GetAllNumbersInOrder yields 00-09 (zero-prefixed) followed by 0-99 (natural),
/// so any range spanning both blocks contains ten values twice. MajorNumber.Display
/// distinguishes them correctly ("07" vs "7"), but NumberFormatHelper's Padded branch
/// renders both arms with ToString("00"), collapsing them into one visible label.
///
/// The tests below LOCK IN CURRENT BEHAVIOUR so Phase 2 refactoring is safe.
/// The Skipped tests below them assert the DESIRED behaviour; unskip them as part
/// of Phase 2 and delete the corresponding characterisation test.
/// </summary>
public class NumberSequenceDuplicationTests
{
    /// <summary>Replicates NumberFormatHelper.FormatNumber's Padded branch, whose two
    /// ternary arms are identical (NumberFormatHelper.cs:24).</summary>
    private static string RenderPadded(MajorNumber n) => n.Value.ToString("00");

    // ---------------------------------------------------------------
    // Characterisation — current behaviour. These pass today.
    // ---------------------------------------------------------------

    [Fact]
    public void GenerateSequence_CompleteMajorSystemPreset_Yields110ItemsNot100()
    {
        // The shipped "Complete Major System" preset (FlashNumbersSetupViewModel.cs:338).
        var sequence = NumberSequence.GenerateSequence("00", "99").ToList();

        Assert.Equal(110, sequence.Count); // DEFECT: a 00-99 range should be 100.
    }

    [Fact]
    public void GenerateSequence_CompleteMajorSystemPreset_RepeatsValues0To9()
    {
        var sequence = NumberSequence.GenerateSequence("00", "99").ToList();

        var repeated = sequence.GroupBy(n => n.Value)
                               .Where(g => g.Count() > 1)
                               .Select(g => g.Key)
                               .OrderBy(v => v)
                               .ToList();

        Assert.Equal(Enumerable.Range(0, 10), repeated);
    }

    [Fact]
    public void GenerateSequence_CompleteMajorSystemPreset_CollapsesTo100LabelsWhenPadded()
    {
        var sequence = NumberSequence.GenerateSequence("00", "99").ToList();

        var labels = sequence.Select(RenderPadded).ToList();

        // 110 items render as only 100 distinct labels: the user is shown "00".."09" twice.
        Assert.Equal(110, labels.Count);
        Assert.Equal(100, labels.Distinct().Count());
        Assert.Equal(new[] { "00", "01", "02", "03", "04", "05", "06", "07", "08", "09" },
                     labels.Take(10));
        Assert.Equal(new[] { "00", "01", "02", "03", "04", "05", "06", "07", "08", "09" },
                     labels.Skip(10).Take(10));
    }

    [Fact]
    public void GenerateSequence_Preset00To69_AlsoContainsTenDuplicates()
    {
        // Second affected shipped preset.
        var labels = NumberSequence.GenerateSequence("00", "69").Select(RenderPadded).ToList();

        Assert.Equal(80, labels.Count);
        Assert.Equal(70, labels.Distinct().Count());
    }

    [Theory]
    [InlineData("0", "99", 100)]  // natural block only
    [InlineData("00", "09", 10)]  // zero-prefixed block only
    [InlineData("10", "99", 90)]
    public void GenerateSequence_RangesWithinASingleBlock_AreUnaffected(
        string start, string end, int expected)
    {
        var labels = NumberSequence.GenerateSequence(start, end).Select(RenderPadded).ToList();

        Assert.Equal(expected, labels.Count);
        Assert.Equal(expected, labels.Distinct().Count());
    }

    [Fact]
    public void MajorNumber_Display_DistinguishesZeroPrefixedFromNatural()
    {
        // Display itself is correct — the collapse is introduced by the UI-layer
        // Padded formatter, not by the domain type. Guards against "fixing" the wrong layer.
        Assert.Equal("07", MajorNumber.FromZeroPrefixed(7).Display);
        Assert.Equal("7", MajorNumber.FromValue(7).Display);
        Assert.NotEqual(MajorNumber.FromZeroPrefixed(7), MajorNumber.FromValue(7));
    }

    // ---------------------------------------------------------------
    // Desired behaviour — unskip during Phase 2, then delete the
    // corresponding characterisation tests above.
    // ---------------------------------------------------------------

    [Fact(Skip = "Phase 2: format-aware generation not implemented yet. See plan §6 Phase 2.")]
    public void GenerateSequence_CompleteMajorSystemPreset_ShouldYield100DistinctNumbers()
    {
        var sequence = NumberSequence.GenerateSequence("00", "99").ToList();

        Assert.Equal(100, sequence.Count);
        Assert.Equal(100, sequence.Select(RenderPadded).Distinct().Count());
    }

    [Fact(Skip = "Phase 2: format-aware generation not implemented yet. See plan §6 Phase 2.")]
    public void GenerateSequence_AnyRange_ShouldNeverRepeatARenderedLabel()
    {
        foreach (var (start, end) in new[] { ("00", "99"), ("00", "69"), ("00", "20"), ("08", "12") })
        {
            var labels = NumberSequence.GenerateSequence(start, end).Select(RenderPadded).ToList();

            Assert.Equal(labels.Count, labels.Distinct().Count());
        }
    }
}
