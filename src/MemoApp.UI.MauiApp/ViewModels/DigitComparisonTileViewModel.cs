using MemoApp.Core.BigNumber;
using Microsoft.Maui.Graphics;

namespace MemoApp.UI.MauiApp.ViewModels;

/// <summary>
/// ViewModel for displaying a single digit tile in the Wordle-style comparison
/// </summary>
public class DigitComparisonTileViewModel
{
    /// <summary>
    /// The digit character to display
    /// </summary>
    public string DisplayText { get; set; } = string.Empty;
    
    /// <summary>
    /// The background color for this tile based on match type
    /// </summary>
    public Color BackgroundColor { get; set; } = Colors.Gray;
    
    /// <summary>
    /// The text color for this tile (typically white for contrast)
    /// </summary>
    public Color TextColor { get; set; } = Colors.White;
    
    /// <summary>
    /// The border color for this tile
    /// </summary>
    public Color BorderColor { get; set; } = Colors.Transparent;
    
    /// <summary>
    /// The border thickness for this tile
    /// </summary>
    public double BorderThickness { get; set; } = 2;
    
    /// <summary>
    /// Whether to show a dashed border (for missing digits)
    /// </summary>
    public bool IsDashedBorder { get; set; } = false;
    
    /// <summary>
    /// The match type for accessibility and semantic information
    /// </summary>
    public DigitMatchType MatchType { get; set; } = DigitMatchType.Incorrect;
    
    /// <summary>
    /// The position index for this tile
    /// </summary>
    public int Position { get; set; }
    
    /// <summary>
    /// Accessibility description for screen readers
    /// </summary>
    public string AccessibilityDescription { get; set; } = string.Empty;
    
    /// <summary>
    /// Creates a tile view model from a digit comparison result
    /// </summary>
    public static DigitComparisonTileViewModel FromComparisonResult(
        DigitComparisonResult result, 
        bool isOriginalRow,
        Func<string, string> getLocalizedString)
    {
        var tile = new DigitComparisonTileViewModel
        {
            Position = result.Position,
            MatchType = result.MatchType
        };
        
        if (isOriginalRow)
        {
            // Original number row - always show in primary blue
            tile.DisplayText = result.OriginalDigit.ToString();
            tile.BackgroundColor = GetPrimaryColor();
            tile.TextColor = Colors.White;
            tile.BorderColor = Colors.Transparent;
            tile.BorderThickness = 0;
            tile.IsDashedBorder = false;
            tile.AccessibilityDescription = string.Format(
                getLocalizedString("BigNumber_OriginalDigitAccessibility"),
                result.OriginalDigit,
                result.Position + 1);
        }
        else
        {
            // User input row - color based on match type
            if (result.UserDigit.HasValue)
            {
                tile.DisplayText = result.UserDigit.Value.ToString();
            }
            else
            {
                tile.DisplayText = "?"; // Missing digit placeholder
            }
            
            switch (result.MatchType)
            {
                case DigitMatchType.Correct:
                    tile.BackgroundColor = Color.FromArgb("#28A745"); // Green
                    tile.TextColor = Colors.White;
                    tile.BorderColor = Colors.Transparent;
                    tile.BorderThickness = 0;
                    tile.AccessibilityDescription = string.Format(
                        getLocalizedString("BigNumber_CorrectDigitAccessibility"),
                        result.UserDigit,
                        result.Position + 1);
                    break;
                    
                case DigitMatchType.WrongPosition:
                    tile.BackgroundColor = Color.FromArgb("#CC5500"); // Orange
                    tile.TextColor = Colors.White;
                    tile.BorderColor = Colors.Transparent;
                    tile.BorderThickness = 0;
                    tile.AccessibilityDescription = string.Format(
                        getLocalizedString("BigNumber_WrongPositionDigitAccessibility"),
                        result.UserDigit,
                        result.Position + 1);
                    break;
                    
                case DigitMatchType.Incorrect:
                    tile.BackgroundColor = Color.FromArgb("#71717A"); // Gray
                    tile.TextColor = Colors.White;
                    tile.BorderColor = Colors.Transparent;
                    tile.BorderThickness = 0;
                    tile.AccessibilityDescription = string.Format(
                        getLocalizedString("BigNumber_IncorrectDigitAccessibility"),
                        result.UserDigit,
                        result.Position + 1);
                    break;
                    
                case DigitMatchType.Missing:
                    tile.BackgroundColor = Colors.White;
                    tile.TextColor = Color.FromArgb("#DC3545"); // Red text
                    tile.BorderColor = Color.FromArgb("#DC3545"); // Red border
                    tile.BorderThickness = 2;
                    tile.IsDashedBorder = true;
                    tile.AccessibilityDescription = string.Format(
                        getLocalizedString("BigNumber_MissingDigitAccessibility"),
                        result.Position + 1);
                    break;
            }
        }
        
        return tile;
    }
    
    private static Color GetPrimaryColor()
    {
        // Try to get the Primary color from app resources, fallback to blue
        if (Application.Current?.Resources?.TryGetValue("Primary", out var primary) == true && primary is Color primaryColor)
        {
            return primaryColor;
        }
        return Color.FromArgb("#007ACC"); // Default blue
    }
}