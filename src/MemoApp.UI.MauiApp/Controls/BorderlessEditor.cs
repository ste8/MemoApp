namespace MemoApp.UI.MauiApp.Controls;

/// <summary>
/// Custom Editor control with no visible borders on any platform
/// </summary>
public class BorderlessEditor : Editor
{
    public BorderlessEditor()
    {
        BackgroundColor = Colors.Transparent;
    }
}