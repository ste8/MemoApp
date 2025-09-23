namespace MemoApp.UI.MauiApp.Components;

public partial class MemoRaiseLogoComponent : ContentView
{
    public static readonly BindableProperty LogoHeightProperty = BindableProperty.Create(
        nameof(LogoHeight),
        typeof(double),
        typeof(MemoRaiseLogoComponent),
        DeviceInfo.Idiom == DeviceIdiom.Phone ? 70.0 : 80.0);

    public static readonly BindableProperty LogoWidthProperty = BindableProperty.Create(
        nameof(LogoWidth),
        typeof(double),
        typeof(MemoRaiseLogoComponent),
        DeviceInfo.Idiom == DeviceIdiom.Phone ? 175.0 : 200.0);

    public double LogoHeight
    {
        get => (double)GetValue(LogoHeightProperty);
        set => SetValue(LogoHeightProperty, value);
    }

    public double LogoWidth
    {
        get => (double)GetValue(LogoWidthProperty);
        set => SetValue(LogoWidthProperty, value);
    }

    public MemoRaiseLogoComponent()
    {
        InitializeComponent();
    }
}