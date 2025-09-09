using System.Globalization;
using System.Linq;

namespace MemoApp.UI.MauiApp.Converters;

/// <summary>
/// Inverts a boolean value. Used for showing/hiding elements based on inverse conditions.
/// </summary>
public class InvertedBoolConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool boolValue)
            return !boolValue;
        
        return false;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool boolValue)
            return !boolValue;
        
        return false;
    }
}

/// <summary>
/// Checks if an object is not null. Useful for conditional visibility.
/// </summary>
public class IsNotNullConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value != null;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Checks if a string is not null or empty. Useful for conditional visibility of images.
/// </summary>
public class IsNotNullOrEmptyConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return !string.IsNullOrEmpty(value as string);
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Checks if a string is null or empty. Useful for conditional visibility of fallback elements.
/// </summary>
public class IsNullOrEmptyConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return string.IsNullOrEmpty(value as string);
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Checks if a numeric value is zero. Useful for empty state visibility.
/// </summary>
public class IsZeroConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is int intValue)
            return intValue == 0;
        if (value is double doubleValue)
            return doubleValue == 0.0;
        if (value is float floatValue)
            return floatValue == 0.0f;
        
        return false;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Checks if a numeric value is not zero. Useful for content visibility.
/// </summary>
public class IsNotZeroConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is int intValue)
            return intValue != 0;
        if (value is double doubleValue)
            return doubleValue != 0.0;
        if (value is float floatValue)
            return floatValue != 0.0f;
        
        return true;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converts boolean to FontAwesome star icon (filled for true, outline for false).
/// </summary>
public class BoolToStarConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool boolValue)
            return boolValue ? Utilities.FontAwesome.Solid.Star : Utilities.FontAwesome.Regular.Star;
        
        return Utilities.FontAwesome.Regular.Star;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converts boolean to FontAwesome font family (Solid for filled star, Regular for outline star).
/// </summary>
public class BoolToStarFontFamilyConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool boolValue)
            return boolValue ? Utilities.FontAwesome.FontFamilies.Solid : Utilities.FontAwesome.FontFamilies.Regular;
        
        return Utilities.FontAwesome.FontFamilies.Regular;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converts boolean to star color (primary for true, gray for false).
/// </summary>
public class BoolToStarColorConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool boolValue)
        {
            if (Application.Current?.Resources != null)
            {
                var resourceKey = boolValue ? "Primary" : "Gray600";
                if (Application.Current.Resources.TryGetValue(resourceKey, out var color))
                    return color;
            }
        }
        
        return Colors.Gray;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converts boolean to opacity (1.0 for true, 0.0 for false). 
/// Used for smooth visibility transitions without layout shifts.
/// </summary>
public class BoolToOpacityConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool boolValue)
            return boolValue ? 1.0 : 0.0;
        
        return 0.0;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is double doubleValue)
            return doubleValue > 0.5;
        
        return false;
    }
}

/// <summary>
/// Multi-value converter that performs AND logic on multiple boolean values.
/// Returns true only if all bound values are true. Used for complex visibility logic.
/// </summary>
public class MultiBooleanAndConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values == null || values.Length == 0)
            return false;
            
        // Check if all values are boolean and all are true
        return values.All(value => value is bool boolValue && boolValue);
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converts a string to boolean based on parameter comparison.
/// Returns true if the bound string equals the converter parameter.
/// </summary>
public class StringToBoolConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string stringValue && parameter is string parameterValue)
            return string.Equals(stringValue, parameterValue, StringComparison.OrdinalIgnoreCase);
        
        return false;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}