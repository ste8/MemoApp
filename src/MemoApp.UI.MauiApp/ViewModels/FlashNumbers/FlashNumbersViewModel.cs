using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MemoApp.Core.MajorSystem;
using MemoApp.Core.MajorSystem.Services;
using MemoApp.Core.MajorSystem.Models;
using MemoApp.Core.Services.Analytics;
using MemoApp.Core.Services.Sharing;
using MemoApp.Core.Models;
using MemoApp.Localization.Services;
using MemoApp.UI.MauiApp.Constants;
using MemoApp.UI.MauiApp.Utilities;
using MemoApp.UI.MauiApp.Services;
using MemoApp.UI.MauiApp.Services.Freemium;
using MemoApp.UI.MauiApp.Services.ErrorHandling;

namespace MemoApp.UI.MauiApp.ViewModels.FlashNumbers;

/// <summary>
/// ViewModel for the Flash Numbers exercise page.
/// Manages a Flash Numbers session using the MemoApp.Core game logic.
/// </summary>
[QueryProperty(nameof(RangeStart), "rangeStart")]
[QueryProperty(nameof(RangeEnd), "rangeEnd")]
[QueryProperty(nameof(PreserveState), "preserveState")]
public partial class FlashNumbersViewModel : BaseViewModel
{
    private readonly ILocalizationService _localizationService;
    private readonly IAnalyticsService _analyticsService;
    private readonly IShareService _shareService;
    private readonly IInterstitialAdManager? _interstitialAdManager;
    private readonly IExerciseCountService _exerciseCountService;
    private readonly IAssociationComposerService _associationComposerService;
    private readonly IExceptionHandler _exceptionHandler;
    private GameSession? _gameSession;

    // Analytics tracking
    private DateTime _sessionStartTime;
    private int _totalAdvances;

    // Navigation state management
    private bool _hasBeenInitialized;

    [ObservableProperty]
    private bool preserveState;

    [ObservableProperty]
    private string rangeStart = "00";

    [ObservableProperty]
    private string rangeEnd = "09";

    public string CurrentRangeDisplay => $"{RangeStart}-{RangeEnd}";

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
    private ShareableResult? sessionShareData;

    [ObservableProperty]
    private ObservableCollection<NumberPerformance> performances = new();

    /// <summary>
    /// Gets the formatted completion time text with localized string
    /// </summary>
    public string CompletionTimeText
    {
        get
        {
            if (SessionStatistics?.TotalDuration == null)
                return string.Empty;

            var duration = SessionStatistics.Value.TotalDuration;
            string timeFormat;

            // Format time based on duration magnitude
            if (duration.TotalHours >= 1)
            {
                // Show hours:minutes:seconds if >= 1 hour
                timeFormat = duration.ToString(@"h\:mm\:ss");
            }
            else if (duration.TotalMinutes >= 1)
            {
                // Show minutes:seconds if >= 1 minute
                timeFormat = duration.ToString(@"m\:ss");
            }
            else
            {
                // Show only seconds with 1 decimal if < 1 minute
                timeFormat = $"{duration.TotalSeconds:F1}s";
            }

            // Get localized format string and apply the time
            var format = _localizationService.GetString("FlashNumbers_Complete");
            return string.Format(format, timeFormat);
        }
    }

    /// <summary>
    /// Gets the formatted time duration without localized text for two-line display
    /// </summary>
    public string FormattedTime
    {
        get
        {
            if (SessionStatistics?.TotalDuration == null)
                return string.Empty;

            var duration = SessionStatistics.Value.TotalDuration;

            // Format time based on duration magnitude
            if (duration.TotalHours >= 1)
            {
                // Show hours:minutes:seconds if >= 1 hour
                return duration.ToString(@"h\:mm\:ss");
            }
            else if (duration.TotalMinutes >= 1)
            {
                // Show minutes:seconds if >= 1 minute
                return duration.ToString(@"m\:ss");
            }
            else
            {
                // Show only seconds with 1 decimal if < 1 minute
                return $"{duration.TotalSeconds:F1}s";
            }
        }
    }

    [ObservableProperty]
    private bool isAssociationRevealed;

    [ObservableProperty]
    private TokenAssociation? currentAssociation;

    [ObservableProperty]
    private string associationRevealButtonText = "";

    [ObservableProperty]
    private string revealButtonIcon = "👁";

    // Track reveal usage for analytics
    private int _revealUsageCount;

