namespace MemoApp.Core.MajorSystem.Models;

/// <summary>
/// Represents a category with its associated tokens
/// </summary>
public class CategoryTokens
{
    /// <summary>
    /// The category key (e.g., "numbers", "letters_upper", "symbols")
    /// </summary>
    public string CategoryKey { get; set; } = string.Empty;
    
    /// <summary>
    /// The localized display name for the category
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;
    
    /// <summary>
    /// List of tokens that belong to this category
    /// </summary>
    public List<string> Tokens { get; set; } = new();
    
    /// <summary>
    /// The sort order for displaying categories
    /// </summary>
    public int SortOrder { get; set; }
}