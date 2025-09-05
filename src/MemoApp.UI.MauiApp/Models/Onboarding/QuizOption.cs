namespace MemoApp.UI.MauiApp.Models.Onboarding;

/// <summary>
/// Represents an option in the interactive practice quiz
/// </summary>
public class QuizOption
{
    /// <summary>
    /// The emoji representation of the option
    /// </summary>
    public string Emoji { get; set; } = string.Empty;
    
    /// <summary>
    /// Localization key for the option text
    /// </summary>
    public string TextKey { get; set; } = string.Empty;
    
    /// <summary>
    /// Whether this option is the correct answer
    /// </summary>
    public bool IsCorrect { get; set; }
    
    /// <summary>
    /// The word this option represents (for reference/debugging)
    /// </summary>
    public string Word { get; set; } = string.Empty;
}

/// <summary>
/// Represents the complete interactive practice quiz
/// </summary>
public class InteractivePractice
{
    /// <summary>
    /// The number being practiced (e.g., 32)
    /// </summary>
    public int Number { get; set; }
    
    /// <summary>
    /// Localization key for the question text
    /// </summary>
    public string QuestionKey { get; set; } = string.Empty;
    
    /// <summary>
    /// The available answer options
    /// </summary>
    public List<QuizOption> Options { get; set; } = new();
    
    /// <summary>
    /// Localization key for correct answer feedback
    /// </summary>
    public string CorrectFeedbackKey { get; set; } = string.Empty;
    
    /// <summary>
    /// Localization key for try again feedback
    /// </summary>
    public string TryAgainFeedbackKey { get; set; } = string.Empty;
}