using System.Globalization;

namespace MemoApp.UI.MauiApp.Converters;

public class PercentageConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is double percentage)
        {
            // Convert percentage (0-100) to decimal (0-1) for ProgressBar
            return percentage / 100.0;
        }
        return 0;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is double decimalValue)
        {
            // Convert decimal (0-1) to percentage (0-100)
            return decimalValue * 100.0;
        }
        return 0;
    }
}