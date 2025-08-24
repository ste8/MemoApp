using Microsoft.Maui.Graphics;

namespace MemoApp.UI.MauiApp.Services;

/// <summary>
/// Service for measuring text dimensions with specific fonts and sizes
/// </summary>
public interface ITextMeasurementService
{
    /// <summary>
    /// Measures the size of the given text with the specified font and size
    /// </summary>
    /// <param name="text">The text to measure</param>
    /// <param name="fontFamily">The font family (e.g., "Courier")</param>
    /// <param name="fontSize">The font size</param>
    /// <returns>The size of the text</returns>
    SizeF MeasureText(string text, string fontFamily, float fontSize);
    
    /// <summary>
    /// Gets the width of a single character with the specified font and size
    /// </summary>
    /// <param name="character">The character to measure</param>
    /// <param name="fontFamily">The font family (e.g., "Courier")</param>
    /// <param name="fontSize">The font size</param>
    /// <returns>The width of the character</returns>
    float GetCharacterWidth(char character, string fontFamily, float fontSize);
}