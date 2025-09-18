using Microsoft.Maui.Handlers;
using MemoApp.UI.MauiApp.Controls;

namespace MemoApp.UI.MauiApp.Handlers;

/// <summary>
/// Custom handler to remove native borders from Editor controls across all platforms
/// </summary>
public static class BorderlessEditorHandler
{
    /// <summary>
    /// Configures the Editor handler to remove native borders on all platforms
    /// </summary>
    public static void Configure()
    {
        EditorHandler.Mapper.AppendToMapping("BorderlessEditor", (handler, view) =>
        {
            // Apply to all Editor controls, not just BorderlessEditor for FeedbackPage
            if (view is Editor editor)
            {
#if IOS || MACCATALYST
                // iOS and macOS - Remove border and background from UITextView
                handler.PlatformView.BackgroundColor = UIKit.UIColor.Clear;
                handler.PlatformView.Layer.BorderWidth = 0;
                // UITextView doesn't have BorderStyle, but we can remove the default styling
                if (handler.PlatformView.Subviews.Length > 0)
                {
                    foreach (var subview in handler.PlatformView.Subviews)
                    {
                        if (subview is UIKit.UIImageView imageView)
                        {
                            imageView.Hidden = true;
                        }
                    }
                }
#elif ANDROID
                // Android - Remove underline and background
                handler.PlatformView.SetBackgroundColor(Android.Graphics.Color.Transparent);
                handler.PlatformView.SetBackground(null);
                handler.PlatformView.SetPadding(0, 0, 0, 0);
#elif WINDOWS
                // Windows - Remove border and background from TextBox (Editor uses TextBox on Windows)
                handler.PlatformView.BorderThickness = new Microsoft.UI.Xaml.Thickness(0);
                handler.PlatformView.Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Transparent);
                handler.PlatformView.BorderBrush = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Transparent);
#endif
            }
        });
    }
}