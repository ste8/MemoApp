using System.Text.Json;
using MemoApp.Core.MajorSystem;

namespace MemoApp.Core.Tests.MajorSystem;

public class MajorNumberJsonConverterTests
{
    [Fact]
    public void JsonConverter_SerializeAndDeserialize_RegularNumber_PreservesData()
    {
        // Arrange
        var originalNumber = MajorNumber.FromValue(23);

        // Act
        var json = JsonSerializer.Serialize(originalNumber);
        var deserializedNumber = JsonSerializer.Deserialize<MajorNumber>(json);

        // Assert
        Assert.Equal(originalNumber.Value, deserializedNumber.Value);
        Assert.Equal(originalNumber.IsZeroPrefixed, deserializedNumber.IsZeroPrefixed);
        Assert.Equal(originalNumber.Display, deserializedNumber.Display);
    }

    [Fact]
    public void JsonConverter_SerializeAndDeserialize_ZeroPrefixedNumber_PreservesData()
    {
        // Arrange
        var originalNumber = MajorNumber.FromZeroPrefixed(5);

        // Act
        var json = JsonSerializer.Serialize(originalNumber);
        var deserializedNumber = JsonSerializer.Deserialize<MajorNumber>(json);

        // Assert
        Assert.Equal(originalNumber.Value, deserializedNumber.Value);
        Assert.Equal(originalNumber.IsZeroPrefixed, deserializedNumber.IsZeroPrefixed);
        Assert.Equal(originalNumber.Display, deserializedNumber.Display);
        Assert.Equal("05", deserializedNumber.Display); // Specific test for zero-prefixed display
    }

    [Fact]
    public void JsonConverter_NumberPerformance_SerializationPreservesNumberData()
    {
        // Arrange
        var number = MajorNumber.FromValue(42);
        var startTime = DateTime.Now.AddSeconds(-10);
        var endTime = DateTime.Now;
        var performance = new NumberPerformance(number, startTime, endTime);

        // Act
        var json = JsonSerializer.Serialize(performance);
        var deserializedPerformance = JsonSerializer.Deserialize<NumberPerformance>(json);

        // Assert
        Assert.Equal(performance.Number.Value, deserializedPerformance.Number.Value);
        Assert.Equal(performance.Number.IsZeroPrefixed, deserializedPerformance.Number.IsZeroPrefixed);
        Assert.Equal(performance.Number.Display, deserializedPerformance.Number.Display);
        Assert.Equal("42", deserializedPerformance.Number.Display); // Ensure it's not "00"
    }

    [Theory]
    [InlineData(0, false, "0")]
    [InlineData(1, false, "1")]
    [InlineData(9, false, "9")]
    [InlineData(10, false, "10")]
    [InlineData(23, false, "23")]
    [InlineData(99, false, "99")]
    [InlineData(0, true, "00")]
    [InlineData(1, true, "01")]
    [InlineData(5, true, "05")]
    [InlineData(9, true, "09")]
    public void JsonConverter_VariousNumbers_PreservesDisplayFormat(int value, bool isZeroPrefixed, string expectedDisplay)
    {
        // Arrange
        var originalNumber = new MajorNumber(value, isZeroPrefixed);

        // Act
        var json = JsonSerializer.Serialize(originalNumber);
        var deserializedNumber = JsonSerializer.Deserialize<MajorNumber>(json);

        // Assert
        Assert.Equal(expectedDisplay, deserializedNumber.Display);
        Assert.Equal(originalNumber.Value, deserializedNumber.Value);
        Assert.Equal(originalNumber.IsZeroPrefixed, deserializedNumber.IsZeroPrefixed);
    }
}