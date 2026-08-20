using MemoApp.Core.BigNumber;

namespace MemoApp.UI.MauiApp.Services.BigNumber;

/// <inheritdoc />
public class NumberDisplayFontSizer : INumberDisplayFontSizer
{
    private readonly IContainerSizeService _containerSizeService;
    private readonly ITextMeasurementService _textMeasurementService;

    public NumberDisplayFontSizer(
        IContainerSizeService containerSizeService,
        ITextMeasurementService textMeasurementService)
    {
        _containerSizeService = containerSizeService ?? throw new ArgumentNullException(nameof(containerSizeService));
        _textMeasurementService = textMeasurementService ?? throw new ArgumentNullException(nameof(textMeasurementService));
    }

    public double CalculateBaseFontSize(string displayedNumber, FontSizePreference preference)
    {
        if (preference == FontSizePreference.Auto)
        {
            return CalculateAutoFontSize(displayedNumber);
        }
        else
        {
            double baseFontSize = preference switch
            {
                FontSizePreference.Small => 24,
                FontSizePreference.Medium => 32,
                FontSizePreference.Large => 42,
                FontSizePreference.ExtraLarge => 52,
                _ => 32
            };

            return DeviceInfo.Idiom == DeviceIdiom.Phone ? baseFontSize :
                   DeviceInfo.Idiom == DeviceIdiom.Tablet ? baseFontSize * 1.5 :
                   baseFontSize * 2.0;
        }
    }

    public double CalculateFontSizeForEvenLineBreaks(string displayedNumber, bool showSeparated, FontSizePreference preference)
    {
        if (string.IsNullOrEmpty(displayedNumber))
        {
            return CalculateBaseFontSize(displayedNumber, preference);
        }

        double baseFontSize = CalculateBaseFontSize(displayedNumber, preference);

        // Get actual container width from the UI
        double containerWidth = _containerSizeService.GetNumberDisplayContainerWidth();

        double minFontSize = DeviceInfo.Idiom == DeviceIdiom.Phone ? 12 :
                            DeviceInfo.Idiom == DeviceIdiom.Tablet ? 16 : 20;
        double maxFontSize = DeviceInfo.Idiom == DeviceIdiom.Phone ? 60 :
                            DeviceInfo.Idiom == DeviceIdiom.Tablet ? 80 : 100;

        // Check if base size already works
        if (HasEvenDigitsPerLine(displayedNumber, showSeparated, baseFontSize, containerWidth))
        {
            return baseFontSize;
        }

        double? adjustedSize = null;

        if (preference == FontSizePreference.Auto)
        {
            // For Auto: decrease by 1 until we get even digits per line
            adjustedSize = FindFontSizeWithEvenDigits(displayedNumber, showSeparated, baseFontSize, containerWidth, -1, minFontSize, maxFontSize);
        }
        else
        {
            // For fixed sizes: try increasing first, then decreasing
            adjustedSize = FindFontSizeWithEvenDigits(displayedNumber, showSeparated, baseFontSize, containerWidth, 1, minFontSize, maxFontSize)
                          ?? FindFontSizeWithEvenDigits(displayedNumber, showSeparated, baseFontSize, containerWidth, -1, minFontSize, maxFontSize);
        }

        return adjustedSize ?? (preference == FontSizePreference.Auto ? minFontSize : baseFontSize);
    }

    private double? FindFontSizeWithEvenDigits(string displayedNumber, bool showSeparated, double startSize, double containerWidth, int increment, double minSize, double maxSize)
    {
        double currentSize = startSize + increment; // Skip the starting size since we already checked it
        
        while (currentSize >= minSize && currentSize <= maxSize)
        {
            if (HasEvenDigitsPerLine(displayedNumber, showSeparated, currentSize, containerWidth))
            {
                return currentSize;
            }
            
            currentSize += increment;
        }
        
        return null; // No suitable size found
    }
    
