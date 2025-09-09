using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MemoApp.Core.MajorSystem;
using MemoApp.Core.Services.Analytics;
using MemoApp.Core.Services.Sharing;
using MemoApp.Core.Models;
using MemoApp.Localization.Services;
using MemoApp.UI.MauiApp.Constants;
using MemoApp.UI.MauiApp.Utilities;
using MemoApp.UI.MauiApp.Services;
using MemoApp.UI.MauiApp.Services.Freemium;

namespace MemoApp.UI.MauiApp.ViewModels;

/// <summary>
/// ViewModel for the Flash Numbers exercise page.
/// Manages a Flash Numbers session using the MemoApp.Core game logic.
/// </summary>
[QueryProperty(nameof(RangeStart), "rangeStart")]
[QueryProperty(nameof(RangeEnd), "rangeEnd")]
public partial class FlashNumbersViewModel : BaseViewModel
{
    private readonly ILocalizationService _localizationService;
    private readonly IAnalyticsService _analyticsService;
    private readonly IShareService _shareService;
    private readonly IInterstitialAdManager? _interstitialAdManager;
    private readonly IExerciseCountService _exerciseCountService;
    private readonly IFirstTimeTooltipService _tooltipService;
    private GameSession? _gameSession;
    
    // Analytics tracking
    private DateTime _sessionStartTime;
    private int _totalAdvances;

    [ObservableProperty]
    private string rangeStart = "00";

    [ObservableProperty]
    private string rangeEnd = "09";

    [ObservableProperty]
    private string currentNumber = "";

    [ObservableProperty]
    private int totalNumbers;

    [ObservableProperty]
    private int completedNumbers;

    [ObservableProperty]
    private int remainingNumbers;

    [ObservableProperty]
    private double progressValue;

    [ObservableProperty]
    private bool isSessionActive;

    [ObservableProperty]
    private bool isSessionCompleted;

    [ObservableProperty]
    private SessionStatistics? sessionStatistics;

    [ObservableProperty]
    private ObservableCollection<NumberPerformance> performances = new();

    public FlashNumbersViewModel(
        ILocalizationService localizationService, 
        IAnalyticsService analyticsService,
        IShareService shareService,
        IExerciseCountService exerciseCountService,
        IFirstTimeTooltipService tooltipService,
        IInterstitialAdManager? interstitialAdManager = null)
    {
        _localizationService = localizationService ?? throw new ArgumentNullException(nameof(localizationService));
        _analyticsService = analyticsService ?? throw new ArgumentNullException(nameof(analyticsService));
        _shareService = shareService ?? throw new ArgumentNullException(nameof(shareService));
        _exerciseCountService = exerciseCountService ?? throw new ArgumentNullException(nameof(exerciseCountService));
        _tooltipService = tooltipService ?? throw new ArgumentNullException(nameof(tooltipService));
        _interstitialAdManager = interstitialAdManager;
        Title = "Flash Numbers";
    }

    /// <summary>
    /// Called when the page appears. Initializes the training session.
    /// </summary>
    public async Task InitializeAsync()
    {
        try
        {
            IsBusy = true;
            Title = $"Flash Numbers: {RangeStart}-{RangeEnd}";
            
            // Show first-time tooltip
            await ShowFirstTimeTooltipAsync();
            
            // Prepare interstitial ad for potential showing
            if (_interstitialAdManager != null)
            {
                await _interstitialAdManager.PrepareAdForExerciseAsync(ExerciseTypes.FlashNumbers);
            }
            
            _gameSession = new GameSession(RangeStart, RangeEnd);
            TotalNumbers = _gameSession.TotalNumbers;
            
            StartSession();
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Error", $"Failed to initialize Flash Numbers session: {ex.Message}", "OK");
            await Shell.Current.GoToAsync("..");
        }
        finally
        {
            IsBusy = false;
        }
    }
    
    private async Task ShowFirstTimeTooltipAsync()
    {
        try
        {
            var title = _localizationService.GetString("Tooltip_FlashNumbers_Title");
            var message = _localizationService.GetString("Tooltip_FlashNumbers_Message");
            var dismissText = _localizationService.GetString("Tooltip_Dismiss");
            
            await _tooltipService.ShowTooltipIfFirstTimeAsync("FlashNumbers", title, message, dismissText);
        }
        catch (Exception ex)
        {
            // Log but don't fail the initialization
            System.Diagnostics.Debug.WriteLine($"Error showing tooltip: {ex.Message}");
        }
    }

