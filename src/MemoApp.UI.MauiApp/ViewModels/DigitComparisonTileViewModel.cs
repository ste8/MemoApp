using MemoApp.Core.BigNumber;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Controls;

namespace MemoApp.UI.MauiApp.ViewModels;

/// <summary>
/// ViewModel for displaying a single digit tile in the Wordle-style comparison
/// Enhanced with color-blind friendly visual indicators
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
    /// Border pattern for color-blind accessibility
    /// Controls StrokeDashArray: null=solid, DoubleCollection{6,3}=dashed, DoubleCollection{1,3}=sparse fine dotted
    /// </summary>
    public DoubleCollection BorderPattern { get; set; } = null;

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
    /// Font weight for enhanced text contrast based on match type
    /// </summary>
    public string FontWeight { get; set; } = "Bold";

    /// <summary>
    /// Enhanced accessibility description including visual patterns
    /// </summary>
    public string EnhancedAccessibilityDescription { get; set; } = string.Empty;
    
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
            tile.BorderPattern = null; // Solid border
            tile.FontWeight = "Bold";
            tile.AccessibilityDescription = string.Format(
                getLocalizedString("BigNumber_OriginalDigitAccessibility"),
                result.OriginalDigit,
                result.Position + 1);
            tile.EnhancedAccessibilityDescription = tile.AccessibilityDescription;
        }
        else
        {
            // User input row - color and pattern based on match type
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
                    tile.BorderColor = Color.FromArgb("#1E7E34"); // Darker green border
                    tile.BorderThickness = 3;
                    tile.BorderPattern = null; // Solid border
                    tile.FontWeight = "Bold";
                    tile.AccessibilityDescription = string.Format(
                        getLocalizedString("BigNumber_CorrectDigitAccessibility"),
                        result.UserDigit,
                        result.Position + 1);
                    tile.EnhancedAccessibilityDescription = string.Format(
                        getLocalizedString("BigNumber_CorrectDigitEnhancedAccessibility") ??
                        "{0} at position {1}: Correct - green background with solid border",
                        result.UserDigit,
                        result.Position + 1);
                    break;

                case DigitMatchType.WrongPosition:
                    tile.BackgroundColor = Color.FromArgb("#CC5500"); // Orange
                    tile.TextColor = Colors.White;
                    tile.BorderColor = Color.FromArgb("#A0430D"); // Darker orange border
                    tile.BorderThickness = 3;
                    tile.BorderPattern = new DoubleCollection { 6, 3 }; // Dashed border - larger for visibility
                    tile.FontWeight = "Bold";
                    tile.AccessibilityDescription = string.Format(
                        getLocalizedString("BigNumber_WrongPositionDigitAccessibility"),
                        result.UserDigit,
                        result.Position + 1);
                    tile.EnhancedAccessibilityDescription = string.Format(
                        getLocalizedString("BigNumber_WrongPositionDigitEnhancedAccessibility") ??
                        "{0} at position {1}: Right digit, wrong position - orange background with dashed border",
                        result.UserDigit,
                        result.Position + 1);
                    break;

                case DigitMatchType.Incorrect:
                    tile.BackgroundColor = Color.FromArgb("#71717A"); // Gray
                    tile.TextColor = Colors.White;
                    tile.BorderColor = Color.FromArgb("#52525B"); // Darker gray border
                    tile.BorderThickness = 3;
                    tile.BorderPattern = new DoubleCollection { 1, 3 }; // Sparse fine dotted - better texture contrast vs dashed pattern
                    tile.FontWeight = "Bold";
                    tile.AccessibilityDescription = string.Format(
                        getLocalizedString("BigNumber_IncorrectDigitAccessibility"),
                        result.UserDigit,
                        result.Position + 1);
                    tile.EnhancedAccessibilityDescription = string.Format(
                        getLocalizedString("BigNumber_IncorrectDigitEnhancedAccessibility") ??
                        "{0} at position {1}: Incorrect - gray background with dotted border",
                        result.UserDigit,
                        result.Position + 1);
                    break;

                case DigitMatchType.Missing:
                    tile.BackgroundColor = Colors.White;
                    tile.TextColor = Color.FromArgb("#DC3545"); // Red text
                    tile.BorderColor = Color.FromArgb("#DC3545"); // Red border
                    tile.BorderThickness = 3;
                    tile.BorderPattern = null; // Solid thick border (already distinct)
                    tile.FontWeight = "Bold";
                    tile.AccessibilityDescription = string.Format(
                        getLocalizedString("BigNumber_MissingDigitAccessibility"),
                        result.Position + 1);
                    tile.EnhancedAccessibilityDescription = string.Format(
                        getLocalizedString("BigNumber_MissingDigitEnhancedAccessibility") ??
                        "Position {0}: Missing digit - white background with red thick border",
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