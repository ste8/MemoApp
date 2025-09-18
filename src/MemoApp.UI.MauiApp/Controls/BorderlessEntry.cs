namespace MemoApp.UI.MauiApp.Controls;

/// <summary>
/// Custom Entry control with no visible borders on any platform
/// </summary>
public class BorderlessEntry : Entry
{
    public BorderlessEntry()
    {
        BackgroundColor = Colors.Transparent;
    }
}