    partial void OnSessionStatisticsChanged(SessionStatistics? value)
    {
        // Notify that CompletionTimeText and FormattedTime have changed when statistics update
        OnPropertyChanged(nameof(CompletionTimeText));
        OnPropertyChanged(nameof(FormattedTime));
    }

    [ObservableProperty]
    private string? currentAssociationWord;

    [ObservableProperty]
    private string? currentAssociationImagePath;


    public FlashNumbersViewModel(
        ILocalizationService localizationService,
        IAnalyticsService analyticsService,
        IShareService shareService,
        IExerciseCountService exerciseCountService,
        IAssociationComposerService associationComposerService,
        IExceptionHandler exceptionHandler,
        IInterstitialAdManager? interstitialAdManager = null)
    {
        _localizationService = localizationService ?? throw new ArgumentNullException(nameof(localizationService));
        _analyticsService = analyticsService ?? throw new ArgumentNullException(nameof(analyticsService));
        _shareService = shareService ?? throw new ArgumentNullException(nameof(shareService));
        _exerciseCountService = exerciseCountService ?? throw new ArgumentNullException(nameof(exerciseCountService));
        _associationComposerService = associationComposerService ?? throw new ArgumentNullException(nameof(associationComposerService));
        _exceptionHandler = exceptionHandler ?? throw new ArgumentNullException(nameof(exceptionHandler));
        _interstitialAdManager = interstitialAdManager;
        Title = "Flash Numbers";
    }

