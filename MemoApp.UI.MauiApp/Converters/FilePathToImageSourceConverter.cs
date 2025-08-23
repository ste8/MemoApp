using System.Globalization;

namespace MemoApp.UI.MauiApp.Converters;

/// <summary>
/// Converts local file paths to ImageSource objects for MAUI Image controls
/// </summary>
public class FilePathToImageSourceConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not string filePath || string.IsNullOrEmpty(filePath))
        {
            System.Diagnostics.Debug.WriteLine($"FilePathToImageSourceConverter: null or empty path: '{value}'");
            return null;
        }

        try
        {
            System.Diagnostics.Debug.WriteLine($"FilePathToImageSourceConverter: Converting path: '{filePath}'");
            
            // Check if file exists
            if (!File.Exists(filePath))
            {
                System.Diagnostics.Debug.WriteLine($"FilePathToImageSourceConverter: File does not exist: '{filePath}'");
                return null;
            }

            System.Diagnostics.Debug.WriteLine($"FilePathToImageSourceConverter: File exists, creating ImageSource for: '{filePath}'");
            
            // Convert local file path to ImageSource
            var imageSource = ImageSource.FromFile(filePath);
            System.Diagnostics.Debug.WriteLine($"FilePathToImageSourceConverter: Created ImageSource: {imageSource}");
            return imageSource;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"FilePathToImageSourceConverter: Exception: {ex.Message}");
            return null;
        }
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}