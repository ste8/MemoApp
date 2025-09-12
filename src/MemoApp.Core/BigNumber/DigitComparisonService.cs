using System.Text;

namespace MemoApp.Core.BigNumber;

/// <summary>
/// Service for performing Wordle-style digit comparison between original and user numbers
/// </summary>
public class DigitComparisonService : IDigitComparisonService
{
    /// <summary>
    /// Compares the user's input against the original number using Wordle-style logic
    /// </summary>
    /// <param name="originalNumber">The original number that was memorized</param>
    /// <param name="userInput">The user's input answer</param>
    /// <returns>Detailed comparison results for each digit position</returns>
    public NumberComparisonResult CompareNumbers(string originalNumber, string userInput)
    {
        // Extract digits only from both inputs
        var originalDigits = ExtractDigits(originalNumber);
        var userDigits = ExtractDigits(userInput);
        
        var result = new NumberComparisonResult
        {
            OriginalNumber = originalNumber,
            UserInput = userInput
        };
        
        // Handle empty inputs
        if (string.IsNullOrEmpty(originalDigits))
        {
            return result;
        }
        
        // Determine the length to process (take the longer of the two inputs)
        var maxLength = Math.Max(originalDigits.Length, userDigits.Length);
        var digitResults = new List<DigitComparisonResult>();
        
        var originalArray = originalDigits.ToCharArray();
        var userArray = userDigits.ToCharArray();
        
        // First pass: Mark exact matches and create basic structure
        var originalUsed = new bool[originalArray.Length];
        var userUsed = new bool[userArray.Length];
        
        // Create results for all positions (based on original length or longer if user input is longer)
        for (int i = 0; i < maxLength; i++)
        {
            var digitResult = new DigitComparisonResult
            {
                Position = i
            };
            
            // Set original digit if position exists
            if (i < originalArray.Length)
            {
                digitResult.OriginalDigit = originalArray[i];
            }
            else
            {
                digitResult.OriginalDigit = ' '; // No original digit at this position
            }
            
            // Set user digit if position exists  
            if (i < userArray.Length)
            {
                digitResult.UserDigit = userArray[i];
                
                // Check for exact match
                if (i < originalArray.Length && originalArray[i] == userArray[i])
                {
                    digitResult.MatchType = DigitMatchType.Correct;
                    originalUsed[i] = true;
                    userUsed[i] = true;
                    result.CorrectCount++;
                }
            }
            else
            {
                // User input is shorter - this position is missing
                digitResult.UserDigit = null;
                digitResult.MatchType = DigitMatchType.Missing;
                result.MissingCount++;
            }
            
            digitResults.Add(digitResult);
        }
        
        // Second pass: Handle wrong position matches for unmatched user digits
        for (int userPos = 0; userPos < userArray.Length; userPos++)
        {
            if (userUsed[userPos]) continue; // Already matched exactly
            
            var userDigit = userArray[userPos];
            bool foundMatch = false;
            
            // Look for this digit in unused positions of the original
            for (int origPos = 0; origPos < originalArray.Length; origPos++)
            {
                if (originalUsed[origPos]) continue; // Already used
                
                if (originalArray[origPos] == userDigit)
                {
                    // Found a match in wrong position
                    digitResults[userPos].MatchType = DigitMatchType.WrongPosition;
                    result.WrongPositionCount++;
                    
                    originalUsed[origPos] = true;
                    userUsed[userPos] = true;
                    foundMatch = true;
                    break;
                }
            }
            
            // If no match found, mark as incorrect
            if (!foundMatch)
            {
                digitResults[userPos].MatchType = DigitMatchType.Incorrect;
                result.IncorrectCount++;
            }
        }
        
        result.DigitResults = digitResults;
        
        return result;
    }
    
    /// <summary>
    /// Extracts only digit characters from a string, removing spaces and other formatting
    /// </summary>
    /// <param name="input">Input string that may contain digits, spaces, and formatting</param>
    /// <returns>String containing only the digit characters</returns>
    private static string ExtractDigits(string input)
    {
        if (string.IsNullOrEmpty(input))
            return string.Empty;
            
        var sb = new StringBuilder();
        foreach (char c in input)
        {
            if (char.IsDigit(c))
            {
                sb.Append(c);
            }
        }
        
        return sb.ToString();
    }
}