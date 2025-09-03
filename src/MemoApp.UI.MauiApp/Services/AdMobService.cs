using System.Diagnostics;
using Plugin.AdMob.Services;

namespace MemoApp.UI.MauiApp.Services;

public interface IAdMobService
{
    Task PrepareInterstitialAd();
    
    Task ShowInterstitialAd();
    
    bool IsInterstitialAdLoaded { get; }
}

public class AdMobService : IAdMobService
{
    private readonly IInterstitialAdService _interstitialAdService;
    
    public bool IsInterstitialAdLoaded => _interstitialAdService.IsAdLoaded;
    
    public AdMobService()
    {
        _interstitialAdService = ServiceProvider.GetRequiredService<IInterstitialAdService>();

        _interstitialAdService.OnAdLoaded += (_, __) => Debug.WriteLine("Interstitial ad prepared.");
    }

    public Task PrepareInterstitialAd()
    {
        // Uses the default interstitial ad unit ID configured in AdConfig
        if (!_interstitialAdService.IsAdLoaded) {
            _interstitialAdService.PrepareAd();
        } else {
            Debug.WriteLine("Interstitial already loaded.");
        }
        return Task.CompletedTask;
    }

    public Task ShowInterstitialAd()
    {
        if (_interstitialAdService.IsAdLoaded) 
        {
            _interstitialAdService.ShowAd();
        } 
        else 
        {
            Debug.WriteLine("Interstitial ad not loaded.");
        }
        return Task.CompletedTask;
    }
}

internal static class ServiceProvider
{
    public static TService? GetService<TService>()
        => Current.GetService<TService>();

    public static TService GetRequiredService<TService>() where TService : notnull
        => Current.GetRequiredService<TService>();

    public static IServiceProvider Current =>
        (IPlatformApplication.Current ?? throw new InvalidOperationException("Cannot resolve current application.")).Services;
}