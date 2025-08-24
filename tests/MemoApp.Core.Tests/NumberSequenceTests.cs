using MemoApp.Core.MajorSystem;

namespace MemoApp.Core.Tests;

public class NumberSequenceTests
{
    [Fact]
    public void GenerateSequence_ZeroPrefixedRange_ReturnsCorrectSequence()
    {
        var sequence = NumberSequence.GenerateSequence("00", "05").ToList();
        
        Assert.Equal(6, sequence.Count);
        Assert.Equal("00", sequence[0].Display);
        Assert.Equal("01", sequence[1].Display);
        Assert.Equal("05", sequence[5].Display);
        Assert.All(sequence, n => Assert.True(n.IsZeroPrefixed));
    }

    [Fact]
    public void GenerateSequence_RegularRange_ReturnsCorrectSequence()
    {
        var sequence = NumberSequence.GenerateSequence("10", "12").ToList();
        
        Assert.Equal(3, sequence.Count);
        Assert.Equal("10", sequence[0].Display);
        Assert.Equal("11", sequence[1].Display);
        Assert.Equal("12", sequence[2].Display);
        Assert.All(sequence, n => Assert.False(n.IsZeroPrefixed));
    }

    [Fact]
    public void GenerateSequence_CrossingBoundary_ReturnsCorrectSequence()
    {
        var sequence = NumberSequence.GenerateSequence("08", "2").ToList();
        
        // Expected sequence: 08, 09, 0, 1, 2 (total: 5 items, not > 10)
        Assert.Equal(5, sequence.Count);
        Assert.Equal("08", sequence[0].Display);
        Assert.Equal("09", sequence[1].Display);
        Assert.Equal("0", sequence[2].Display);
        Assert.Equal("1", sequence[3].Display);
        Assert.Equal("2", sequence.Last().Display);
    }

    [Fact]
    public void GenerateSequence_InvalidRange_ThrowsException()
    {
        Assert.Throws<ArgumentException>(() => NumberSequence.GenerateSequence("2", "08"));
    }

    [Fact]
    public void GenerateSequence_InvalidInput_ThrowsException()
    {
        Assert.Throws<ArgumentException>(() => NumberSequence.GenerateSequence("", "10"));
        Assert.Throws<ArgumentException>(() => NumberSequence.GenerateSequence("abc", "10"));
        Assert.Throws<ArgumentException>(() => NumberSequence.GenerateSequence("100", "10"));
    }
}