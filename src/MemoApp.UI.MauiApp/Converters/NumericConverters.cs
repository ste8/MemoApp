using System.Globalization;

namespace MemoApp.UI.MauiApp.Converters;

/// <summary>
/// Performs division for calculating progress values.
/// Used for progress bars and percentage calculations.
/// </summary>
public class DivisionConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is int numerator && parameter is int denominator && denominator != 0)
            return (double)numerator / denominator;
        
        return 0.0;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converts response time to color for visual performance feedback.
/// Fast responses are green, slow responses are red.
/// </summary>
public class ResponseTimeToColorConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is TimeSpan responseTime)
        {
            var seconds = responseTime.TotalSeconds;
            
            // Fast (< 2 seconds): Green
            if (seconds < 2.0)
                return Colors.Green;
            
            // Medium (2-5 seconds): Orange
            if (seconds < 5.0)
                return Colors.Orange;
            
            // Slow (> 5 seconds): Red
            return Colors.Red;
        }
        
        return Colors.Gray;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}