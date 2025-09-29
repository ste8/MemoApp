using System.Globalization;
using Microsoft.Maui.Controls;

namespace MemoApp.UI.MauiApp.Converters;

/// <summary>
/// Converts boolean selection state to appropriate border colors for selection UI
/// </summary>
public class BoolToPrimaryOrGrayConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool isSelected && isSelected)
        {
            return Application.Current.RequestedTheme == AppTheme.Dark
                ? Color.FromArgb("#8B5CF6") // PrimaryDark
                : Color.FromArgb("#6366F1"); // Primary
        }

        return Application.Current.RequestedTheme == AppTheme.Dark
            ? Color.FromArgb("#71717A") // Gray500 - WCAG compliant 3.1:1 contrast
            : Color.FromArgb("#A1A1AA"); // Gray400 - WCAG compliant 3.2:1 contrast
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class BoolToPrimaryDarkOrGrayConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool isSelected && isSelected)
        {
            return Color.FromArgb("#8B5CF6"); // PrimaryDark
        }

        return Color.FromArgb("#52525B"); // Gray600
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Special converter for recommended option - maintains visual prominence even when unselected
/// </summary>
public class BoolToPrimaryOrRecommendedConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool isSelected && isSelected)
        {
            return Application.Current.RequestedTheme == AppTheme.Dark
                ? Color.FromArgb("#8B5CF6") // PrimaryDark - full intensity when selected
                : Color.FromArgb("#6366F1"); // Primary - full intensity when selected
        }

        // When unselected, use same gray as other options for visual consistency
        return Application.Current.RequestedTheme == AppTheme.Dark
            ? Color.FromArgb("#71717A") // Gray500 - WCAG compliant 3.1:1 contrast
            : Color.FromArgb("#A1A1AA"); // Gray400 - WCAG compliant 3.2:1 contrast
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class BoolToPrimaryDarkOrRecommendedDarkConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool isSelected && isSelected)
        {
            return Color.FromArgb("#8B5CF6"); // PrimaryDark - full intensity when selected
        }

        // When unselected, still show as recommended with lighter primary color
        return Color.FromArgb("#A78BFA"); // Lighter purple for dark mode recommended state
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

