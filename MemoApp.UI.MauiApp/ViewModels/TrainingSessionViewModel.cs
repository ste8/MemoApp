using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MemoApp.Core.MajorSystem;
using MemoApp.Core.Services.Analytics;
using MemoApp.Localization.Services;
using MemoApp.UI.MauiApp.Utilities;
using MemoApp.UI.MauiApp.Services;

namespace MemoApp.UI.MauiApp.ViewModels;

/// <summary>
/// ViewModel for the training session page.
/// Manages a training session using the MemoApp.Core game logic.
/// </summary>
[QueryProperty(nameof(RangeStart), "rangeStart")]
[QueryProperty(nameof(RangeEnd), "rangeEnd")]
public partial class TrainingSessionViewModel : BaseViewModel
{
    private readonly ILocalizationService _localizationService;
    private readonly IAnalyticsService _analyticsService;
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

    public TrainingSessionViewModel(ILocalizationService localizationService, IAnalyticsService analyticsService)
    {
        _localizationService = localizationService ?? throw new ArgumentNullException(nameof(localizationService));
        _analyticsService = analyticsService ?? throw new ArgumentNullException(nameof(analyticsService));
        Title = "Training Session";
    }

    /// <summary>
    /// Called when the page appears. Initializes the training session.
    /// </summary>
    public async Task InitializeAsync()
    {
        try
        {
            IsBusy = true;
            Title = $"Training: {RangeStart}-{RangeEnd}";
            
            
            _gameSession = new GameSession(RangeStart, RangeEnd);
            TotalNumbers = _gameSession.TotalNumbers;
            
            StartSession();
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Error", $"Failed to initialize training session: {ex.Message}", "OK");
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

        // Track session start analytics
        _sessionStartTime = DateTime.UtcNow;
        _totalAdvances = 0;
        
        var rangeSize = int.Parse(RangeEnd) - int.Parse(RangeStart) + 1;
        
        await _analyticsService.TrackEventAsync(AnalyticsEvents.TrainingSessionStarted, new Dictionary<string, object>
        {
            [AnalyticsParameters.ExerciseType] = ExerciseTypes.TrainingSession,
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
        
        // Track training step completion
        await _analyticsService.TrackEventAsync(AnalyticsEvents.TrainingStepCompleted, new Dictionary<string, object>
        {
            [AnalyticsParameters.ExerciseType] = ExerciseTypes.TrainingSession,
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
                [AnalyticsParameters.ExerciseType] = ExerciseTypes.TrainingSession,
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

        // Track session completion analytics
        await _analyticsService.TrackEventAsync(AnalyticsEvents.TrainingSessionCompleted, new Dictionary<string, object>
        {
            [AnalyticsParameters.ExerciseType] = ExerciseTypes.TrainingSession,
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
        Title = "Session Complete!";
    }

}