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

    /// <summary>
    /// True when in ready state (before session starts)
    /// </summary>
    public bool IsReadyState => !IsSessionActive && !IsSessionCompleted;

    /// <summary>
    /// Gets the primary action button text based on current state
    /// </summary>
    public string PrimaryActionButtonText => IsReadyState
        ? _localizationService.GetString("FlashNumbers_StartButton")
        : _localizationService.GetString("FlashNumbers_NextNumberArrow");

    /// <summary>
    /// Gets the display value for the prominent number area
    /// Shows range in ready state, current number in active state
    /// </summary>
    public string DisplayValue => IsReadyState ? CurrentRangeDisplay : (CurrentNumber ?? "");

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

    partial void OnIsSessionActiveChanged(bool value)
    {
        // Notify computed properties that depend on session state
        OnPropertyChanged(nameof(IsReadyState));
        OnPropertyChanged(nameof(PrimaryActionButtonText));
        OnPropertyChanged(nameof(DisplayValue));
    }

    partial void OnIsSessionCompletedChanged(bool value)
    {
        // Notify computed properties that depend on session state
        OnPropertyChanged(nameof(IsReadyState));
        OnPropertyChanged(nameof(PrimaryActionButtonText));
        OnPropertyChanged(nameof(DisplayValue));
    }

    partial void OnCurrentNumberChanged(string value)
    {
        // Notify DisplayValue when current number changes
        OnPropertyChanged(nameof(DisplayValue));
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
        Title = _localizationService.GetString("FlashNumbers_Title");
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

            _gameSession = new GameSession(RangeStart, RangeEnd, _localizationService.CurrentNumberFormat);
            TotalNumbers = _gameSession.TotalNumbers;

            _hasBeenInitialized = true;

            // Set ready state (don't auto-start)
            IsSessionActive = false;
            IsSessionCompleted = false;

            // Notify UI that ready state properties are available
            OnPropertyChanged(nameof(IsReadyState));
            OnPropertyChanged(nameof(DisplayValue));
            OnPropertyChanged(nameof(PrimaryActionButtonText));
            OnPropertyChanged(nameof(CurrentRangeDisplay));
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
    private async Task StartSession()
    {
        if (_gameSession == null) return;

        try
        {
        // Track session start analytics
        _sessionStartTime = DateTime.UtcNow;
        _totalAdvances = 0;

        // Guarded: an unparseable range previously crashed the app outright.
        var rangeSize = int.TryParse(RangeEnd, out var rangeEndValue)
                     && int.TryParse(RangeStart, out var rangeStartValue)
            ? rangeEndValue - rangeStartValue + 1
            : TotalNumbers;

        await _analyticsService.TrackEventAsync(AnalyticsEvents.FlashNumbersStarted, new Dictionary<string, object>
        {
            [AnalyticsParameters.ExerciseType] = ExerciseTypes.FlashNumbers,
            [AnalyticsParameters.NumberRange] = $"{RangeStart}_{RangeEnd}",
            [AnalyticsParameters.DifficultyDigits] = rangeSize,
            [AnalyticsParameters.TokensAvailable] = TotalNumbers
        });

        // Increment started exercise count for ad frequency tracking when user starts training
        await _exerciseCountService.IncrementExerciseStartedCountAsync(ExerciseTypes.FlashNumbers);

        // Set session state FIRST before updating UI to avoid race conditions
        IsSessionActive = true;
        IsSessionCompleted = false;

        // Then start the game session and update display
        _gameSession.StartSession();
        UpdateSessionStatus();
        }
        catch (Exception ex)
        {
            await _exceptionHandler.HandleExceptionAsync(ex, "flash_numbers_start_session",
                new Dictionary<string, object>
                {
                    { "range_start", RangeStart },
                    { "range_end", RangeEnd }
                });
        }
    }

    [RelayCommand]
    private async Task AdvanceToNext()
    {
        if (_gameSession == null || !IsSessionActive) return;

        try
        {
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
            await CompleteSession();
        }
        
        // Reset association reveal when advancing to next number
        IsAssociationRevealed = false;
        CurrentAssociation = null;
        CurrentAssociationWord = null;
        CurrentAssociationImagePath = null;
        }
        catch (Exception ex)
        {
            await _exceptionHandler.HandleExceptionAsync(ex, "flash_numbers_advance_to_next",
                new Dictionary<string, object> { { "current_number", CurrentNumber ?? string.Empty } });
        }
    }

    [RelayCommand]
    private async Task ToggleAssociationReveal()
    {
        try
        {
            if (IsAssociationRevealed)
            {
                // Hide the association (or placeholder in ready state)
                IsAssociationRevealed = false;

                // Only clear association data if in active state
                if (IsSessionActive)
                {
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
            }
            else
            {
                // Show association or placeholder
                IsAssociationRevealed = true;

                // Only load actual association if in active state with a current number
                if (IsSessionActive && _gameSession?.CurrentNumber != null)
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

    private async Task CompleteSession()
    {
        if (_gameSession == null) return;

        var statistics = _gameSession.GetStatistics();

        // Close the re-entry window BEFORE awaiting anything. AdvanceToNext guards on
        // IsSessionActive; awaiting analytics first left that guard open long enough for
        // a second tap to re-enter a completed GameSession and throw.
        IsSessionActive = false;
        IsSessionCompleted = true;
        SessionStatistics = statistics;
        Title = _localizationService.GetString("FlashNumbers_Results");

        // Share data now comes from the session's own measurements rather than a second
        // ViewModel-side clock, and reports the real fastest response instead of the average.
        SessionShareData = new FlashNumbersShareResult
        {
            ExerciseType = "FlashNumbers",
            Duration = statistics.TotalDuration,
            TotalNumbers = TotalNumbers,
            NumberRange = $"{RangeStart}-{RangeEnd}",
            AverageTimePerNumber = statistics.AverageResponseTime,
            FastestTime = statistics.FastestResponse.ResponseTime
        };

        try
        {
            // Increment completed exercise count for comprehensive analytics tracking
            await _exerciseCountService.IncrementExerciseCompletedCountAsync(ExerciseTypes.FlashNumbers);

            // Track session completion analytics
            await _analyticsService.TrackEventAsync(AnalyticsEvents.FlashNumbersCompleted, new Dictionary<string, object>
            {
                [AnalyticsParameters.ExerciseType] = ExerciseTypes.FlashNumbers,
                [AnalyticsParameters.NumberRange] = $"{RangeStart}_{RangeEnd}",
                [AnalyticsParameters.DurationSeconds] = (int)statistics.TotalDuration.TotalSeconds,
                [AnalyticsParameters.Success] = true,
                [AnalyticsParameters.TokensAvailable] = TotalNumbers,
                [AnalyticsParameters.Attempts] = _totalAdvances,
                [AnalyticsParameters.ProgressPercentage] = 100
            });
        }
        catch (Exception ex)
        {
            await _exceptionHandler.HandleExceptionAsync(ex, "flash_numbers_complete_session",
                showUserMessage: false);
        }
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

            // Reuse the result captured at completion. Recomputing here measured from
            // session start to the moment Share was tapped, so time spent reading the
            // results screen was published as part of the session.
            var shareResult = SessionShareData as FlashNumbersShareResult ?? new FlashNumbersShareResult
            {
                ExerciseType = "FlashNumbers",
                Duration = statistics.TotalDuration,
                TotalNumbers = TotalNumbers,
                NumberRange = $"{RangeStart}-{RangeEnd}",
                AverageTimePerNumber = statistics.AverageResponseTime,
                FastestTime = statistics.FastestResponse.ResponseTime
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