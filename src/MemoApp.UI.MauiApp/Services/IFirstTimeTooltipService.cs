namespace MemoApp.UI.MauiApp.Services;

/// <summary>
/// Service to manage first-time tooltips for new users
/// </summary>
public interface IFirstTimeTooltipService
{
    /// <summary>
    /// Check if a tooltip has been shown for a specific page
    /// </summary>
    Task<bool> HasTooltipBeenShownAsync(string pageKey);
    
    /// <summary>
    /// Mark a tooltip as shown for a specific page
    /// </summary>
    Task MarkTooltipShownAsync(string pageKey);
    
    /// <summary>
    /// Show a tooltip if it hasn't been shown before
    /// </summary>
    Task ShowTooltipIfFirstTimeAsync(string pageKey, string title, string message, string dismissText);
    
    /// <summary>
    /// Reset all tooltips (useful for testing or user request)
    /// </summary>
    Task ResetAllTooltipsAsync();
}