    [RelayCommand]
    private async void StartSession()
    {
        if (_gameSession == null) return;

        // Show interstitial ad if needed (before starting exercise)
        if (_interstitialAdManager != null)
        {
            await _interstitialAdManager.ShowAdIfNeededAsync(ExerciseTypes.FlashNumbers);
        }

        // Track session start analytics
        _sessionStartTime = DateTime.UtcNow;
        _totalAdvances = 0;
        
        var rangeSize = int.Parse(RangeEnd) - int.Parse(RangeStart) + 1;
        
        await _analyticsService.TrackEventAsync(AnalyticsEvents.FlashNumbersStarted, new Dictionary<string, object>
        {
            [AnalyticsParameters.ExerciseType] = ExerciseTypes.FlashNumbers,
            [AnalyticsParameters.NumberRange] = $"{RangeStart}_{RangeEnd}",
            [AnalyticsParameters.DifficultyDigits] = rangeSize,
            [AnalyticsParameters.TokensAvailable] = TotalNumbers
        });

        _gameSession.StartSession();
        UpdateSessionStatus();
        IsSessionActive = true;
        IsSessionCompleted = false;
    }

    [RelayCommand]
    private async void AdvanceToNext()
    {
        if (_gameSession == null || !IsSessionActive) return;

        _totalAdvances++;
        
        // Track Flash Numbers step completion
        await _analyticsService.TrackEventAsync(AnalyticsEvents.FlashNumbersStepCompleted, new Dictionary<string, object>
        {
            [AnalyticsParameters.ExerciseType] = ExerciseTypes.FlashNumbers,
            [AnalyticsParameters.Token] = CurrentNumber,
            [AnalyticsParameters.ProgressPercentage] = (int)(ProgressValue * 100),
            [AnalyticsParameters.Attempts] = _totalAdvances
        });

        _gameSession.AdvanceToNext();
        UpdateSessionStatus();

        if (_gameSession.State == SessionState.Completed)
        {
            CompleteSession();
        }
    }

    [RelayCommand]
    private async Task ShowStatistics()
    {
        if (SessionStatistics == null) return;


        // Set the statistics data in the StatisticsViewModel
        StatisticsViewModel.SetPendingStatistics(SessionStatistics.Value);
        
        await Shell.Current.GoToAsync("statistics");
    }

    [RelayCommand]
    private async Task StartNewSession()
    {
        // Track user starting a new session
        if (_gameSession != null && IsSessionActive)
        {
            // Session was abandoned before completion
            var currentTime = DateTime.UtcNow;
            var partialDuration = (currentTime - _sessionStartTime).TotalSeconds;
            
            await _analyticsService.TrackEventAsync(AnalyticsEvents.ExerciseAbandoned, new Dictionary<string, object>
            {
                [AnalyticsParameters.ExerciseType] = ExerciseTypes.FlashNumbers,
                [AnalyticsParameters.NumberRange] = $"{RangeStart}_{RangeEnd}",
                [AnalyticsParameters.DurationSeconds] = (int)partialDuration,
                [AnalyticsParameters.AbandonmentReason] = AbandonmentReasons.UserQuit,
                [AnalyticsParameters.AbandonmentPoint] = PhaseTypes.Setup,
                [AnalyticsParameters.ProgressPercentage] = (int)(ProgressValue * 100)
            });
        }
        
        await Shell.Current.GoToAsync("..");
    }

    private void UpdateSessionStatus()
    {
        if (_gameSession == null) return;

        CurrentNumber = _gameSession.CurrentNumber.HasValue 
            ? NumberFormatHelper.FormatNumber(_gameSession.CurrentNumber.Value, _localizationService) 
            : "";
        CompletedNumbers = _gameSession.CompletedNumbers;
        RemainingNumbers = _gameSession.RemainingNumbers;
        ProgressValue = TotalNumbers > 0 ? (double)CompletedNumbers / TotalNumbers : 0.0;
        
        // Update performances collection for real-time display
        Performances.Clear();
        foreach (var performance in _gameSession.Performances)
        {
            Performances.Add(performance);
        }
    }