    private bool HasEvenDigitsPerLine(string displayedNumber, bool showSeparated, double fontSize, double containerWidth)
    {
        // Use actual text measurement for accurate calculation
        var textSize = _textMeasurementService.MeasureText(displayedNumber, "Courier", (float)fontSize);
        
        // Check if the text fits in one line
        if (textSize.Width <= containerWidth)
        {
            // Fits on one line - acceptable regardless of even/odd
            return true;
        }
        
        // Calculate character width using actual measurement
        float charWidth = _textMeasurementService.GetCharacterWidth('0', "Courier", (float)fontSize);
        int maxCharsPerLine = (int)(containerWidth / charWidth);
        
        // For separated format, account for spaces
        if (showSeparated)
        {
            // Count actual digits (excluding spaces)
            string digitsOnly = displayedNumber.Replace(" ", "");
            int totalDigits = digitsOnly.Length;
            
            // Calculate how digits would be distributed
            return WouldHaveEvenDigitsPerLineSeparated(totalDigits, maxCharsPerLine);
        }
        else
        {
            // For continuous format, simple calculation
            int totalDigits = displayedNumber.Length;
            return WouldHaveEvenDigitsPerLineContinuous(totalDigits, maxCharsPerLine);
        }
    }
    
    private bool WouldHaveEvenDigitsPerLineContinuous(int totalDigits, int maxCharsPerLine)
    {
        if (totalDigits <= maxCharsPerLine)
        {
            // Fits on one line - acceptable regardless of even/odd
            return true;
        }
        
        // Check if all lines except the last would have even digits
        int fullLines = totalDigits / maxCharsPerLine;
        int remainder = totalDigits % maxCharsPerLine;
        
        // If maxCharsPerLine is odd, all full lines would have odd digits - not acceptable
        if (maxCharsPerLine % 2 != 0)
            return false;
            
        // All full lines would have even digits, which is good
        return true;
    }
    
    private bool WouldHaveEvenDigitsPerLineSeparated(int totalDigits, int maxCharsPerLine)
    {
        if (totalDigits <= maxCharsPerLine)
        {
            // Fits on one line - acceptable
            return true;
        }
        
        // For separated format, each pair takes 3 characters (e.g., "12 ")
        // except the last pair on each line doesn't need trailing space
        int pairsPerLine = maxCharsPerLine / 3;
        if (pairsPerLine == 0) return false; // Too narrow
        
        int digitsPerLine = pairsPerLine * 2; // Each pair = 2 digits
        
        // Check if digits per line is even (it should be since each pair = 2 digits)
        return digitsPerLine % 2 == 0;
    }

    private double CalculateAutoFontSize(string displayedNumber)
    {
        if (string.IsNullOrEmpty(displayedNumber))
        {
            // Default size when no number is displayed
            return DeviceInfo.Idiom == DeviceIdiom.Phone ? 32 :
                   DeviceInfo.Idiom == DeviceIdiom.Tablet ? 48 : 64;
        }

        // Get actual container width
        double containerWidth = _containerSizeService.GetNumberDisplayContainerWidth();

        // Define font size limits
        double minFontSize = DeviceInfo.Idiom == DeviceIdiom.Phone ? 16 :
                            DeviceInfo.Idiom == DeviceIdiom.Tablet ? 20 : 24;
        double maxFontSize = DeviceInfo.Idiom == DeviceIdiom.Phone ? 48 :
                            DeviceInfo.Idiom == DeviceIdiom.Tablet ? 64 : 80;

        // Binary search for the optimal font size that fits the container
        double low = minFontSize;
        double high = maxFontSize;
        double bestSize = minFontSize;

        while (high - low > 1)
        {
            double mid = (low + high) / 2;
            var textSize = _textMeasurementService.MeasureText(displayedNumber, "Courier", (float)mid);
            
            if (textSize.Width <= containerWidth)
            {
                bestSize = mid;
                low = mid;
            }
            else
            {
                high = mid;
            }
        }
        
        return bestSize;
    }
}
