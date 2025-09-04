namespace MemoApp.UI.MauiApp.Views;

public partial class AdMobSmartBanner : ContentView
{
    public static readonly BindableProperty AndroidAdUnitIdProperty =
        BindableProperty.Create(nameof(AndroidAdUnitId), typeof(string), typeof(AdMobSmartBanner), string.Empty, propertyChanged: OnAdUnitIdChanged);

    public static readonly BindableProperty iOSAdUnitIdProperty =
        BindableProperty.Create(nameof(iOSAdUnitId), typeof(string), typeof(AdMobSmartBanner), string.Empty, propertyChanged: OnAdUnitIdChanged);

    public static readonly BindableProperty AdUnitIdProperty =
        BindableProperty.Create(nameof(AdUnitId), typeof(string), typeof(AdMobSmartBanner), string.Empty);

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

    public string AdUnitId
    {
        get => (string)GetValue(AdUnitIdProperty);
        private set => SetValue(AdUnitIdProperty, value);
    }

    public AdMobSmartBanner()
    {
        InitializeComponent();
        UpdateAdUnitId();
    }

    private static void OnAdUnitIdChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is AdMobSmartBanner banner)
        {
            banner.UpdateAdUnitId();
        }
    }

    private void UpdateAdUnitId()
    {
#if ANDROID
        AdUnitId = AndroidAdUnitId;
#elif IOS
        AdUnitId = iOSAdUnitId;
#else
        AdUnitId = string.Empty;
#endif
    }
}