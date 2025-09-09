using MemoApp.Localization.Services;

namespace MemoApp.UI.MauiApp.Services;

/// <summary>
/// Implementation of first-time tooltip service using Preferences for persistence
/// </summary>
public class FirstTimeTooltipService : IFirstTimeTooltipService
{
    private const string TooltipPrefix = "Tooltip_Shown_";
    private readonly ILocalizationService _localizationService;
    
    public FirstTimeTooltipService(ILocalizationService localizationService)
    {
        _localizationService = localizationService;
    }
    
    public Task<bool> HasTooltipBeenShownAsync(string pageKey)
    {
        var key = $"{TooltipPrefix}{pageKey}";
        var hasBeenShown = Preferences.Get(key, false);
        return Task.FromResult(hasBeenShown);
    }
    
    public Task MarkTooltipShownAsync(string pageKey)
    {
        var key = $"{TooltipPrefix}{pageKey}";
        Preferences.Set(key, true);
        return Task.CompletedTask;
    }
    
    public async Task ShowTooltipIfFirstTimeAsync(string pageKey, string title, string message, string dismissText)
    {
        // Check if tooltip has already been shown
        if (await HasTooltipBeenShownAsync(pageKey))
            return;
        
        // Show the tooltip
        if (Application.Current?.MainPage != null)
        {
            await Application.Current.MainPage.DisplayAlert(
                title,
                message,
                dismissText);
            
            // Mark as shown
            await MarkTooltipShownAsync(pageKey);
        }
    }
    
    public Task ResetAllTooltipsAsync()
    {
        // Reset all tooltip preferences
        var keysToRemove = new List<string>
        {
            $"{TooltipPrefix}Associations",
            $"{TooltipPrefix}FlashNumbers",
            $"{TooltipPrefix}BigNumbers"
        };
        
        foreach (var key in keysToRemove)
        {
            Preferences.Remove(key);
        }
        
        return Task.CompletedTask;
    }
}