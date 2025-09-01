using System.Diagnostics;
using Plugin.AdMob.Services;

namespace MemoApp.UI.MauiApp.Services;

public interface IAdMobService
{
    Task PrepareInterstitialAd(string? adUnitId = null);
    
    Task ShowInterstitialAd();
}

public class AdMobService : IAdMobService
{
    private readonly IInterstitialAdService _interstitialAdService;
    private string? _interstitialAdUnitId;
    
    public AdMobService()
    {
        _interstitialAdService = ServiceProvider.GetRequiredService<IInterstitialAdService>();

        _interstitialAdService.OnAdLoaded += (_, __) => Debug.WriteLine("Interstitial ad prepared.");
    }

    public Task PrepareInterstitialAd(string? adUnitId = null)
    {
        _interstitialAdUnitId = adUnitId;
        _interstitialAdService.PrepareAd(_interstitialAdUnitId);
        return Task.CompletedTask;
    }

    public Task ShowInterstitialAd()
    {
        if (_interstitialAdService.IsAdLoaded) {
            _interstitialAdService.ShowAd();
        } else {
            Debug.WriteLine("Interstitial NON loaded.");
        }
        _interstitialAdService.PrepareAd(_interstitialAdUnitId);
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