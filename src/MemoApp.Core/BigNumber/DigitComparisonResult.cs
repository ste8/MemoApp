namespace MemoApp.Core.BigNumber;

/// <summary>
/// Represents the comparison result for a single digit in the Wordle-style comparison
/// </summary>
public enum DigitMatchType
{
    /// <summary>
    /// Correct digit in correct position (GREEN)
    /// </summary>
    Correct,
    
    /// <summary>
    /// Digit exists in original but in wrong position (ORANGE/YELLOW)
    /// </summary>
    WrongPosition,
    
    /// <summary>
    /// Digit does not exist in original number (GRAY)
    /// </summary>
    Incorrect,
    
    /// <summary>
    /// Position is missing in user's answer (RED with dashed border)
    /// </summary>
    Missing
}

/// <summary>
/// Represents the comparison result for a single digit position
/// </summary>
public class DigitComparisonResult
{
    /// <summary>
    /// The digit from the original number at this position
    /// </summary>
    public char OriginalDigit { get; set; }
    
    /// <summary>
    /// The digit from the user's input at this position (null if missing)
    /// </summary>
    public char? UserDigit { get; set; }
    
    /// <summary>
    /// The type of match for this position
    /// </summary>
    public DigitMatchType MatchType { get; set; }
    
    /// <summary>
    /// The zero-based position index
    /// </summary>
    public int Position { get; set; }
}

/// <summary>
/// Represents the complete comparison result between original and user numbers
/// </summary>
public class NumberComparisonResult
{
    /// <summary>
    /// Comparison results for each digit position
    /// </summary>
    public List<DigitComparisonResult> DigitResults { get; set; } = new();
    
    /// <summary>
    /// Count of correct digits (right digit, right position)
    /// </summary>
    public int CorrectCount { get; set; }
    
    /// <summary>
    /// Count of digits in wrong positions
    /// </summary>
    public int WrongPositionCount { get; set; }
    
    /// <summary>
    /// Count of incorrect digits (not in original)
    /// </summary>
    public int IncorrectCount { get; set; }
    
    /// <summary>
    /// Count of missing digits
    /// </summary>
    public int MissingCount { get; set; }
    
    /// <summary>
    /// Overall accuracy percentage
    /// </summary>
    public double AccuracyPercentage => DigitResults.Count > 0 
        ? (double)CorrectCount / DigitResults.Count * 100 
        : 0;
    
    /// <summary>
    /// The original number that was being memorized
    /// </summary>
    public string OriginalNumber { get; set; } = string.Empty;
    
    /// <summary>
    /// The user's input answer
    /// </summary>
    public string UserInput { get; set; } = string.Empty;
}