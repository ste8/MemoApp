using System.Globalization;
using MemoApp.Localization.Extensions;

namespace MemoApp.UI.MauiApp.Converters;

/// <summary>
/// Formats a bound value into a localized format string.
///
/// Replaces the pattern <c>StringFormat='{localization:Localize Key}'</c>, which silently
/// does not work: a single-quoted markup-extension argument is treated as a literal, so the
/// inner extension is never evaluated and the raw markup reaches string.Format.
///
/// Usage:
/// <code>
/// Text="{Binding AttachedImageCount,
///        Converter={StaticResource LocalizedFormatConverter},
///        ConverterParameter=Feedback_ImagesAttached}"
/// </code>
/// where the resource value contains a <c>{0}</c> placeholder.
/// </summary>
public class LocalizedFormatConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (parameter is not string key || string.IsNullOrWhiteSpace(key))
        {
            return value?.ToString() ?? string.Empty;
        }

        var format = LocalizationResourceManager.Instance[key];

        if (string.IsNullOrEmpty(format))
        {
            return value?.ToString() ?? string.Empty;
        }

        try
        {
            return string.Format(culture, format, value);
        }
        catch (FormatException)
        {
            // A malformed resource value must not take the page down.
            return format;
        }
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException($"{nameof(LocalizedFormatConverter)} is one-way only.");
}
