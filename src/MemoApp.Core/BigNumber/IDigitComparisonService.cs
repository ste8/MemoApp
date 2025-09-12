namespace MemoApp.Core.BigNumber;

/// <summary>
/// Service for performing Wordle-style digit comparison between original and user numbers
/// </summary>
public interface IDigitComparisonService
{
    /// <summary>
    /// Compares the user's input against the original number using Wordle-style logic
    /// </summary>
    /// <param name="originalNumber">The original number that was memorized</param>
    /// <param name="userInput">The user's input answer</param>
    /// <returns>Detailed comparison results for each digit position</returns>
    NumberComparisonResult CompareNumbers(string originalNumber, string userInput);
}