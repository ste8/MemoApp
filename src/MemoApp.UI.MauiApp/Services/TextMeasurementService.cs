using Microsoft.Maui.Graphics;
using GFont = Microsoft.Maui.Graphics.Font;

namespace MemoApp.UI.MauiApp.Services;

/// <summary>
/// Cross-platform text measurement service using simplified approach
/// </summary>
public class TextMeasurementService : ITextMeasurementService
{
    public SizeF MeasureText(string text, string fontFamily, float fontSize)
    {
        if (string.IsNullOrEmpty(text))
            return SizeF.Zero;

        try
        {
            // For monospace fonts like Courier, we can calculate more accurately
            if (fontFamily?.Equals("Courier", StringComparison.OrdinalIgnoreCase) == true)
            {
                // Courier font has a consistent character width ratio
                float charWidth = fontSize * 0.6f; // This is more accurate for Courier
                float textHeight = fontSize * 1.2f; // Line height
                
                return new SizeF(text.Length * charWidth, textHeight);
            }
            
            // For other fonts, use a general calculation
            float averageCharWidth = fontSize * 0.5f;
            return new SizeF(text.Length * averageCharWidth, fontSize * 1.2f);
        }
        catch (Exception ex)
        {
            // Log the error and fall back to basic calculation
            System.Diagnostics.Debug.WriteLine($"Text measurement failed: {ex.Message}");
            
            // Basic fallback
            float charWidth = fontSize * 0.5f;
            return new SizeF(text.Length * charWidth, fontSize * 1.2f);
        }
    }

    public float GetCharacterWidth(char character, string fontFamily, float fontSize)
    {
        var size = MeasureText(character.ToString(), fontFamily, fontSize);
        return size.Width;
    }
}