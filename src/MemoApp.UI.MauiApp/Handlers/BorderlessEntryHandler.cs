using Microsoft.Maui.Handlers;
using MemoApp.UI.MauiApp.Controls;

namespace MemoApp.UI.MauiApp.Handlers;

/// <summary>
/// Custom handler to remove native borders from Entry controls across all platforms
/// </summary>
public static class BorderlessEntryHandler
{
    /// <summary>
    /// Configures the Entry handler to remove native borders on all platforms
    /// </summary>
    public static void Configure()
    {
        EntryHandler.Mapper.AppendToMapping("BorderlessEntry", (handler, view) =>
        {
            // Apply to all Entry controls, not just BorderlessEntry for FeedbackPage
            if (view is Entry entry)
            {
#if IOS || MACCATALYST
                // iOS and macOS - Remove border style and background
                handler.PlatformView.BorderStyle = UIKit.UITextBorderStyle.None;
                handler.PlatformView.BackgroundColor = UIKit.UIColor.Clear;
                handler.PlatformView.Layer.BorderWidth = 0;
#elif ANDROID
                // Android - Remove underline and background
                handler.PlatformView.SetBackgroundColor(Android.Graphics.Color.Transparent);
                handler.PlatformView.Background = null;
                handler.PlatformView.SetPadding(0, 0, 0, 0);
#elif WINDOWS
                // Windows - Remove border and background from TextBox
                handler.PlatformView.BorderThickness = new Microsoft.UI.Xaml.Thickness(0);
                handler.PlatformView.Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Transparent);
                handler.PlatformView.BorderBrush = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Transparent);
#endif
            }
        });
    }
}