    private async void CompleteSession()
    {
        if (_gameSession == null) return;

        var sessionEndTime = DateTime.UtcNow;
        var totalDuration = (sessionEndTime - _sessionStartTime).TotalSeconds;
        var statistics = _gameSession.GetStatistics();

        // Increment exercise count for ad frequency tracking
        await _exerciseCountService.IncrementExerciseCountAsync(ExerciseTypes.FlashNumbers);

        // Track session completion analytics
        await _analyticsService.TrackEventAsync(AnalyticsEvents.FlashNumbersCompleted, new Dictionary<string, object>
        {
            [AnalyticsParameters.ExerciseType] = ExerciseTypes.FlashNumbers,
            [AnalyticsParameters.NumberRange] = $"{RangeStart}_{RangeEnd}",
            [AnalyticsParameters.DurationSeconds] = (int)totalDuration,
            [AnalyticsParameters.Success] = true,
            [AnalyticsParameters.TokensAvailable] = TotalNumbers,
            [AnalyticsParameters.Attempts] = _totalAdvances,
            [AnalyticsParameters.ProgressPercentage] = 100
        });

        IsSessionActive = false;
        IsSessionCompleted = true;
        SessionStatistics = statistics;
        Title = "Flash Numbers Complete!";
    }

    [RelayCommand]
    private async Task ShareResultsAsync()
    {
        try
        {
            if (_gameSession?.GetStatistics() is not { } statistics)
            {
                await Shell.Current.DisplayAlert(
                    _localizationService.GetString("Error_Title"), 
                    "No results to share", 
                    _localizationService.GetString("Common_OK"));
                return;
            }

            // Calculate totals for sharing
            var sessionEndTime = DateTime.UtcNow;
            var totalDuration = (sessionEndTime - _sessionStartTime).TotalSeconds;
            var averageTime = TotalNumbers > 0 ? totalDuration / TotalNumbers : 0;
            
            // For now, use average as fastest time (we don't have individual timing data readily available)
            var fastestTime = averageTime;

            // Create shareable result
            var shareResult = new FlashNumbersShareResult
            {
                ExerciseType = "FlashNumbers",
                Duration = TimeSpan.FromSeconds(totalDuration),
                TotalNumbers = TotalNumbers,
                NumberRange = $"{RangeStart}-{RangeEnd}",
                AverageTimePerNumber = TimeSpan.FromSeconds(averageTime),
                FastestTime = TimeSpan.FromSeconds(fastestTime)
            };

            // Generate localized share content
            var shareContent = shareResult.GenerateShareContent(key => _localizationService.GetString(key));

            // Track share initiation
            await _analyticsService.TrackEventAsync(AnalyticsEvents.ShareInitiated, new Dictionary<string, object>
            {
                [AnalyticsParameters.ExerciseType] = shareResult.ExerciseType,
                ["total_numbers"] = shareResult.TotalNumbers,
                ["duration_seconds"] = (int)shareResult.Duration.TotalSeconds
            });

            // Attempt to share
            var shareSuccess = await _shareService.ShareTextAsync(shareContent.Title, shareContent.Message);

            if (shareSuccess)
            {
                // Track successful share
                await _analyticsService.TrackEventAsync(AnalyticsEvents.ShareCompleted, new Dictionary<string, object>
                {
                    [AnalyticsParameters.ExerciseType] = shareResult.ExerciseType,
                    ["share_method"] = "native"
                });
            }
            else
            {
                // Track failed share
                await _analyticsService.TrackEventAsync(AnalyticsEvents.ShareFailed, new Dictionary<string, object>
                {
                    [AnalyticsParameters.ExerciseType] = shareResult.ExerciseType,
                    ["error"] = "share_cancelled_or_failed"
                });
            }
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert(
                _localizationService.GetString("Error_Title"), 
                $"Failed to share results: {ex.Message}", 
                _localizationService.GetString("Common_OK"));

            // Track share error
            await _analyticsService.TrackEventAsync(AnalyticsEvents.ShareFailed, new Dictionary<string, object>
            {
                [AnalyticsParameters.ExerciseType] = "FlashNumbers",
                ["error"] = ex.Message
            });
        }
    }

}