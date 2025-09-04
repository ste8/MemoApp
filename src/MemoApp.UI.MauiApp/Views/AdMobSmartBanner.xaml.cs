using Microsoft.Extensions.Configuration;

namespace MemoApp.UI.MauiApp.Views;

public partial class AdMobSmartBanner : ContentView
{
    public static readonly BindableProperty AdUnitIdProperty =
        BindableProperty.Create(nameof(AdUnitId), typeof(string), typeof(AdMobSmartBanner), string.Empty);

    public string AdUnitId
    {
        get => (string)GetValue(AdUnitIdProperty);
        private set => SetValue(AdUnitIdProperty, value);
    }

    public AdMobSmartBanner()
    {
        InitializeComponent();
        InitializeAdConfiguration();
    }

    private void InitializeAdConfiguration()
    {
        try
        {
            var configuration = Handler?.MauiContext?.Services?.GetService<IConfiguration>();
            if (configuration == null) 
            {
                System.Diagnostics.Debug.WriteLine("Warning: IConfiguration service not available for AdMobSmartBanner");
                return;
            }

#if ANDROID
            AdUnitId = configuration["Freemium:AdMob:Android:BannerAdUnitId"] ?? string.Empty;
#elif IOS
            AdUnitId = configuration["Freemium:AdMob:iOS:BannerAdUnitId"] ?? string.Empty;
#else
            AdUnitId = string.Empty;
#endif
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading ad configuration: {ex.Message}");
            AdUnitId = string.Empty;
        }
    }
}