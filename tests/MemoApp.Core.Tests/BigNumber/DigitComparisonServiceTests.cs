using MemoApp.Core.BigNumber;

namespace MemoApp.Core.Tests.BigNumber;

public class DigitComparisonServiceTests
{
    private readonly DigitComparisonService _service;

    public DigitComparisonServiceTests()
    {
        _service = new DigitComparisonService();
    }

    [Theory]
    [InlineData("123", "123", 3, 0, 0, 0)] // Perfect match
    [InlineData("123", "12", 2, 0, 0, 1)]  // User input shorter
    [InlineData("12", "123", 2, 0, 1, 0)]  // User input longer
    [InlineData("123", "321", 1, 2, 0, 0)] // All digits exist but positions wrong
    [InlineData("123", "456", 0, 0, 3, 0)] // No matching digits
    [InlineData("112", "121", 1, 2, 0, 0)] // Duplicates with some correct - fixed expectation
    [InlineData("123", "132", 1, 2, 0, 0)] // Partial position match
    [InlineData("1234", "124", 2, 1, 0, 1)] // Mixed scenario - fixed expectation
    public void CompareNumbers_ShouldReturnCorrectCounts(
        string original, 
        string userInput, 
        int expectedCorrect, 
        int expectedWrongPosition, 
        int expectedIncorrect, 
        int expectedMissing)
    {
        // Act
        var result = _service.CompareNumbers(original, userInput);

        // Assert
        Assert.Equal(expectedCorrect, result.CorrectCount);
        Assert.Equal(expectedWrongPosition, result.WrongPositionCount);
        Assert.Equal(expectedIncorrect, result.IncorrectCount);
        Assert.Equal(expectedMissing, result.MissingCount);
        Assert.Equal(original, result.OriginalNumber);
        Assert.Equal(userInput, result.UserInput);
    }

    [Fact]
    public void CompareNumbers_PerfectMatch_AllDigitsCorrect()
    {
        // Arrange
        var original = "12345";
        var userInput = "12345";

        // Act
        var result = _service.CompareNumbers(original, userInput);

        // Assert
        Assert.Equal(5, result.DigitResults.Count);
        Assert.All(result.DigitResults, dr => Assert.Equal(DigitMatchType.Correct, dr.MatchType));
        Assert.Equal(100.0, result.AccuracyPercentage);
    }

    [Fact]
    public void CompareNumbers_CompletelyWrong_AllDigitsIncorrect()
    {
        // Arrange
        var original = "123";
        var userInput = "456";

        // Act
        var result = _service.CompareNumbers(original, userInput);

        // Assert
        Assert.Equal(3, result.DigitResults.Count);
        Assert.All(result.DigitResults, dr => Assert.Equal(DigitMatchType.Incorrect, dr.MatchType));
        Assert.Equal(0.0, result.AccuracyPercentage);
    }

    [Fact]
    public void CompareNumbers_UserInputShorter_ShowsMissingDigits()
    {
        // Arrange
        var original = "12345";
        var userInput = "123";

        // Act
        var result = _service.CompareNumbers(original, userInput);

        // Assert
        Assert.Equal(5, result.DigitResults.Count);
        
        // First 3 should be correct
        for (int i = 0; i < 3; i++)
        {
            Assert.Equal(DigitMatchType.Correct, result.DigitResults[i].MatchType);
            Assert.NotNull(result.DigitResults[i].UserDigit);
        }
        
        // Last 2 should be missing
        for (int i = 3; i < 5; i++)
        {
            Assert.Equal(DigitMatchType.Missing, result.DigitResults[i].MatchType);
            Assert.Null(result.DigitResults[i].UserDigit);
        }
    }

