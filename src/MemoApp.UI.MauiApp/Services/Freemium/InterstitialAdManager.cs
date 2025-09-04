using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace MemoApp.UI.MauiApp.Services.Freemium;

public class InterstitialAdManager : IInterstitialAdManager
{
    private readonly IAdMobService _adMobService;
    private readonly IExerciseCountService _exerciseCountService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<InterstitialAdManager> _logger;
    
    private const int AdFrequency = 5; // Show ad every 5 sessions
    
    public InterstitialAdManager(
        IAdMobService adMobService,
        IExerciseCountService exerciseCountService,
        IConfiguration configuration,
        ILogger<InterstitialAdManager> logger)
    {
        _adMobService = adMobService;
        _exerciseCountService = exerciseCountService ?? throw new ArgumentNullException(nameof(exerciseCountService));
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
            // Check total exercise count across all exercise types
            var totalCount = await _exerciseCountService.GetTotalExerciseCountAsync();
            
            // Show ad every 5 sessions
            return totalCount > 0 && totalCount % AdFrequency == 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if should show ad (total count logic)");
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
    
}