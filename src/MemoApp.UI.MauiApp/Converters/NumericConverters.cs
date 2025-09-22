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

/// <summary>
/// Converts TimeSpan values to user-friendly display strings for response times.
/// Formats times under 60 seconds as "2.45s", longer times as "1:23.45".
/// </summary>
public class TimeSpanToDisplayConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is TimeSpan timeSpan)
        {
            // Format: "2.45s" for times under 60 seconds
            if (timeSpan.TotalSeconds < 60)
            {
                return $"{timeSpan.TotalSeconds:F2}s";
            }

            // Format: "1:23.45" for longer times
            return $"{timeSpan:mm\\:ss\\.ff}";
        }

        return "0.00s";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException("TimeSpanToDisplayConverter is one-way only");
    }
}