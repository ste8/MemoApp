using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace MemoApp.UI.MauiApp.Services.Freemium;

public class InterstitialAdManager : IInterstitialAdManager
{
    private readonly IAdMobService _adMobService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<InterstitialAdManager> _logger;
    
    private const string FlashNumbersKey = "FlashNumbers";
    private const string BigNumberKey = "BigNumber";
    private const int AdFrequency = 4; // Show ad every 4-5 sessions
    private readonly Random _random = new();
    
    public InterstitialAdManager(
        IAdMobService adMobService,
        IConfiguration configuration,
        ILogger<InterstitialAdManager> logger)
    {
        _adMobService = adMobService;
        _configuration = configuration;
        _logger = logger;
    }
    
    public async Task PrepareAdForExerciseAsync(string exerciseType)
    {
        try
        {
            // Always prepare ads since we're ads-only
            await _adMobService.PrepareInterstitialAd();
            _logger.LogInformation("Prepared interstitial ad for {ExerciseType}", exerciseType);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error preparing ad for {ExerciseType}", exerciseType);
        }
    }
    
    public async Task<bool> ShouldShowAdForExerciseAsync(string exerciseType)
    {
        try
        {
            // Always check ad frequency since we're ads-only
            var count = await GetExerciseCountAsync(exerciseType);
            
            // Show ad every 4-5 sessions (randomized)
            var threshold = AdFrequency + _random.Next(0, 2); // 4 or 5
            
            return count > 0 && count % threshold == 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if should show ad for {ExerciseType}", exerciseType);
            return false;
        }
    }
    
    public async Task ShowAdIfNeededAsync(string exerciseType)
    {
        try
        {
            if (await ShouldShowAdForExerciseAsync(exerciseType))
            {
                await _adMobService.ShowInterstitialAd();
                _logger.LogInformation("Showed interstitial ad for {ExerciseType}", exerciseType);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error showing ad for {ExerciseType}", exerciseType);
        }
    }
    
    public async Task IncrementExerciseCountAsync(string exerciseType)
    {
        try
        {
            var key = GetCountKeyForExercise(exerciseType);
            var count = Preferences.Get(key, 0);
            count++;
            Preferences.Set(key, count);
            
            _logger.LogInformation("Incremented {ExerciseType} count to {Count}", exerciseType, count);
            
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error incrementing exercise count for {ExerciseType}", exerciseType);
        }
    }
    
    private async Task<int> GetExerciseCountAsync(string exerciseType)
    {
        var key = GetCountKeyForExercise(exerciseType);
        return await Task.FromResult(Preferences.Get(key, 0));
    }
    
    private string GetCountKeyForExercise(string exerciseType)
    {
        return $"exercise_count_{exerciseType}";
    }
    
}