using MemoApp.Core.MajorSystem;

namespace MemoApp.Core.Tests;

public class MajorNumberTests
{
    [Fact]
    public void Constructor_ValidRegularNumber_CreatesCorrectly()
    {
        var number = new MajorNumber(42);
        
        Assert.Equal(42, number.Value);
        Assert.False(number.IsZeroPrefixed);
        Assert.Equal("42", number.Display);
    }

    [Fact]
    public void Constructor_ValidZeroPrefixedNumber_CreatesCorrectly()
    {
        var number = new MajorNumber(5, true);
        
        Assert.Equal(5, number.Value);
        Assert.True(number.IsZeroPrefixed);
        Assert.Equal("05", number.Display);
    }

    [Fact]
    public void Constructor_InvalidValue_ThrowsException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new MajorNumber(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new MajorNumber(100));
    }

    [Fact]
    public void Constructor_ZeroPrefixedValueTooLarge_ThrowsException()
    {
        Assert.Throws<ArgumentException>(() => new MajorNumber(10, true));
    }

    [Fact]
    public void FromValue_CreatesRegularNumber()
    {
        var number = MajorNumber.FromValue(25);
        
        Assert.Equal(25, number.Value);
        Assert.False(number.IsZeroPrefixed);
    }

    [Fact]
    public void FromZeroPrefixed_CreatesZeroPrefixedNumber()
    {
        var number = MajorNumber.FromZeroPrefixed(7);
        
        Assert.Equal(7, number.Value);
        Assert.True(number.IsZeroPrefixed);
    }

    [Fact]
    public void ImplicitConversion_ToInt_ReturnsValue()
    {
        var number = new MajorNumber(15);
        int value = number;
        
        Assert.Equal(15, value);
    }
}