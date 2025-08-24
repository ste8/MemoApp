namespace MemoApp.Core.MajorSystem.Models;

/// <summary>
/// Stores user's custom default preferences for token associations
/// </summary>
public class UserDefaultPreferences
{
    /// <summary>
    /// The pack ID these preferences apply to
    /// </summary>
    public string PackId { get; set; } = string.Empty;
    
    /// <summary>
    /// Custom defaults organized by category and token
    /// Key: category name, Value: Dictionary of token -> filename
    /// </summary>
    public Dictionary<string, Dictionary<string, string>> CustomDefaults { get; set; } = new();
    
    /// <summary>
    /// The last selected category
    /// </summary>
    public string? LastSelectedCategory { get; set; }
    
    /// <summary>
    /// Gets or sets a custom default for a specific token
    /// </summary>
    public void SetDefault(string category, string token, string filename)
    {
        if (!CustomDefaults.ContainsKey(category))
        {
            CustomDefaults[category] = new Dictionary<string, string>();
        }
        
        CustomDefaults[category][token] = filename;
    }
    
    /// <summary>
    /// Gets the custom default for a specific token if it exists
    /// </summary>
    public string? GetDefault(string category, string token)
    {
        if (CustomDefaults.TryGetValue(category, out var categoryDefaults))
        {
            if (categoryDefaults.TryGetValue(token, out var filename))
            {
                return filename;
            }
        }
        
        return null;
    }
    
    /// <summary>
    /// Clears all custom defaults for a specific category
    /// </summary>
    public void ClearCategoryDefaults(string category)
    {
        if (CustomDefaults.ContainsKey(category))
        {
            CustomDefaults.Remove(category);
        }
    }
}