using Microsoft.Maui.Handlers;

namespace MemoApp.UI.MauiApp.Handlers;

/// <summary>
/// Custom handler to remove native borders from Picker controls across all platforms
/// </summary>
public static class BorderlessPickerHandler
{
    /// <summary>
    /// Configures the Picker handler to remove native borders on all platforms
    /// </summary>
    public static void Configure()
    {
        PickerHandler.Mapper.AppendToMapping("BorderlessPicker", (handler, view) =>
        {
            if (view is Picker picker)
            {
#if IOS || MACCATALYST
                // iOS and macOS - Remove border style and background
                handler.PlatformView.BorderStyle = UIKit.UITextBorderStyle.None;
                handler.PlatformView.BackgroundColor = UIKit.UIColor.Clear;
                handler.PlatformView.Layer.BorderWidth = 0;
                handler.PlatformView.Layer.CornerRadius = 0;
#elif ANDROID
                // Android - Remove background drawable (which includes the border)
                handler.PlatformView.Background = null;
                handler.PlatformView.SetBackgroundColor(Android.Graphics.Color.Transparent);

                // Remove padding to prevent layout issues
                handler.PlatformView.SetPadding(0, 0, 0, 0);
#elif WINDOWS
                // Windows - Remove border and background from ComboBox
                handler.PlatformView.BorderThickness = new Microsoft.UI.Xaml.Thickness(0);
                handler.PlatformView.Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Transparent);
                handler.PlatformView.BorderBrush = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Transparent);
#endif
            }
        });
    }
}