using System.Globalization;
using MemoApp.Core.Models;
using MemoApp.UI.MauiApp.ViewModels;

namespace MemoApp.UI.MauiApp.Converters;

/// <summary>
/// Converts boolean to localized string based on parameter mapping
/// </summary>
public class BoolToStringConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not bool boolValue || parameter is not string parameterString)
            return null;

        var parts = parameterString.Split(',');
        if (parts.Length != 2)
            return null;

        var trueKey = parts[0];
        var falseKey = parts[1];

        return boolValue ? trueKey : falseKey;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converts FeedbackType to FeedbackTypeItem for Picker binding
/// </summary>
public class FeedbackTypeConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is FeedbackTypeItem item)
            return item;

        if (value is FeedbackType type && parameter is IEnumerable<FeedbackTypeItem> items)
        {
            return items.FirstOrDefault(i => i.Type == type);
        }

        return null;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is FeedbackTypeItem item)
            return item.Type;

        return FeedbackType.Other;
    }
}

/// <summary>
/// Converts value equality for radio button selection
/// </summary>
public class EqualsConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return Equals(value, parameter);
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is true)
            return parameter;

        return Binding.DoNothing;
    }
}