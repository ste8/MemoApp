using System.Globalization;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace MemoApp.UI.MauiApp.Converters;

/// <summary>
/// Converter for boolean values to colors with parameters
/// Usage: {Binding IsSelected, Converter={StaticResource BoolToColorConverter}, ConverterParameter='Selected|Default'}
/// </summary>
public class BoolToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not bool boolValue || parameter is not string parameterString)
            return Colors.Transparent;
            
        var parts = parameterString.Split('|');
        if (parts.Length != 2)
            return Colors.Transparent;
            
        var trueColor = parts[0];
        var falseColor = parts[1];
        
        var colorKey = boolValue ? trueColor : falseColor;
        
        // Map to predefined colors
        return colorKey switch
        {
            "Selected" => Color.FromArgb("#E8E4FF"),
            "SelectedBorder" => Color.FromArgb("#512BD4"),
            "Default" => Color.FromArgb("#FAFAFA"),
            "DefaultBorder" => Color.FromArgb("#E5E5E5"),
            "Primary" => Color.FromArgb("#512BD4"),
            "Success" => Color.FromArgb("#28A745"),
            "Warning" => Color.FromArgb("#FFC107"),
            "Danger" => Color.FromArgb("#DC3545"),
            _ => Colors.Transparent
        };
    }
    
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}


/// <summary>
/// Converter for pack state to appropriate icon
/// </summary>
public class PackStateToIconConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not string state)
            return "📦";
            
        return state switch
        {
            "Downloaded" => "✅",
            "UpdateAvailable" => "🔄",
            "Downloading" => "⬇️",
            "Error" => "❌",
            _ => "📦"
        };
    }
    
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converter for pack size bytes to human-readable format
/// </summary>
public class FileSizeConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not long bytes)
            return "Unknown";
            
        string[] sizes = { "B", "KB", "MB", "GB" };
        double len = bytes;
        int order = 0;
        
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len /= 1024;
        }
        
        return $"{len:0.##} {sizes[order]}";
    }
    
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converter for selection mode string to SelectionMode enum
/// </summary>
public class StringToSelectionModeConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not string mode)
            return SelectionMode.None;
            
        return mode.ToLower() switch
        {
            "single" => SelectionMode.Single,
            "multiple" => SelectionMode.Multiple,
            _ => SelectionMode.None
        };
    }
    
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is SelectionMode mode)
            return mode.ToString();
        return "None";
    }
}

/// <summary>
/// Multi-value converter for complex visibility logic
/// </summary>
public class MultiConditionConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values == null || values.Length == 0)
            return false;
            
        var condition = parameter?.ToString()?.ToLower();
        
        return condition switch
        {
            "and" => values.All(v => v is bool b && b),
            "or" => values.Any(v => v is bool b && b),
            "not" => values.Any(v => v is bool b && !b),
            _ => false
        };
    }
    
    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converter for responsive grid columns based on screen size
/// </summary>
public class ResponsiveColumnsConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not double width)
            return 1;
            
        // Responsive breakpoints
        if (width < 768)
            return 1; // Mobile
        else if (width < 1200)
            return 2; // Tablet
        else
            return 3; // Desktop
    }
    
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Animation duration converter for different platforms
/// </summary>
public class PlatformAnimationDurationConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var baseDuration = value is int duration ? duration : 300;
        
        // Adjust duration based on platform preferences
        if (DeviceInfo.Platform == DevicePlatform.iOS)
            return baseDuration;
        else if (DeviceInfo.Platform == DevicePlatform.Android)
            return (int)(baseDuration * 0.8); // Slightly faster on Android
        else
            return baseDuration;
    }
    
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}