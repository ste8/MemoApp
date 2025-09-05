namespace MemoApp.UI.MauiApp.Models.Onboarding;

/// <summary>
/// Represents a Major System example showing how a number converts to a word/image
/// </summary>
public class MajorSystemExample
{
    /// <summary>
    /// The number being converted (e.g., 41)
    /// </summary>
    public int Number { get; set; }
    
    /// <summary>
    /// The final word result (e.g., "RAT" for English, "MOTO" for Italian)
    /// </summary>
    public string Word { get; set; } = string.Empty;
    
    /// <summary>
    /// The emoji representation of the word
    /// </summary>
    public string Emoji { get; set; } = string.Empty;
    
    /// <summary>
    /// The conversion steps showing digit to letter mapping
    /// </summary>
    public List<ConversionStep> ConversionSteps { get; set; } = new();
    
    /// <summary>
    /// Localization key for the remember text
    /// </summary>
    public string RememberTextKey { get; set; } = string.Empty;
}

/// <summary>
/// Represents a single step in the Major System conversion process
/// </summary>
public class ConversionStep
{
    /// <summary>
    /// The digit being converted
    /// </summary>
    public int Digit { get; set; }
    
    /// <summary>
    /// The letter it converts to
    /// </summary>
    public string Letter { get; set; } = string.Empty;
    
    /// <summary>
    /// Display text for this step (e.g., "4 = R")
    /// </summary>
    public string DisplayText => $"{Digit} = {Letter}";
}