namespace MemoApp.UI.MauiApp.Views;

public partial class AdMobSmartBanner : ContentView
{
    public static readonly BindableProperty AndroidAdUnitIdProperty =
        BindableProperty.Create(nameof(AndroidAdUnitId), typeof(string), typeof(AdMobSmartBanner), "android-ad-unit-id");

    public static readonly BindableProperty iOSAdUnitIdProperty =
        BindableProperty.Create(nameof(iOSAdUnitId), typeof(string), typeof(AdMobSmartBanner), "ios-ad-unit-id");

    public string AndroidAdUnitId
    {
        get => (string)GetValue(AndroidAdUnitIdProperty);
        set => SetValue(AndroidAdUnitIdProperty, value);
    }

    public string iOSAdUnitId
    {
        get => (string)GetValue(iOSAdUnitIdProperty);
        set => SetValue(iOSAdUnitIdProperty, value);
    }

    public AdMobSmartBanner()
    {
        InitializeComponent();
    }
}