    [Fact]
    public void CompareNumbers_AllWrongPositions_CorrectCounts()
    {
        // Arrange
        var original = "123";
        var userInput = "321";

        // Act
        var result = _service.CompareNumbers(original, userInput);

        // Assert
        Assert.Equal(3, result.DigitResults.Count);
        Assert.Equal(1, result.CorrectCount); // Middle digit '2' is in correct position
        Assert.Equal(2, result.WrongPositionCount); // '1' and '3' are in wrong positions
        Assert.Equal(0, result.IncorrectCount);
        Assert.Equal(0, result.MissingCount);
    }

    [Fact]
    public void CompareNumbers_WithDuplicates_HandlesCorrectly()
    {
        // Arrange
        var original = "1123";
        var userInput = "3211";

        // Act
        var result = _service.CompareNumbers(original, userInput);

        // Assert
        Assert.Equal(4, result.DigitResults.Count);
        
        // Position 0: user has '3', original has '1' -> '3' exists at position 2, so wrong position
        Assert.Equal(DigitMatchType.WrongPosition, result.DigitResults[0].MatchType);
        
        // Position 1: user has '2', original has '1' -> '2' exists at position 3, so wrong position  
        Assert.Equal(DigitMatchType.WrongPosition, result.DigitResults[1].MatchType);
        
        // Position 2: user has '1', original has '2' -> '1' exists at positions 0,1, so wrong position
        Assert.Equal(DigitMatchType.WrongPosition, result.DigitResults[2].MatchType);
        
        // Position 3: user has '1', original has '3' -> '1' exists at positions 0,1, so wrong position
        Assert.Equal(DigitMatchType.WrongPosition, result.DigitResults[3].MatchType);
    }

    [Fact]
    public void CompareNumbers_WithSpacesAndFormatting_ExtractsDigitsOnly()
    {
        // Arrange
        var original = "1 2 3 4 5";
        var userInput = "12-345";

        // Act
        var result = _service.CompareNumbers(original, userInput);

        // Assert
        Assert.Equal(5, result.DigitResults.Count);
        Assert.All(result.DigitResults, dr => Assert.Equal(DigitMatchType.Correct, dr.MatchType));
        Assert.Equal(100.0, result.AccuracyPercentage);
    }

    [Theory]
    [InlineData("", "123")]
    [InlineData("123", "")]
    [InlineData("", "")]
    public void CompareNumbers_WithEmptyInputs_HandlesGracefully(string original, string userInput)
    {
        // Act
        var result = _service.CompareNumbers(original, userInput);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(original, result.OriginalNumber);
        Assert.Equal(userInput, result.UserInput);
    }

    [Fact]
    public void CompareNumbers_LargeNumbers_PerformsEfficiently()
    {
        // Arrange
        var original = string.Join("", Enumerable.Range(0, 500).Select(i => (i % 10).ToString()));
        var userInput = string.Join("", Enumerable.Range(0, 500).Select(i => ((i + 1) % 10).ToString()));

        // Act
        var result = _service.CompareNumbers(original, userInput);

        // Assert
        Assert.Equal(500, result.DigitResults.Count);
        Assert.True(result.AccuracyPercentage >= 0 && result.AccuracyPercentage <= 100);
        
        // Verify all positions have comparison results
        Assert.All(result.DigitResults, dr => Assert.True(Enum.IsDefined(typeof(DigitMatchType), dr.MatchType)));
    }

    [Fact]
    public void CompareNumbers_DigitResultsHaveCorrectPositions()
    {
        // Arrange
        var original = "12345";
        var userInput = "54321";

        // Act
        var result = _service.CompareNumbers(original, userInput);

        // Assert
        Assert.Equal(5, result.DigitResults.Count);
        
        for (int i = 0; i < result.DigitResults.Count; i++)
        {
            Assert.Equal(i, result.DigitResults[i].Position);
            Assert.Equal(original[i], result.DigitResults[i].OriginalDigit);
            
            if (i < userInput.Length)
            {
                Assert.Equal(userInput[i], result.DigitResults[i].UserDigit);
            }
        }
    }
}