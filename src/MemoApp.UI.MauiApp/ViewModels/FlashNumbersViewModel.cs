using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MemoApp.Core.MajorSystem;
using MemoApp.Core.Services.Analytics;
using MemoApp.Localization.Services;
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
    private readonly IInterstitialAdManager? _interstitialAdManager;
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
        IInterstitialAdManager? interstitialAdManager = null)
    {
        _localizationService = localizationService ?? throw new ArgumentNullException(nameof(localizationService));
        _analyticsService = analyticsService ?? throw new ArgumentNullException(nameof(analyticsService));
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
            
            // Prepare interstitial ad for potential showing
            if (_interstitialAdManager != null)
            {
                await _interstitialAdManager.PrepareAdForExerciseAsync("FlashNumbers");
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

    [RelayCommand]
    private async void StartSession()
    {
        if (_gameSession == null) return;

        // Show interstitial ad if needed (before starting exercise)
        if (_interstitialAdManager != null)
        {
            await _interstitialAdManager.ShowAdIfNeededAsync("FlashNumbers");
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
        if (_interstitialAdManager != null)
        {
            await _interstitialAdManager.IncrementExerciseCountAsync("FlashNumbers");
        }

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

}