    /// <summary>
    /// Called when the page appears. Initializes the training session only if needed.
    /// </summary>
    public async Task InitializeAsync()
    {
        // If we're returning from navigation and session is completed, preserve state
        if (PreserveState && IsSessionCompleted)
        {
            PreserveState = false; // Reset flag
            return;
        }

        // If already initialized and session is active or completed, don't reinitialize
        if (_hasBeenInitialized && (IsSessionActive || IsSessionCompleted))
        {
            return;
        }

        try
        {
            IsBusy = true;
            Title = _localizationService.GetString("FlashNumbers_Title");

            // Prepare interstitial ad for potential showing
            if (_interstitialAdManager != null)
            {
                await _interstitialAdManager.PrepareAdForExerciseAsync(ExerciseTypes.FlashNumbers);
            }

            _gameSession = new GameSession(RangeStart, RangeEnd);
            TotalNumbers = _gameSession.TotalNumbers;

            _hasBeenInitialized = true;
            StartSession();
        }
        catch (Exception ex)
        {
            await _exceptionHandler.HandleExceptionAsync(ex, "flash_numbers_initialize",
                new Dictionary<string, object>
                {
                    { "range_start", RangeStart },
                    { "range_end", RangeEnd },
                    { "preserve_state", PreserveState }
                });
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
        
        // Reset association reveal when advancing to next number
        IsAssociationRevealed = false;
        CurrentAssociation = null;
        CurrentAssociationWord = null;
        CurrentAssociationImagePath = null;
    }

    [RelayCommand]
    private async void ToggleAssociationReveal()
    {
        if (_gameSession?.CurrentNumber == null || !IsSessionActive) return;

        try
        {
            if (IsAssociationRevealed)
            {
                // Hide the association
                IsAssociationRevealed = false;
                CurrentAssociation = null;
                CurrentAssociationWord = null;
                CurrentAssociationImagePath = null;
                
                // Track hiding association
                await _analyticsService.TrackEventAsync(AnalyticsEvents.FlashNumbersAssociationHidden, new Dictionary<string, object>
                {
                    [AnalyticsParameters.ExerciseType] = ExerciseTypes.FlashNumbers,
                    [AnalyticsParameters.Token] = CurrentNumber,
                    [AnalyticsParameters.NumberRange] = $"{RangeStart}_{RangeEnd}"
                });
            }
            else
            {
                // Load and show the association
                await LoadAssociationForCurrentNumber();
                
                // Track revealing association
                await _analyticsService.TrackEventAsync(AnalyticsEvents.FlashNumbersAssociationRevealed, new Dictionary<string, object>
                {
                    [AnalyticsParameters.ExerciseType] = ExerciseTypes.FlashNumbers,
                    [AnalyticsParameters.Token] = CurrentNumber,
                    [AnalyticsParameters.NumberRange] = $"{RangeStart}_{RangeEnd}",
                    [AnalyticsParameters.HasAssociation] = CurrentAssociation != null
                });
            }
        }
        catch (Exception ex)
        {
            await _exceptionHandler.HandleExceptionAsync(ex, "flash_numbers_association_reveal",
                new Dictionary<string, object>
                {
                    { "current_number", CurrentNumber },
                    { "is_revealing", !IsAssociationRevealed },
                    { "range", $"{RangeStart}_{RangeEnd}" }
                }, showUserMessage: false);
        }
    }

    /// <summary>
    /// Loads the association for the current number from the pack associations service
    /// </summary>
    private async Task LoadAssociationForCurrentNumber()
    {
        if (_gameSession?.CurrentNumber == null) return;

        try
        {
            var currentNumber = _gameSession.CurrentNumber.Value;
            var token = currentNumber.Display; // Get the display format (e.g., "07" for zero-prefixed)
            
            // Use "numbers" category like other parts of the app (BigNumberViewModel, AssociationsViewModel)
            var categoryKey = "numbers";
            var numberFormat = _localizationService.CurrentNumberFormat;

            // Get complete associations (pack + custom) with proper default resolution
            var associations = await _associationComposerService.GetCompleteTokenAssociationsAsync(categoryKey, token, numberFormat);

            if (associations?.Count > 0)
            {
                // Prefer default association, but use first available if no default
                // This now includes custom associations if user has set them
                var association = associations.FirstOrDefault(a => a.IsDefault) ?? associations.First();
                
                CurrentAssociation = association;
                CurrentAssociationWord = association.Word;
                // Use ThumbnailPath first, fallback to ImagePath if thumbnail not available
                CurrentAssociationImagePath = !string.IsNullOrEmpty(association.ThumbnailPath) 
                    ? association.ThumbnailPath 
                    : association.ImagePath;
                IsAssociationRevealed = true;
            }
            else
            {
                // No association found - still set as revealed to show "no association" message
                CurrentAssociation = null;
                CurrentAssociationWord = null;
                CurrentAssociationImagePath = null;
                IsAssociationRevealed = true;
            }
        }
        catch (Exception ex)
        {
            await _exceptionHandler.HandleExceptionAsync(ex, "flash_numbers_load_association",
                new Dictionary<string, object>
                {
                    { "token", _gameSession.CurrentNumber?.Display ?? "unknown" },
                    { "current_number", CurrentNumber },
                    { "category_key", "numbers" }
                }, showUserMessage: false);

            // Set as revealed but with no data to show error state
            IsAssociationRevealed = true;
            CurrentAssociation = null;
            CurrentAssociationWord = null;
            CurrentAssociationImagePath = null;
        }
    }

    [RelayCommand]
    private async Task ShowStatistics()
    {
        if (SessionStatistics == null) return;

        // Navigate with proper parameters using JSON serialization
        var statisticsJson = System.Text.Json.JsonSerializer.Serialize(SessionStatistics.Value);
        var encodedJson = System.Web.HttpUtility.UrlEncode(statisticsJson);

        await Shell.Current.GoToAsync($"statistics?statisticsJson={encodedJson}&returnToSummary=true");
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

        // Reset state flags when leaving
        _hasBeenInitialized = false;
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
        Title = "Results";

        // Create shareable result for the ShareButtonComponent
        var averageTime = TotalNumbers > 0 ? totalDuration / TotalNumbers : 0;
        var fastestTime = averageTime; // For now, use average as fastest time
        SessionShareData = new FlashNumbersShareResult
        {
            ExerciseType = "FlashNumbers",
            Duration = TimeSpan.FromSeconds(totalDuration),
            TotalNumbers = TotalNumbers,
            NumberRange = $"{RangeStart}-{RangeEnd}",
            AverageTimePerNumber = TimeSpan.FromSeconds(averageTime),
            FastestTime = TimeSpan.FromSeconds(fastestTime)
        };
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
                    _localizationService.GetString("Error_NoResultsToShare"), 
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
            await _exceptionHandler.HandleExceptionAsync(ex, "flash_numbers_share_results",
                new Dictionary<string, object>
                {
                    { "exercise_type", "FlashNumbers" },
                    { "total_numbers", TotalNumbers },
                    { "range", $"{RangeStart}_{RangeEnd}" },
                    { "has_statistics", SessionStatistics.HasValue }
                });
        }
    }

    partial void OnRangeStartChanged(string value)
    {
        OnPropertyChanged(nameof(CurrentRangeDisplay));
    }

    partial void OnRangeEndChanged(string value)
    {
        OnPropertyChanged(nameof(CurrentRangeDisplay));
    }
}