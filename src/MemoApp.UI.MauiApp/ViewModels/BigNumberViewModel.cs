using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MemoApp.Core.BigNumber;
using MemoApp.Core.Services.Analytics;
using MemoApp.Core.Services.Sharing;
using MemoApp.Core.Models;
using MemoApp.Core.MajorSystem.Services;
using MemoApp.Core.MajorSystem.Models;
using MemoApp.Localization.Services;
using MemoApp.UI.MauiApp.Constants;
using MemoApp.UI.MauiApp.Services;
using MemoApp.UI.MauiApp.Services.Freemium;
using System.Collections.ObjectModel;
using Microsoft.Maui.Graphics;
using System.Globalization;
using System.Text;

namespace MemoApp.UI.MauiApp.ViewModels;

public partial class BigNumberViewModel : BaseViewModel
{
    private readonly IBigNumberService _gameService;
    private readonly IBigNumberSettingsService _settingsService;
    private readonly ILocalizationService _localizationService;
    private readonly IShareService _shareService;
    private readonly ITextMeasurementService _textMeasurementService;
    private readonly IContainerSizeService _containerSizeService;
    private readonly IAnalyticsService _analyticsService;
    private readonly IInterstitialAdManager? _interstitialAdManager;
    private readonly IExerciseCountService _exerciseCountService;
    private readonly IPackAssociationsService _packAssociationsService;
    private readonly IFirstTimeTooltipService _tooltipService;
    private readonly IHapticFeedbackService? _hapticFeedbackService;
    private readonly IUserNotificationService _userNotificationService;

    // Confirmation dialog thresholds (based on UX agent recommendations)
    private const int MOBILE_RECALL_THRESHOLD_SECONDS = 5;
    private const int DESKTOP_RECALL_THRESHOLD_SECONDS = 15;
    private readonly IDigitComparisonService _digitComparisonService;
    private IDispatcherTimer? _timer;
    
    // Event for auto-scrolling to verification results
    public event EventHandler? ScrollToVerificationResults;
    public event EventHandler? FocusUserInputRequested;
    public event EventHandler<int>? ScrollToPositionRequested;
    public event EventHandler? ScrollToLegendRequested;
    
    private BigNumberGame _currentGame = new();
    private string _originalNumber = string.Empty;
    
    // Analytics tracking
    private DateTime _exerciseStartTime;
    private DateTime _memorizationStartTime;
    private DateTime _recallStartTime;

    [ObservableProperty]
    private int numberOfDigits = 20;

    /// <summary>
    /// Dynamic minimum height for the input Editor based on expected number length
    /// Optimizes mobile screen space usage for short sequences while maintaining usability
    /// </summary>
    [ObservableProperty]
    private double dynamicMinimumHeight = 50;

    [ObservableProperty]
    private int maxPairValue = 99;

    [ObservableProperty]
    private bool showSeparated = false;

    [ObservableProperty]
    private bool showTimer = false;

    [ObservableProperty]
    private FontSizePreference fontSizePreference = FontSizePreference.Auto;

    [ObservableProperty]
    private bool isContinuousSelected = true;

    [ObservableProperty]
    private bool isSeparatedSelected = false;

    [ObservableProperty]
    private GamePhase currentPhase = GamePhase.Setup;

    [ObservableProperty]
    private string timerText = "00:00";

    [ObservableProperty]
    private bool isTimerVisible = false;

    [ObservableProperty]
    private string displayedNumber = string.Empty;

    [ObservableProperty]
    private bool isNumberVisible = true;

    [ObservableProperty]
    private bool isSetupSectionVisible = true;

    [ObservableProperty]
    private bool isGameSectionVisible = false;

    [ObservableProperty]
    private bool isStopButtonVisible = false;

    [ObservableProperty]
    private bool isRecallButtonVisible = false;

    [ObservableProperty]
    private double numberFontSize = 32;


    [ObservableProperty]
    private bool isAssociationsVisible = false;

    [ObservableProperty]
    private ObservableCollection<TokenAssociation> numberAssociations = new();

    private List<TokenAssociation> _allAssociations = new();
    private const int ASSOCIATIONS_BATCH_SIZE = 20;

    [ObservableProperty]
    private bool isRecallPhase = false;

    [ObservableProperty]
    private string associationsToggleText = "Show";

    [ObservableProperty]
    private Color associationsToggleColor = Colors.Green;

    [ObservableProperty]
    private bool canShareResults = false;

    [ObservableProperty]
    private BigNumberShareResult? shareData = null;

    // Input Verification Properties
    [ObservableProperty]
    private bool enableInputVerification = false;

    [ObservableProperty]
    private bool isInputVerificationVisible = false;

    [ObservableProperty]
    private bool showOnDemandVerifyButton = false;

    [ObservableProperty]
    private bool isOnDemandVerificationActive = false;

    [ObservableProperty]
    private bool isVerificationPanelManuallyClosed = false;

    [ObservableProperty]
    private string userInput = string.Empty;

    [ObservableProperty]
    private double inputProgress = 0.0;

    [ObservableProperty]
    private string inputProgressText = string.Empty;

    [ObservableProperty]
    private bool canVerifyInput = false;

    [ObservableProperty]
    private bool isVerificationProcessing = false;

    [ObservableProperty]
    private string verificationButtonText = string.Empty;

    [ObservableProperty]
    private Color verificationButtonBackgroundColor = Colors.Green;

    [ObservableProperty]
    private bool showVerificationResults = false;


    [ObservableProperty]
    private string accuracyTitle = string.Empty;

    [ObservableProperty]
    private string accuracyPercentageText = string.Empty;

    [ObservableProperty]
    private double accuracyPercentage = 0;

    [ObservableProperty]
    private Color accuracyColor = Colors.Black;

    [ObservableProperty]
    private Color accuracyBorderColor = Colors.Gray;

    [ObservableProperty]
    private string accuracyIcon = string.Empty;

    [ObservableProperty]
    private int correctDigitsCount = 0;

    // Digit Comparison Properties
    [ObservableProperty]
    private bool showDigitComparison = false;

    [ObservableProperty]
    private bool isDigitComparisonExpanded = false;

    [ObservableProperty]
    private string comparisonToggleText = string.Empty;

    [ObservableProperty]
    private ObservableCollection<string> originalNumberDigits = new();

    [ObservableProperty]
    private ObservableCollection<DigitComparisonTileViewModel> comparisonResults = new();

    private NumberComparisonResult? _currentComparisonResult;

    // New UI Architecture Properties
    
    // Mode Selection Properties
    [ObservableProperty]
    private bool isManualVerificationSelected = false;

    [ObservableProperty]
    private bool isAppVerificationSelected = true;

    [ObservableProperty]
    private VerificationModePreference verificationMode = VerificationModePreference.InApp;
    
    // Legacy properties for backward compatibility
    public bool IsTraditionalModeSelected => IsManualVerificationSelected;
    public bool IsGuidedModeSelected => IsAppVerificationSelected;
    
    [ObservableProperty]
    private Color manualVerificationBackgroundColor = Colors.Transparent;
    
    [ObservableProperty]
    private Color manualVerificationBorderColor = Colors.Gray;
    
    [ObservableProperty]
    private Color manualVerificationTextColor = Colors.Black;
    
    [ObservableProperty]
    private Color manualVerificationSubtextColor = Colors.Gray;
    
    [ObservableProperty]
    private Color appVerificationBackgroundColor = Colors.Transparent;
    
    [ObservableProperty]
    private Color appVerificationBorderColor = Colors.Gray;
    
    [ObservableProperty]
    private Color appVerificationTextColor = Colors.Black;
    
    [ObservableProperty]
    private Color appVerificationSubtextColor = Colors.Gray;
    
    // Legacy properties for backward compatibility
    public Color TraditionalModeBackgroundColor => ManualVerificationBackgroundColor;
    public Color TraditionalModeBorderColor => ManualVerificationBorderColor;
    public Color TraditionalModeTextColor => ManualVerificationTextColor;
    public Color TraditionalModeSubtextColor => ManualVerificationSubtextColor;
    public Color GuidedModeBackgroundColor => AppVerificationBackgroundColor;
    public Color GuidedModeBorderColor => AppVerificationBorderColor;
    public Color GuidedModeTextColor => AppVerificationTextColor;
    public Color GuidedModeSubtextColor => AppVerificationSubtextColor;
    
    // Progressive Disclosure Properties
    [ObservableProperty]
    private bool showAdvancedSettings = true;
    
    [ObservableProperty]
    private bool showAdvancedSettingsToggle = false;
    
    [ObservableProperty]
    private bool isAdvancedSettingsExpanded = false;
    
    [ObservableProperty]
    private string advancedSettingsToggleText = "Show";
    
    // Context-Aware Action Properties
    [ObservableProperty]
    private string primaryActionText = "Start";
    
    [ObservableProperty]
    private Color primaryActionBackgroundColor = Colors.Blue;
    
    [ObservableProperty]
    private bool showPrimaryAction = true;
    
    [ObservableProperty]
    private string secondaryActionText = "Stop";
    
    [ObservableProperty]
    private Color secondaryActionBackgroundColor = Colors.Orange;
    
    [ObservableProperty]
    private bool showSecondaryAction = false;
    
    [ObservableProperty]
    private bool isGuidedPracticeInputVisible = false;
    
    [ObservableProperty]
    private bool showSessionActions = false;
    
    // Dynamic Display Visibility Properties
    [ObservableProperty]
    private bool isNumberDisplayVisible = true;
    
    [ObservableProperty]
    private bool isFontControlsVisible = true;
    
    [ObservableProperty]
    private bool isManualVerificationGuidanceVisible = false;
    
    // Wordle-style Digit Comparison Properties
    [ObservableProperty]
    private ObservableCollection<DigitComparisonTileViewModel> originalNumberTiles = new();
    
    [ObservableProperty]
    private ObservableCollection<DigitComparisonTileViewModel> userAnswerTiles = new();
    
    // Navigation Enhancement Properties
    [ObservableProperty]
    private bool showComparisonNavigation = false;
    
    [ObservableProperty]
    private bool showComparisonLegend = false;
    
    [ObservableProperty]
    private string comparisonPositionText = string.Empty;
    
    [ObservableProperty]
    private string comparisonSummaryText = string.Empty;
    
    [ObservableProperty]
    private bool canNavigateLeft = false;
    
    [ObservableProperty]
    private bool canNavigateRight = false;
    
    [ObservableProperty]
    private string dynamicPositionText = string.Empty;

    // Legend Visibility Properties
    [ObservableProperty]
    private bool isLegendVisible = false;

    [ObservableProperty]
    private string legendToggleText = string.Empty;

    private NumberComparisonResult? _comparisonResult;
    private int _currentScrollPosition = 0;
    private const int DIGITS_PER_CHUNK = 20;

    public string CurrentFormatExample => GenerateExampleNumber(ShowSeparated);

    /// <summary>
    /// Determines if the associations frame should be visible
    /// Always shows during recall phase (manual verification) or after game completion (app verification)
    /// Starts collapsed by default to prioritize results and actions
    /// </summary>
    public bool ShouldShowAssociationsFrame =>
        IsRecallPhase ||
        (CurrentPhase == GamePhase.Stopped && ShowVerificationResults && (EnableInputVerification || IsOnDemandVerificationActive));

    public BigNumberViewModel(
        IBigNumberService gameService,
        IBigNumberSettingsService settingsService,
        ILocalizationService localizationService,
        IShareService shareService,
        ITextMeasurementService textMeasurementService,
        IContainerSizeService containerSizeService,
        IAnalyticsService analyticsService,
        IExerciseCountService exerciseCountService,
        IPackAssociationsService packAssociationsService,
        IUserNotificationService userNotificationService,
        IDigitComparisonService digitComparisonService,
        IInterstitialAdManager? interstitialAdManager = null,
        IHapticFeedbackService? hapticFeedbackService = null)
    {
        _gameService = gameService ?? throw new ArgumentNullException(nameof(gameService));
        _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
        _localizationService = localizationService ?? throw new ArgumentNullException(nameof(localizationService));
        _shareService = shareService ?? throw new ArgumentNullException(nameof(shareService));
        _textMeasurementService = textMeasurementService ?? throw new ArgumentNullException(nameof(textMeasurementService));
        _containerSizeService = containerSizeService ?? throw new ArgumentNullException(nameof(containerSizeService));
        _analyticsService = analyticsService ?? throw new ArgumentNullException(nameof(analyticsService));
        _exerciseCountService = exerciseCountService ?? throw new ArgumentNullException(nameof(exerciseCountService));
        _packAssociationsService = packAssociationsService ?? throw new ArgumentNullException(nameof(packAssociationsService));
        _userNotificationService = userNotificationService ?? throw new ArgumentNullException(nameof(userNotificationService));
        _digitComparisonService = digitComparisonService ?? throw new ArgumentNullException(nameof(digitComparisonService));
        _interstitialAdManager = interstitialAdManager;
        _hapticFeedbackService = hapticFeedbackService;
        
        Title = _localizationService.GetString("BigNumber_Title");
        
        // Initialize timer
        _timer = Application.Current?.Dispatcher.CreateTimer();
        if (_timer != null)
        {
            _timer.Interval = TimeSpan.FromSeconds(1);
            _timer.Tick += OnTimerTick;
        }
    }

    public async Task InitializeAsync()
    {
        // Load settings
        var settings = await _settingsService.LoadSettingsAsync();
        NumberOfDigits = settings.NumberOfDigits;
        MaxPairValue = settings.MaxPairValue;
        ShowSeparated = settings.ShowSeparated;
        ShowTimer = settings.ShowTimer;
        FontSizePreference = settings.FontSize;
        EnableInputVerification = settings.EnableInputVerification;
        VerificationMode = settings.VerificationMode;

        // Ensure boolean flags are aligned with loaded verification mode
        IsManualVerificationSelected = settings.VerificationMode == VerificationModePreference.Manual;
        IsAppVerificationSelected = settings.VerificationMode == VerificationModePreference.InApp;

        IsContinuousSelected = !settings.ShowSeparated;
        IsSeparatedSelected = settings.ShowSeparated;
        
        // Initialize new UI architecture
        UpdateModeColors();
        UpdateContextActions();
        
        // Initialize advanced settings state - start collapsed for mobile
        ShowAdvancedSettings = false;
        ShowAdvancedSettingsToggle = true;
        IsAdvancedSettingsExpanded = false;
        AdvancedSettingsToggleText = _localizationService.GetString("BigNumber_Show");

        // Initialize verification button state
        SetVerificationNormalState();

        UpdateNumberFontSize();
        
        // Initialize dynamic minimum height based on current settings
        UpdateDynamicMinimumHeight();
        
        // Initialize InputProgressText to always be visible (prevents layout shift)
        InputProgressText = string.Format(
            CultureInfo.CurrentCulture,
            _localizationService.GetString("BigNumber_DigitCount"),
            0,
            NumberOfDigits);

        // Initialize verification button text
        InitializeVerificationButtonState();
    }
    

    partial void OnNumberOfDigitsChanged(int value)
    {
        // Clamp value
        if (value < 1) NumberOfDigits = 1;
        else if (value > 500) NumberOfDigits = 500;
        
        // Update command states
        DecrementNumberOfDigitsCommand.NotifyCanExecuteChanged();
        IncrementNumberOfDigitsCommand.NotifyCanExecuteChanged();
        
        // Update examples
        UpdateExamples();
        
        // Update dynamic minimum height based on expected content length
        UpdateDynamicMinimumHeight();
        
        // Update InputProgressText with new target digit count
        var currentDigits = UserInput?.Where(char.IsDigit).Count() ?? 0;
        InputProgressText = string.Format(
            CultureInfo.CurrentCulture,
            _localizationService.GetString("BigNumber_DigitCount"),
            currentDigits,
            NumberOfDigits);
        
        _ = SaveSettingsAsync();
    }

    partial void OnMaxPairValueChanged(int value)
    {
        // Clamp value
        if (value < 10) MaxPairValue = 10;
        else if (value > 99) MaxPairValue = 99;
        
        // Update command states
        DecrementMaxPairValueCommand.NotifyCanExecuteChanged();
        IncrementMaxPairValueCommand.NotifyCanExecuteChanged();
        
        // Update examples
        UpdateExamples();
        
        _ = SaveSettingsAsync();
    }

    partial void OnShowSeparatedChanged(bool value)
    {
        _ = SaveSettingsAsync();
        UpdateDisplayedNumber();
        
        // Update example when format changes
        OnPropertyChanged(nameof(CurrentFormatExample));
    }

    partial void OnShowTimerChanged(bool value)
    {
        _ = SaveSettingsAsync();
        // Update timer visibility: during memorizing respect setting, when stopped always show
        IsTimerVisible = (value && CurrentPhase == GamePhase.Memorizing) || CurrentPhase == GamePhase.Stopped;
    }


    partial void OnFontSizePreferenceChanged(FontSizePreference value)
    {
        // If we have a number displayed, adjust font size for even digits per line
        if (!string.IsNullOrEmpty(DisplayedNumber))
        {
            AdjustFontSizeForEvenLineBreaks();
        }
        else
        {
            UpdateNumberFontSize();
        }
        _ = SaveSettingsAsync();
    }

    partial void OnIsContinuousSelectedChanged(bool value)
    {
        if (value)
        {
            ShowSeparated = false;
            IsSeparatedSelected = false;
        }
    }

    partial void OnIsSeparatedSelectedChanged(bool value)
    {
        if (value)
        {
            ShowSeparated = true;
            IsContinuousSelected = false;
        }
    }

    partial void OnCurrentPhaseChanged(GamePhase value)
    {
        UpdateUIState();
        UpdateContextActions();
    }

    partial void OnEnableInputVerificationChanged(bool value)
    {
        _ = SaveSettingsAsync();
        UpdateUIState();
        OnPropertyChanged(nameof(ShouldShowAssociationsFrame)); // Notify associations frame visibility change

        if (!value)
        {
            // Clear input verification state when disabled
            ClearInputVerificationState();
        }
    }

    partial void OnVerificationModeChanged(VerificationModePreference value)
    {
        // Update the UI states based on the selected mode
        IsManualVerificationSelected = value == VerificationModePreference.Manual;
        IsAppVerificationSelected = value == VerificationModePreference.InApp;

        // For App Verification, ensure input verification is enabled
        if (value == VerificationModePreference.InApp)
        {
            EnableInputVerification = true;
        }

        _ = SaveSettingsAsync();
        UpdateModeColors();
        UpdateUIState();
    }

    partial void OnIsOnDemandVerificationActiveChanged(bool value)
    {
        OnPropertyChanged(nameof(ShouldShowAssociationsFrame)); // Notify associations frame visibility change
    }

    partial void OnShowVerificationResultsChanged(bool value)
    {
        OnPropertyChanged(nameof(ShouldShowAssociationsFrame)); // Notify associations frame visibility change
    }

    partial void OnUserInputChanged(string value)
    {
        // Enforce digits-only input and max length for Editor
        if (!string.IsNullOrEmpty(value))
        {
            // Extract only digits from input (removes any non-digit characters)
            var digitsOnly = new string(value.Where(char.IsDigit).ToArray());
            
            // If input contains non-digit characters, replace with digits-only version
            if (digitsOnly != value)
            {
                UserInput = digitsOnly.Length > NumberOfDigits 
                    ? digitsOnly.Substring(0, NumberOfDigits)
                    : digitsOnly;
                return; // Prevent recursive call
            }
            
            // If input exceeds digit limit, truncate to limit
            if (digitsOnly.Length > NumberOfDigits)
            {
                UserInput = digitsOnly.Substring(0, NumberOfDigits);
                return; // Prevent recursive call
            }
        }
        
        // Update digit count and progress
        var digitsEntered = value?.Where(char.IsDigit).Count() ?? 0;
        
        
        // Update progress
        InputProgress = NumberOfDigits > 0 ? (double)digitsEntered / NumberOfDigits : 0;
        InputProgressText = string.Format(
            CultureInfo.CurrentCulture,
            _localizationService.GetString("BigNumber_InputProgress"),
            digitsEntered,
            NumberOfDigits);
        
        // Update verification button state
        CanVerifyInput = digitsEntered > 0 && !IsVerificationProcessing;
        VerifyInputCommand.NotifyCanExecuteChanged();
    }

    /// <summary>
    /// Sets the container element reference for accurate size measurement
    /// </summary>
    /// <param name="containerElement">The container element (Frame or Label)</param>
    public void SetContainerElement(VisualElement containerElement)
    {
        _containerSizeService.SetContainerElement(containerElement);
    }

    [RelayCommand]
    private async Task StartMemorizing()
    {
        // Prepare ad for potential display
        if (_interstitialAdManager != null)
        {
            await _interstitialAdManager.PrepareAdForExerciseAsync(ExerciseTypes.BigNumber);
        }
        
        // Track analytics for exercise start
        _exerciseStartTime = DateTime.UtcNow;
        _memorizationStartTime = _exerciseStartTime;
        
        await _analyticsService.TrackEventAsync(AnalyticsEvents.BigNumberStarted, new Dictionary<string, object>
        {
            [AnalyticsParameters.DifficultyDigits] = NumberOfDigits,
            [AnalyticsParameters.NumberRange] = MaxPairValue == 99 ? "0_99" : "00_99",
            [AnalyticsParameters.PhaseType] = PhaseTypes.Memorization
        });

        // Create new game with current settings
        var settings = new BigNumberSettings
        {
            NumberOfDigits = NumberOfDigits,
            MaxPairValue = MaxPairValue,
            ShowSeparated = ShowSeparated,
            ShowTimer = ShowTimer,
            FontSize = FontSizePreference
        };

        _currentGame = _gameService.CreateNewGame(settings);
        _originalNumber = _currentGame.GeneratedNumber;
        _gameService.StartGame(_currentGame);
        
        CurrentPhase = _currentGame.Phase;
        
        // Start timer and immediately update display to show 00:00
        _timer?.Start();
        UpdateTimerDisplay();
    }

    [RelayCommand]
    private async Task StopAndHide()
    {
        // Track exercise abandonment
        var currentTime = DateTime.UtcNow;
        var totalDuration = (currentTime - _exerciseStartTime).TotalSeconds;
        var currentPhaseType = _currentGame.IsNumberVisible ? PhaseTypes.Memorization : PhaseTypes.Recall;
        
        await _analyticsService.TrackEventAsync(AnalyticsEvents.ExerciseAbandoned, new Dictionary<string, object>
        {
            [AnalyticsParameters.ExerciseType] = ExerciseTypes.BigNumber,
            [AnalyticsParameters.DurationSeconds] = (int)totalDuration,
            [AnalyticsParameters.AbandonmentPoint] = currentPhaseType,
            [AnalyticsParameters.AbandonmentReason] = AbandonmentReasons.UserQuit,
            [AnalyticsParameters.DifficultyDigits] = NumberOfDigits
        });
        
        _gameService.StopGame(_currentGame);
        CurrentPhase = _currentGame.Phase;
        
        // Stop timer
        _timer?.Stop();
        UpdateTimerDisplay();
        
        // In app verification mode, automatically activate input verification after memorization
        if (IsAppVerificationSelected && CurrentPhase == GamePhase.Stopped)
        {
            // Automatically show input verification panel for guided practice
            IsOnDemandVerificationActive = true;
            IsVerificationPanelManuallyClosed = false;
            UserInput = string.Empty; // Clear any previous input
            UpdateUIState();
            
            // Request focus on the input field (this will be handled by the view)
            RequestUserInputFocus();
        }
    }

    [RelayCommand]
    private async Task StartRecall()
    {
        if (_currentGame.Phase == GamePhase.Stopped)
        {
            // Start recall phase - show number and associations
            _recallStartTime = DateTime.UtcNow;
            var memorizationDuration = (_recallStartTime - _memorizationStartTime).TotalSeconds;
            
            _gameService.StartRecall(_currentGame);
            // Show the number during recall phase
            _currentGame.IsNumberVisible = true;
            CurrentPhase = _currentGame.Phase;
            
            // Initialize associations collapsed by default for optimal UX
            IsAssociationsVisible = false;
            AssociationsToggleText = _localizationService.GetString("BigNumber_ShowAssociations_Button");
            AssociationsToggleColor = Colors.Green;
            
            await _analyticsService.TrackEventAsync(AnalyticsEvents.NumberRecallStarted, new Dictionary<string, object>
            {
                [AnalyticsParameters.DifficultyDigits] = NumberOfDigits,
                [AnalyticsParameters.MemorizationTime] = (int)memorizationDuration,
                [AnalyticsParameters.PhaseType] = PhaseTypes.Recall
            });
            
            UpdateDisplayedNumber();
        }
    }

    [RelayCommand]
    private async Task StartNewGame()
    {
        // Smart confirmation logic - only show when there's risk of losing progress
        if (ShouldShowNewGameConfirmation())
        {
            var confirmed = await ShowNewGameConfirmationAsync();
            if (!confirmed)
            {
                return; // User cancelled
            }
        }
        // Track exercise completion if user finished a previous game
        if (_currentGame.Phase != GamePhase.Setup)
        {
            var currentTime = DateTime.UtcNow;
            var totalDuration = (currentTime - _exerciseStartTime).TotalSeconds;
            var wasInRecall = !_currentGame.IsNumberVisible;
            var recallDuration = wasInRecall && _recallStartTime != default 
                ? (currentTime - _recallStartTime).TotalSeconds 
                : 0;
            
            await _analyticsService.TrackEventAsync(AnalyticsEvents.BigNumberCompleted, new Dictionary<string, object>
            {
                [AnalyticsParameters.ExerciseType] = ExerciseTypes.BigNumber,
                [AnalyticsParameters.DurationSeconds] = (int)totalDuration,
                [AnalyticsParameters.Success] = true, // User completed the session (reached recall phase)
                [AnalyticsParameters.DifficultyDigits] = NumberOfDigits,
                [AnalyticsParameters.RecallTime] = (int)recallDuration,
                [AnalyticsParameters.NumberRange] = MaxPairValue == 99 ? "0_99" : "00_99"
            });
            
            // Show ad if needed and increment exercise count
            if (_interstitialAdManager != null)
            {
                await _interstitialAdManager.ShowAdIfNeededAsync(ExerciseTypes.BigNumber);
            }
            
            // Increment exercise count for ad frequency tracking
            await _exerciseCountService.IncrementExerciseCountAsync(ExerciseTypes.BigNumber);
        }
        
        _timer?.Stop();
        _gameService.ResetGame(_currentGame);
        CurrentPhase = _currentGame.Phase;
        TimerText = "00:00";
        
        // Reset transient UI state for consistent user experience
        ClearInputVerificationState();
        IsVerificationPanelManuallyClosed = false; // Reset for new game

        // Clear share data for new game
        ShareData = null;
        
        // Update UI state to reflect cleared verification state
        UpdateUIState();
    }

    private void UpdateUIState()
    {
        IsSetupSectionVisible = CurrentPhase == GamePhase.Setup;
        IsGameSectionVisible = CurrentPhase != GamePhase.Setup;
        IsStopButtonVisible = CurrentPhase == GamePhase.Memorizing;
        IsRecallButtonVisible = CurrentPhase == GamePhase.Stopped;
        IsRecallPhase = CurrentPhase == GamePhase.Recall;

        // Update associations frame visibility
        OnPropertyChanged(nameof(ShouldShowAssociationsFrame));

        // Enable sharing when memorization is completed (stopped or in recall)
        CanShareResults = CurrentPhase == GamePhase.Stopped || CurrentPhase == GamePhase.Recall;

        // Update share data when sharing becomes available
        if (CanShareResults)
        {
            UpdateShareData();
        }
        else
        {
            ShareData = null;
        }
        
        // Timer visibility logic:
        // - During memorizing: only show if ShowTimer setting is enabled
        // - When stopped or recall: ALWAYS show the final time regardless of setting
        IsTimerVisible = (ShowTimer && CurrentPhase == GamePhase.Memorizing) || CurrentPhase == GamePhase.Stopped || CurrentPhase == GamePhase.Recall;
        
        // Dynamic Display Visibility Logic
        // Number display should only be visible when:
        // 1. Number should be shown to user (memorizing or recall phases)
        // 2. Not in stopped phase (where number is hidden for recall)
        IsNumberDisplayVisible = IsGameSectionVisible && 
            (CurrentPhase == GamePhase.Memorizing || (CurrentPhase == GamePhase.Recall && _currentGame.IsNumberVisible));
        
        // Font controls only needed when number display is visible
        IsFontControlsVisible = IsNumberDisplayVisible;
        
        // Manual verification guidance: show in stopped phase for manual verification mode
        IsManualVerificationGuidanceVisible = IsManualVerificationSelected && CurrentPhase == GamePhase.Stopped;
        
        // Input verification visibility: show when (enabled OR on-demand active) AND NOT manually closed, and in stopped phase
        IsInputVerificationVisible = (EnableInputVerification || IsOnDemandVerificationActive) && !IsVerificationPanelManuallyClosed && CurrentPhase == GamePhase.Stopped;
        
        // On-demand verify button: Never show in app verification mode - input should be automatic
        // Only show in manual verification mode when verification is not visible but could be activated
        ShowOnDemandVerifyButton = IsManualVerificationSelected && 
            ((EnableInputVerification && IsVerificationPanelManuallyClosed) || 
             (!EnableInputVerification && !IsOnDemandVerificationActive)) && 
            CurrentPhase == GamePhase.Stopped;
        
        UpdateDisplayedNumber();
        UpdateAssociationsVisibility();
    }

    private void UpdateDisplayedNumber()
    {
        if (_currentGame.Phase == GamePhase.Setup || !_currentGame.IsNumberVisible)
        {
            DisplayedNumber = string.Empty;
        }
        else
        {
            var formattedNumber = _gameService.FormatNumber(_currentGame.GeneratedNumber, ShowSeparated);
            DisplayedNumber = formattedNumber;
            
            // Always adjust font size to ensure even digits per line
            AdjustFontSizeForEvenLineBreaks();
        }
        
        IsNumberVisible = !string.IsNullOrEmpty(DisplayedNumber);
        
        // Update associations when display changes
        UpdateAssociationsVisibility();
    }

    private void OnTimerTick(object? sender, EventArgs e)
    {
        if (_currentGame.IsTimerRunning && _currentGame.StartTime.HasValue)
        {
            _currentGame.ElapsedTime = DateTime.Now - _currentGame.StartTime.Value;
            UpdateTimerDisplay();
        }
    }

    private void UpdateTimerDisplay()
    {
        TimerText = _gameService.FormatTime(_currentGame.ElapsedTime);
    }

    private void UpdateNumberFontSize()
    {
        // This method now just sets the base font size
        // The adjustment for even digits per line is handled in AdjustFontSizeForEvenLineBreaks
        if (FontSizePreference == FontSizePreference.Auto)
        {
            NumberFontSize = CalculateAutoFontSize();
        }
        else
        {
            double baseFontSize = FontSizePreference switch
            {
                FontSizePreference.Small => 24,
                FontSizePreference.Medium => 32,
                FontSizePreference.Large => 42,
                FontSizePreference.ExtraLarge => 52,
                _ => 32
            };

            NumberFontSize = DeviceInfo.Idiom == DeviceIdiom.Phone ? baseFontSize :
                            DeviceInfo.Idiom == DeviceIdiom.Tablet ? baseFontSize * 1.5 :
                            baseFontSize * 2.0;
        }
    }

    private void AdjustFontSizeForEvenLineBreaks()
    {
        if (string.IsNullOrEmpty(DisplayedNumber))
        {
            // Set default font size when no number is displayed
            UpdateNumberFontSize();
            return;
        }

        // Get the base font size first
        UpdateNumberFontSize();
        double baseFontSize = NumberFontSize;

        // Get actual container width from the UI
        double containerWidth = _containerSizeService.GetNumberDisplayContainerWidth();

        double minFontSize = DeviceInfo.Idiom == DeviceIdiom.Phone ? 12 :
                            DeviceInfo.Idiom == DeviceIdiom.Tablet ? 16 : 20;
        double maxFontSize = DeviceInfo.Idiom == DeviceIdiom.Phone ? 60 :
                            DeviceInfo.Idiom == DeviceIdiom.Tablet ? 80 : 100;

        // Check if base size already works
        if (HasEvenDigitsPerLine(baseFontSize, containerWidth))
        {
            NumberFontSize = baseFontSize;
            return;
        }

        double? adjustedSize = null;

        if (FontSizePreference == FontSizePreference.Auto)
        {
            // For Auto: decrease by 1 until we get even digits per line
            adjustedSize = FindFontSizeWithEvenDigits(baseFontSize, containerWidth, -1, minFontSize, maxFontSize);
        }
        else
        {
            // For fixed sizes: try increasing first, then decreasing
            adjustedSize = FindFontSizeWithEvenDigits(baseFontSize, containerWidth, 1, minFontSize, maxFontSize)
                          ?? FindFontSizeWithEvenDigits(baseFontSize, containerWidth, -1, minFontSize, maxFontSize);
        }

        // Apply the adjusted size or fallback
        NumberFontSize = adjustedSize ?? (FontSizePreference == FontSizePreference.Auto ? minFontSize : baseFontSize);
    }

    private double? FindFontSizeWithEvenDigits(double startSize, double containerWidth, int increment, double minSize, double maxSize)
    {
        double currentSize = startSize + increment; // Skip the starting size since we already checked it
        
        while (currentSize >= minSize && currentSize <= maxSize)
        {
            if (HasEvenDigitsPerLine(currentSize, containerWidth))
            {
                return currentSize;
            }
            
            currentSize += increment;
        }
        
        return null; // No suitable size found
    }
    
    private bool HasEvenDigitsPerLine(double fontSize, double containerWidth)
    {
        // Use actual text measurement for accurate calculation
        var textSize = _textMeasurementService.MeasureText(DisplayedNumber, "Courier", (float)fontSize);
        
        // Check if the text fits in one line
        if (textSize.Width <= containerWidth)
        {
            // Fits on one line - acceptable regardless of even/odd
            return true;
        }
        
        // Calculate character width using actual measurement
        float charWidth = _textMeasurementService.GetCharacterWidth('0', "Courier", (float)fontSize);
        int maxCharsPerLine = (int)(containerWidth / charWidth);
        
        // For separated format, account for spaces
        if (ShowSeparated)
        {
            // Count actual digits (excluding spaces)
            string digitsOnly = DisplayedNumber.Replace(" ", "");
            int totalDigits = digitsOnly.Length;
            
            // Calculate how digits would be distributed
            return WouldHaveEvenDigitsPerLineSeparated(totalDigits, maxCharsPerLine);
        }
        else
        {
            // For continuous format, simple calculation
            int totalDigits = DisplayedNumber.Length;
            return WouldHaveEvenDigitsPerLineContinuous(totalDigits, maxCharsPerLine);
        }
    }
    
    private bool WouldHaveEvenDigitsPerLineContinuous(int totalDigits, int maxCharsPerLine)
    {
        if (totalDigits <= maxCharsPerLine)
        {
            // Fits on one line - acceptable regardless of even/odd
            return true;
        }
        
        // Check if all lines except the last would have even digits
        int fullLines = totalDigits / maxCharsPerLine;
        int remainder = totalDigits % maxCharsPerLine;
        
        // If maxCharsPerLine is odd, all full lines would have odd digits - not acceptable
        if (maxCharsPerLine % 2 != 0)
            return false;
            
        // All full lines would have even digits, which is good
        return true;
    }
    
    private bool WouldHaveEvenDigitsPerLineSeparated(int totalDigits, int maxCharsPerLine)
    {
        if (totalDigits <= maxCharsPerLine)
        {
            // Fits on one line - acceptable
            return true;
        }
        
        // For separated format, each pair takes 3 characters (e.g., "12 ")
        // except the last pair on each line doesn't need trailing space
        int pairsPerLine = maxCharsPerLine / 3;
        if (pairsPerLine == 0) return false; // Too narrow
        
        int digitsPerLine = pairsPerLine * 2; // Each pair = 2 digits
        
        // Check if digits per line is even (it should be since each pair = 2 digits)
        return digitsPerLine % 2 == 0;
    }

    private double CalculateAutoFontSize()
    {
        if (string.IsNullOrEmpty(DisplayedNumber))
        {
            // Default size when no number is displayed
            return DeviceInfo.Idiom == DeviceIdiom.Phone ? 32 :
                   DeviceInfo.Idiom == DeviceIdiom.Tablet ? 48 : 64;
        }

        // Get actual container width
        double containerWidth = _containerSizeService.GetNumberDisplayContainerWidth();

        // Define font size limits
        double minFontSize = DeviceInfo.Idiom == DeviceIdiom.Phone ? 16 :
                            DeviceInfo.Idiom == DeviceIdiom.Tablet ? 20 : 24;
        double maxFontSize = DeviceInfo.Idiom == DeviceIdiom.Phone ? 48 :
                            DeviceInfo.Idiom == DeviceIdiom.Tablet ? 64 : 80;

        // Binary search for the optimal font size that fits the container
        double low = minFontSize;
        double high = maxFontSize;
        double bestSize = minFontSize;

        while (high - low > 1)
        {
            double mid = (low + high) / 2;
            var textSize = _textMeasurementService.MeasureText(DisplayedNumber, "Courier", (float)mid);
            
            if (textSize.Width <= containerWidth)
            {
                bestSize = mid;
                low = mid;
            }
            else
            {
                high = mid;
            }
        }
        
        return bestSize;
    }

    [RelayCommand]
    private void SetFontSize(string sizeString)
    {
        if (Enum.TryParse<FontSizePreference>(sizeString, out var fontSize))
        {
            FontSizePreference = fontSize;
        }
    }

    [RelayCommand(CanExecute = nameof(CanDecrementNumberOfDigits))]
    private void DecrementNumberOfDigits()
    {
        NumberOfDigits--;
    }

    [RelayCommand(CanExecute = nameof(CanIncrementNumberOfDigits))]
    private void IncrementNumberOfDigits()
    {
        NumberOfDigits++;
    }

    private bool CanDecrementNumberOfDigits() => NumberOfDigits > 1;
    private bool CanIncrementNumberOfDigits() => NumberOfDigits < 500;

    [RelayCommand(CanExecute = nameof(CanDecrementMaxPairValue))]
    private void DecrementMaxPairValue()
    {
        MaxPairValue--;
    }

    [RelayCommand(CanExecute = nameof(CanIncrementMaxPairValue))]
    private void IncrementMaxPairValue()
    {
        MaxPairValue++;
    }

    private bool CanDecrementMaxPairValue() => MaxPairValue > 10;
    private bool CanIncrementMaxPairValue() => MaxPairValue < 99;

    [RelayCommand]
    private void ToggleShowTimer()
    {
        ShowTimer = !ShowTimer;
    }

    private string GenerateExampleNumber(bool separated)
    {
        // Generate a sample number based on current settings for display format examples
        var random = new Random(12345); // Fixed seed for consistent examples
        var exampleLength = Math.Min(NumberOfDigits, 12); // Limit example length for display
        var numbers = new List<int>();
        
        for (int i = 0; i < exampleLength; i += 2)
        {
            // Generate pairs within the max pair value constraint
            int pair = random.Next(0, Math.Min(MaxPairValue + 1, 100));
            numbers.Add(pair);
        }
        
        // Convert to string format
        var numberString = string.Join("", numbers.Select(n => n.ToString().PadLeft(2, '0')));
        
        // Truncate to exact length if needed
        if (numberString.Length > exampleLength)
        {
            numberString = numberString.Substring(0, exampleLength);
        }
        
        return _gameService.FormatNumber(numberString, separated);
    }

    private void UpdateExamples()
    {
        OnPropertyChanged(nameof(CurrentFormatExample));
    }

    private async Task SaveSettingsAsync()
    {
        var settings = new BigNumberSettings
        {
            NumberOfDigits = NumberOfDigits,
            MaxPairValue = MaxPairValue,
            ShowSeparated = ShowSeparated,
            ShowTimer = ShowTimer,
            FontSize = FontSizePreference,
            EnableInputVerification = EnableInputVerification,
            VerificationMode = VerificationMode
        };

        await _settingsService.SaveSettingsAsync(settings);
    }

    [RelayCommand]
    private void LoadMoreAssociations()
    {
        // Load next batch of associations for virtualization
        if (CurrentPhase == GamePhase.Recall && _allAssociations.Count > NumberAssociations.Count)
        {
            var nextBatch = _allAssociations
                .Skip(NumberAssociations.Count)
                .Take(ASSOCIATIONS_BATCH_SIZE)
                .ToList();
            
            foreach (var association in nextBatch)
            {
                NumberAssociations.Add(association);
            }
        }
    }

    [RelayCommand]
    private void ToggleAssociationsVisibility()
    {
        // Toggle associations visibility during recall phase OR in-app verification mode
        if (CurrentPhase == GamePhase.Recall ||
            (CurrentPhase == GamePhase.Stopped && ShowVerificationResults && (EnableInputVerification || IsOnDemandVerificationActive)))
        {
            IsAssociationsVisible = !IsAssociationsVisible;

            // Update toggle button text and color
            AssociationsToggleText = IsAssociationsVisible
                ? _localizationService.GetString("BigNumber_HideAssociations")
                : _localizationService.GetString("BigNumber_ShowAssociations_Button");
            AssociationsToggleColor = IsAssociationsVisible ? Colors.Red : Colors.Green;

            // Load associations if needed
            if (IsAssociationsVisible && NumberAssociations.Count == 0)
            {
                _ = LoadNumberAssociationsAsync();
            }
        }
    }

    private void UpdateAssociationsVisibility()
    {
        // Load associations during recall phase if visible
        if (CurrentPhase == GamePhase.Recall)
        {
            if (IsAssociationsVisible && NumberAssociations.Count == 0)
            {
                _ = LoadNumberAssociationsAsync();
            }
        }
        else if (CurrentPhase == GamePhase.Stopped && ShowVerificationResults && (EnableInputVerification || IsOnDemandVerificationActive))
        {
            // In-app verification mode: start with associations collapsed for clean UX
            // User can expand if they want to review educational content
            IsAssociationsVisible = false;

            // Update toggle button to show option to expand
            AssociationsToggleText = _localizationService.GetString("BigNumber_ShowAssociations_Button");
            AssociationsToggleColor = Colors.Green;

            // Load associations in background so they're ready if user expands
            if (NumberAssociations.Count == 0)
            {
                _ = LoadNumberAssociationsAsync();
            }
        }
        else
        {
            // Clear associations when not in recall phase and not in-app verification
            IsAssociationsVisible = false;
            NumberAssociations.Clear();
            _allAssociations.Clear();
        }
    }

    /// <summary>
    /// Updates the dynamic minimum height for the Editor based on expected content length.
    /// Optimizes mobile screen space usage by starting compact for short sequences
    /// while ensuring adequate space for longer numbers.
    /// </summary>
    private void UpdateDynamicMinimumHeight()
    {
        // Calculate optimal minimum height based on content length and platform
        double baseHeight;
        double platformMultiplier;

        // Get platform-specific base values
        if (DeviceInfo.Idiom == DeviceIdiom.Phone)
        {
            baseHeight = 44; // Standard mobile touch target
            platformMultiplier = 1.0;
        }
        else if (DeviceInfo.Idiom == DeviceIdiom.Tablet)
        {
            baseHeight = 56;
            platformMultiplier = 1.2;
        }
        else // Desktop
        {
            baseHeight = 40;
            platformMultiplier = 0.9;
        }

        // Smart sizing based on expected content length
        double calculatedHeight;
        
        if (NumberOfDigits <= 25)
        {
            // Short sequences (20-25 digits): Start compact
            // Most mobile users can see this in 1-2 lines, save screen space
            calculatedHeight = baseHeight * platformMultiplier;
        }
        else if (NumberOfDigits <= 50)
        {
            // Medium sequences (26-50 digits): Moderate height
            // Give more initial space as content will likely wrap
            calculatedHeight = baseHeight * 1.4 * platformMultiplier;
        }
        else if (NumberOfDigits <= 100)
        {
            // Long sequences (51-100 digits): Generous initial height
            // User will definitely need multi-line editing
            calculatedHeight = baseHeight * 1.8 * platformMultiplier;
        }
        else
        {
            // Very long sequences (100+ digits): Maximum initial height
            // Provide ample space for extensive multi-line content
            calculatedHeight = baseHeight * 2.2 * platformMultiplier;
        }

        // Apply minimum thresholds for touch targets and usability
        var minThreshold = DeviceInfo.Idiom == DeviceIdiom.Phone ? 40 : 
                          DeviceInfo.Idiom == DeviceIdiom.Tablet ? 50 : 36;
        
        DynamicMinimumHeight = Math.Max(calculatedHeight, minThreshold);
    }
    
    private void RequestUserInputFocus()
    {
        // Raise event to request focus on the user input field
        // The view will handle this to set focus on the Entry control
        FocusUserInputRequested?.Invoke(this, EventArgs.Empty);
    }


    private async Task LoadNumberAssociationsAsync()
    {
        try
        {
            NumberAssociations.Clear();
            _allAssociations.Clear();
            
            if (_currentGame?.GeneratedNumber == null)
                return;

            // Split the generated number into pairs
            var numberString = _currentGame.GeneratedNumber;
            var pairs = new List<string>();
            
            for (int i = 0; i < numberString.Length; i += 2)
            {
                if (i + 1 < numberString.Length)
                {
                    pairs.Add(numberString.Substring(i, 2));
                }
                else
                {
                    // Handle odd number of digits
                    pairs.Add(numberString.Substring(i, 1));
                }
            }

            // Load associations for each pair/digit in parallel
            var associationTasks = pairs.Select(async pair =>
            {
                var categoryKey = pair.Length == 1 ? "numbers" : "numbers";
                var tokenAssociations = await _packAssociationsService.GetTokenAssociationsAsync(categoryKey, pair);
                
                if (tokenAssociations.Any())
                {
                    // Get the default association or the first one
                    return tokenAssociations.FirstOrDefault(a => a.IsDefault) ?? tokenAssociations.First();
                }
                return null;
            });

            var results = await Task.WhenAll(associationTasks);
            _allAssociations.AddRange(results.Where(a => a != null)!);

            // Load first batch immediately for instant display
            var firstBatch = _allAssociations.Take(ASSOCIATIONS_BATCH_SIZE).ToList();
            
            // Update the observable collection on UI thread
            foreach (var association in firstBatch)
            {
                NumberAssociations.Add(association);
            }
        }
        catch (Exception ex)
        {
            // Log error but don't crash the app
            System.Diagnostics.Debug.WriteLine($"Error loading associations: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task ShareResults()
    {
        try
        {
            // Check if we have results to share (either verification results or general exercise completion)
            if (!ShowVerificationResults && !CanShareResults)
            {
                await _userNotificationService.ShowErrorDialogAsync(
                    _localizationService.GetString("Error_Title"), 
                    _localizationService.GetString("Error_NoResultsToShare"));
                return;
            }
            
            // For backward compatibility, also check the old timing conditions
            if (_memorizationStartTime == default)
            {
                // If we don't have memorization start time, set it to exercise start time
                _memorizationStartTime = _exerciseStartTime != default ? _exerciseStartTime : DateTime.UtcNow.AddMinutes(-1);
            }
            
            // If we don't have recall start time, estimate it (verification results means they've completed recall)
            if (_recallStartTime == default)
            {
                _recallStartTime = DateTime.UtcNow.AddSeconds(-30); // Estimate they spent 30 seconds on recall
            }

            var memorizationTime = _recallStartTime - _memorizationStartTime;

            // Create shareable result
            var shareResult = new BigNumberShareResult
            {
                ExerciseType = "BigNumber",
                Duration = memorizationTime,
                NumberOfDigits = _currentGame.GeneratedNumber.Length,
                MaxPairValue = MaxPairValue,
                MemorizationTime = memorizationTime
            };

            // Generate localized share content
            var shareContent = shareResult.GenerateShareContent(key => _localizationService.GetString(key));

            // Track share initiation
            // Track share initiation
            await _analyticsService.TrackEventAsync(AnalyticsEvents.ShareInitiated, new Dictionary<string, object>
            {
                [AnalyticsParameters.ExerciseType] = shareResult.ExerciseType,
                ["number_digits"] = shareResult.NumberOfDigits,
                ["memorization_seconds"] = (int)shareResult.MemorizationTime.TotalSeconds
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
            await _userNotificationService.ShowErrorDialogAsync(
                _localizationService.GetString("Error_Title"), 
                $"Failed to share results: {ex.Message}");

            // Track share error
            await _analyticsService.TrackEventAsync(AnalyticsEvents.ShareFailed, new Dictionary<string, object>
            {
                [AnalyticsParameters.ExerciseType] = "BigNumber",
                ["error"] = ex.Message
            });
        }
    }

    /// <summary>
    /// Updates the ShareData property with current exercise information
    /// </summary>
    private void UpdateShareData()
    {
        try
        {
            // Only create share data when there's something meaningful to share
            if (_currentGame?.GeneratedNumber == null)
            {
                ShareData = null;
                return;
            }

            // For backward compatibility, also check the old timing conditions
            if (_memorizationStartTime == default)
            {
                // If we don't have memorization start time, set it to exercise start time
                _memorizationStartTime = _exerciseStartTime != default ? _exerciseStartTime : DateTime.UtcNow.AddMinutes(-1);
            }

            // If we don't have recall start time, estimate it (verification results means they've completed recall)
            if (_recallStartTime == default)
            {
                _recallStartTime = DateTime.UtcNow.AddSeconds(-30); // Estimate they spent 30 seconds on recall
            }

            var memorizationTime = _recallStartTime - _memorizationStartTime;

            // Create shareable result data
            ShareData = new BigNumberShareResult
            {
                ExerciseType = "BigNumber",
                Duration = memorizationTime,
                NumberOfDigits = _currentGame.GeneratedNumber.Length,
                MaxPairValue = MaxPairValue,
                MemorizationTime = memorizationTime
            };
        }
        catch (Exception ex)
        {
            // Log error but don't crash - sharing will just not work
            System.Diagnostics.Debug.WriteLine($"Error updating share data: {ex.Message}");
            ShareData = null;
        }
    }

    [RelayCommand]
    private void StartNewChallenge()
    {
        // Use existing StartNewGame logic for new challenge
        StartNewGame();
    }

    // Input Verification Commands
    [RelayCommand]
    private void ToggleInputVerification()
    {
        EnableInputVerification = !EnableInputVerification;
    }

    [RelayCommand]
    private void StartOnDemandInputVerification()
    {
        IsOnDemandVerificationActive = true;
        IsVerificationPanelManuallyClosed = false; // Reset the manually closed flag when reopening

        // Start with associations collapsed for optimal UX
        // User can expand if they want to review
        IsAssociationsVisible = false;

        // Load associations in background so they're ready if user expands
        if (NumberAssociations.Count == 0)
        {
            _ = LoadNumberAssociationsAsync();
        }

        UpdateUIState(); // This will hide the on-demand button and show input verification interface
    }


    [RelayCommand(CanExecute = nameof(CanVerifyInput))]
    private async Task VerifyInput()
    {
        if (string.IsNullOrEmpty(UserInput) || string.IsNullOrEmpty(_originalNumber))
            return;

        // Use enhanced verification with timeout and retry mechanisms
        await ExecuteVerificationWithRetry();
    }

    /// <summary>
    /// Executes verification with enhanced error handling, timeout, and retry mechanisms
    /// </summary>
    private async Task ExecuteVerificationWithRetry()
    {
        const int maxRetries = 2;
        const int timeoutMs = 10000; // 10 second timeout

        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                // Enter processing state
                SetVerificationProcessingState();

                // Create cancellation token with timeout
                using var timeoutCts = new CancellationTokenSource(TimeSpan.FromMilliseconds(timeoutMs));

                // Execute verification with timeout protection
                await ExecuteVerificationCore(timeoutCts.Token);

                // Success - exit retry loop
                return;
            }
            catch (OperationCanceledException) when (attempt < maxRetries)
            {
                // Timeout occurred, but we have retries left
                System.Diagnostics.Debug.WriteLine($"Verification timeout on attempt {attempt}, retrying...");

                // Brief delay before retry
                await Task.Delay(500);
                continue;
            }
            catch (OperationCanceledException)
            {
                // Final timeout - no more retries
                await HandleVerificationError(new TimeoutException("Verification process timed out after multiple attempts"));
                return;
            }
            catch (Exception ex) when (attempt < maxRetries)
            {
                // Non-timeout error, but we have retries left
                System.Diagnostics.Debug.WriteLine($"Verification error on attempt {attempt}: {ex.Message}, retrying...");

                // Brief delay before retry
                await Task.Delay(500);
                continue;
            }
            catch (Exception ex)
            {
                // Final error - no more retries
                await HandleVerificationError(ex);
                return;
            }
        }
    }

    /// <summary>
    /// Core verification logic that can be executed with cancellation token support
    /// </summary>
    private async Task ExecuteVerificationCore(CancellationToken cancellationToken = default)
    {
        // Add platform-optimized delay to ensure users see the loading state
        await Task.Delay(GetPlatformOptimizedProcessingDelay(), cancellationToken);

        // Check for cancellation after delay
        cancellationToken.ThrowIfCancellationRequested();
        // Extract digits from user input
        var userDigits = new string(UserInput.Where(char.IsDigit).ToArray());
        var originalDigits = new string(_originalNumber.Where(char.IsDigit).ToArray());

        // Calculate accuracy
        var correctDigits = 0;
        var minLength = Math.Min(userDigits.Length, originalDigits.Length);

        for (int i = 0; i < minLength; i++)
        {
            // Check for cancellation during processing
            if (i % 100 == 0) // Check every 100 iterations to avoid excessive checks
                cancellationToken.ThrowIfCancellationRequested();

            if (userDigits[i] == originalDigits[i])
                correctDigits++;
        }

        CorrectDigitsCount = correctDigits;
        var accuracy = originalDigits.Length > 0 ? (double)correctDigits / originalDigits.Length * 100 : 0;
        AccuracyPercentage = accuracy;
        AccuracyPercentageText = $"{accuracy:F1}%";

        // Set colors and icons based on accuracy
        if (accuracy >= 95)
        {
            AccuracyColor = Color.FromArgb("#4CAF50"); // Green
            AccuracyBorderColor = Color.FromArgb("#4CAF50");
            AccuracyIcon = "✅";
            AccuracyTitle = _localizationService.GetString("BigNumber_AccuracyExcellent");
        }
        else if (accuracy >= 80)
        {
            AccuracyColor = Color.FromArgb("#FF9800"); // Orange
            AccuracyBorderColor = Color.FromArgb("#FF9800");
            AccuracyIcon = "👍";
            AccuracyTitle = _localizationService.GetString("BigNumber_AccuracyGood");
        }
        else
        {
            AccuracyColor = Color.FromArgb("#F44336"); // Red
            AccuracyBorderColor = Color.FromArgb("#F44336");
            AccuracyIcon = "💪";
            AccuracyTitle = _localizationService.GetString("BigNumber_AccuracyKeepPracticing");
        }

        ShowVerificationResults = true;
        CanShareResults = true; // Enable sharing when verification results are shown

        // Update share data with verification results
        UpdateShareData();

        // Check for cancellation before UI updates
        cancellationToken.ThrowIfCancellationRequested();

        // Generate the Wordle-style digit comparison
        GenerateDigitComparison(_originalNumber, UserInput);

        // Show completion state briefly, then return to normal
        await SetVerificationCompletedState();

        // Check for cancellation before triggering events
        cancellationToken.ThrowIfCancellationRequested();

        // Trigger auto-scroll to bring results into view
        ScrollToVerificationResults?.Invoke(this, EventArgs.Empty);

        // Haptic feedback
        if (_hapticFeedbackService?.IsSupported == true)
        {
            if (accuracy >= 95)
                _hapticFeedbackService.PerformSuccessFeedback();
            else
                _hapticFeedbackService.PerformSelectionFeedback();
        }

        // Track analytics with cancellation support
        try
        {
            await _analyticsService.TrackEventAsync("input_verification_completed", new Dictionary<string, object>
            {
                ["exercise_type"] = "BigNumber",
                ["accuracy_percentage"] = accuracy,
                ["correct_digits"] = correctDigits,
                ["total_digits"] = originalDigits.Length,
                ["user_input_length"] = userDigits.Length
            });
        }
        catch (Exception analyticsEx)
        {
            // Don't fail the entire verification if analytics fails
            System.Diagnostics.Debug.WriteLine($"Analytics tracking failed: {analyticsEx.Message}");
        }
    }

    /// <summary>
    /// Handles verification errors with appropriate user feedback
    /// </summary>
    private async Task HandleVerificationError(Exception ex)
    {
        // Reset to normal state on error
        SetVerificationNormalState();

        // Provide user-friendly error messages based on error type
        string errorMessage = ex switch
        {
            TimeoutException => _localizationService.GetString("BigNumber_VerificationTimeout"),
            OperationCanceledException => _localizationService.GetString("BigNumber_VerificationCancelled"),
            _ => $"{_localizationService.GetString("BigNumber_VerificationError")}: {ex.Message}"
        };

        await _userNotificationService.ShowErrorDialogAsync(
            _localizationService.GetString("Error_Title"),
            errorMessage);

        // Track error for analytics
        try
        {
            await _analyticsService.TrackEventAsync("input_verification_failed", new Dictionary<string, object>
            {
                ["exercise_type"] = "BigNumber",
                ["error_type"] = ex.GetType().Name,
                ["error_message"] = ex.Message
            });
        }
        catch (Exception analyticsEx)
        {
            System.Diagnostics.Debug.WriteLine($"Error analytics tracking failed: {analyticsEx.Message}");
        }
    }


    [RelayCommand]
    private void CancelVerification()
    {
        // Always close the verification panel and show the reopen button
        ClearInputVerificationState();
        IsVerificationPanelManuallyClosed = true;
        UpdateUIState();
    }

    /// <summary>
    /// Initializes the verification button to its default state
    /// </summary>
    private void InitializeVerificationButtonState()
    {
        VerificationButtonText = _localizationService.GetString("BigNumber_CheckAnswer");
        VerificationButtonBackgroundColor = Application.Current?.Resources.TryGetValue("Success", out var success) == true
            ? (Color)success
            : Color.FromArgb("#28A745");
        IsVerificationProcessing = false;
    }

    /// <summary>
    /// Sets the verification button to processing state
    /// </summary>
    private void SetVerificationProcessingState()
    {
        IsVerificationProcessing = true;
        VerificationButtonText = _localizationService.GetString("BigNumber_CheckAnswer_Processing");
        VerificationButtonBackgroundColor = Color.FromArgb("#6C757D"); // Gray color for processing
        CanVerifyInput = false;
        VerifyInputCommand.NotifyCanExecuteChanged();
    }

    /// <summary>
    /// Sets the verification button to completed state (brief success indication)
    /// </summary>
    private async Task SetVerificationCompletedState()
    {
        IsVerificationProcessing = true; // Keep disabled during success state
        VerificationButtonText = _localizationService.GetString("BigNumber_CheckAnswer_Completed");
        VerificationButtonBackgroundColor = Application.Current?.Resources.TryGetValue("Success", out var success) == true
            ? (Color)success
            : Color.FromArgb("#28A745");

        // Show success state with platform-optimized timing
        await Task.Delay(GetPlatformOptimizedSuccessDelay());
        SetVerificationNormalState();
    }

    /// <summary>
    /// Gets platform-optimized delay for success state display
    /// Desktop: Faster interactions (600ms)
    /// Mobile: Longer for better accessibility and touch feedback (1000ms)
    /// </summary>
    private int GetPlatformOptimizedSuccessDelay()
    {
        if (DeviceInfo.Idiom == DeviceIdiom.Desktop)
            return 600;  // Faster for desktop users with precise mouse control

        if (DeviceInfo.Idiom == DeviceIdiom.Phone)
            return 1000; // Longer for mobile users and screen reader accessibility

        if (DeviceInfo.Idiom == DeviceIdiom.Tablet)
            return 800;  // Medium timing for tablet users

        return 800;      // Default fallback
    }

    /// <summary>
    /// Gets platform-optimized delay for processing state visibility
    /// Ensures users see the loading state before results appear
    /// Desktop: Minimal delay (200ms) for responsive feel
    /// Mobile: Longer delay (400ms) for better visual feedback on touch interactions
    /// </summary>
    private int GetPlatformOptimizedProcessingDelay()
    {
        if (DeviceInfo.Idiom == DeviceIdiom.Desktop)
            return 200; // Minimal delay for desktop responsiveness

        if (DeviceInfo.Idiom == DeviceIdiom.Phone)
            return 400; // Longer for mobile touch feedback and loading visibility

        if (DeviceInfo.Idiom == DeviceIdiom.Tablet)
            return 300; // Balanced timing for tablet interactions

        return 300;     // Default fallback
    }

    /// <summary>
    /// Returns the verification button to normal state
    /// </summary>
    private void SetVerificationNormalState()
    {
        IsVerificationProcessing = false;
        VerificationButtonText = _localizationService.GetString("BigNumber_CheckAnswer");
        VerificationButtonBackgroundColor = Application.Current?.Resources.TryGetValue("Success", out var success) == true
            ? (Color)success
            : Color.FromArgb("#28A745");

        // Update button availability based on current input
        var digitsEntered = UserInput?.Where(char.IsDigit).Count() ?? 0;
        CanVerifyInput = digitsEntered > 0 && !IsVerificationProcessing;
        VerifyInputCommand.NotifyCanExecuteChanged();
    }

    // Helper methods
    private void ClearInputVerificationState()
    {
        UserInput = string.Empty;
        ShowVerificationResults = false;
        CanShareResults = false; // Disable sharing when clearing verification state
        ShareData = null; // Clear share data when clearing verification
        InputProgress = 0;
        // Initialize InputProgressText to always be visible (prevents layout shift)
        InputProgressText = string.Format(
            CultureInfo.CurrentCulture,
            _localizationService.GetString("BigNumber_DigitCount"),
            0,
            NumberOfDigits);
        CanVerifyInput = false;
        VerifyInputCommand.NotifyCanExecuteChanged();
        IsOnDemandVerificationActive = false;
        AccuracyPercentage = 0;
        AccuracyPercentageText = string.Empty;

        // Reset verification button to normal state
        SetVerificationNormalState();

        // Clear digit comparison
        ClearDigitComparison();
    }


    private string FormatNumberInput(string input)
    {
        if (string.IsNullOrEmpty(input))
            return string.Empty;

        // Extract only digits
        var digits = new string(input.Where(char.IsDigit).ToArray());
        
        if (string.IsNullOrEmpty(digits))
            return string.Empty;

        // Apply same formatting as displayed number
        if (ShowSeparated)
        {
            // Format as pairs: "12 34 56 78"
            var formatted = new StringBuilder();
            for (int i = 0; i < digits.Length; i += 2)
            {
                if (i > 0) formatted.Append(" ");
                
                if (i + 1 < digits.Length)
                {
                    formatted.Append(digits[i]).Append(digits[i + 1]);
                }
                else
                {
                    formatted.Append(digits[i]);
                }
            }
            return formatted.ToString();
        }
        else
        {
            // Return continuous digits
            return digits;
        }
    }
    
    // New UI Architecture Commands
    
    [RelayCommand]
    private void SelectManualVerification()
    {
        VerificationMode = VerificationModePreference.Manual;
        // Reset verification flags when switching to manual verification mode
        IsOnDemandVerificationActive = false;
        IsVerificationPanelManuallyClosed = false;
        UpdateContextActions();
    }
    
    // Legacy command for backward compatibility
    [RelayCommand]
    private void SelectTraditionalMode()
    {
        SelectManualVerification();
    }
    
    [RelayCommand]
    private void SelectAppVerification()
    {
        VerificationMode = VerificationModePreference.InApp;
        IsOnDemandVerificationActive = false; // Reset this flag when switching modes
        IsVerificationPanelManuallyClosed = false; // Ensure panel isn't marked as closed
        UpdateContextActions();
    }
    
    // Legacy command for backward compatibility
    [RelayCommand]
    private void SelectGuidedMode()
    {
        SelectAppVerification();
    }
    
    [RelayCommand]
    private void ToggleAdvancedSettings()
    {
        if (ShowAdvancedSettings)
        {
            // Collapse to toggle button
            ShowAdvancedSettings = false;
            ShowAdvancedSettingsToggle = true;
            IsAdvancedSettingsExpanded = false;
        }
        else
        {
            // Show collapsed state first, then expand
            ShowAdvancedSettings = true;
            ShowAdvancedSettingsToggle = false;
            IsAdvancedSettingsExpanded = !IsAdvancedSettingsExpanded;
            AdvancedSettingsToggleText = IsAdvancedSettingsExpanded ? "Hide" : "Show";
        }
    }
    
    [RelayCommand]
    private void PrimaryAction()
    {
        switch (CurrentPhase)
        {
            case GamePhase.Setup:
                StartMemorizing();
                break;
            case GamePhase.Memorizing:
                StopAndHide();
                break;
            case GamePhase.Stopped:
                if (IsManualVerificationSelected)
                {
                    StartRecall();
                }
                else if (IsAppVerificationSelected)
                {
                    // In app verification mode, primary action should start new game (as indicated by button text)
                    StartNewGame();
                }
                break;
            case GamePhase.Recall:
                StartNewGame();
                break;
        }
    }
    
    [RelayCommand]
    private void SecondaryAction()
    {
        switch (CurrentPhase)
        {
            case GamePhase.Memorizing:
                // Quick restart for adjustments
                StartNewGame();
                break;
            case GamePhase.Stopped:
                if (IsManualVerificationSelected)
                {
                    StartNewGame();
                }
                else if (IsAppVerificationSelected)
                {
                    // In app verification mode, secondary action should just start a new game
                    // Input verification should already be visible automatically
                    StartNewGame();
                }
                break;
            case GamePhase.Recall:
                StopAndHide();
                break;
        }
    }
    
    private void StartInputVerification()
    {
        IsOnDemandVerificationActive = true;
        IsVerificationPanelManuallyClosed = false; // Reset the manually closed flag when reopening
        UpdateUIState(); // This will hide the on-demand button and show input verification interface
    }
    
    private void UpdateModeColors()
    {
        var primaryColor = Application.Current?.Resources.TryGetValue("Primary", out var primary) == true ? (Color)primary : Colors.Blue;
        var grayColor = Colors.Gray;
        var lightGrayColor = Colors.LightGray;
        var whiteColor = Colors.White;
        var blackColor = Colors.Black;
        
        if (IsManualVerificationSelected)
        {
            ManualVerificationBackgroundColor = lightGrayColor;
            ManualVerificationBorderColor = primaryColor;
            ManualVerificationTextColor = primaryColor;
            ManualVerificationSubtextColor = blackColor;
            
            AppVerificationBackgroundColor = whiteColor;
            AppVerificationBorderColor = grayColor;
            AppVerificationTextColor = blackColor;
            AppVerificationSubtextColor = grayColor;
        }
        else
        {
            ManualVerificationBackgroundColor = whiteColor;
            ManualVerificationBorderColor = grayColor;
            ManualVerificationTextColor = blackColor;
            ManualVerificationSubtextColor = grayColor;
            
            AppVerificationBackgroundColor = lightGrayColor;
            AppVerificationBorderColor = primaryColor;
            AppVerificationTextColor = primaryColor;
            AppVerificationSubtextColor = blackColor;
        }
    }

    /// <summary>
    /// Determines if we should show confirmation dialog before starting new game.
    /// Based on UI/UX agent recommendations for smart contextual confirmation.
    /// No confirmation needed post-completion when results are shown.
    /// </summary>
    private bool ShouldShowNewGameConfirmation()
    {
        return CurrentPhase switch
        {
            GamePhase.Memorizing => true, // Always confirm during memorization (high risk)
            GamePhase.Stopped => false,  // No confirmation when stopped - results are shown, user intent is clear
            GamePhase.Recall => !ShowVerificationResults, // Confirm during input, but not after results are shown
            GamePhase.Setup => false,    // No progress to lose
            _ => false
        };
    }

    /// <summary>
    /// Platform-aware logic for recall phase confirmation.
    /// Mobile users get more aggressive confirmation due to higher accidental tap risk.
    /// </summary>
    private bool ShouldConfirmDuringRecall()
    {
        if (_recallStartTime == default) return false; // Just entered recall, no time invested

        var recallDuration = (DateTime.UtcNow - _recallStartTime).TotalSeconds;

        // Platform-specific thresholds based on UX agent recommendations
        var thresholdSeconds = DeviceInfo.Idiom == DeviceIdiom.Phone || DeviceInfo.Idiom == DeviceIdiom.Tablet
            ? MOBILE_RECALL_THRESHOLD_SECONDS
            : DESKTOP_RECALL_THRESHOLD_SECONDS;

        return recallDuration > thresholdSeconds;
    }

    /// <summary>
    /// Shows context-aware confirmation dialog with proper localization and analytics.
    /// </summary>
    private async Task<bool> ShowNewGameConfirmationAsync()
    {
        try
        {
            var title = _localizationService.GetString("BigNumber_ConfirmNewGame_Title");
            var message = GetContextualConfirmationMessage();
            var cancelText = _localizationService.GetString("BigNumber_ConfirmNewGame_Cancel");
            var confirmText = _localizationService.GetString("BigNumber_ConfirmNewGame_Confirm");

            // Track confirmation dialog display for analytics (fire-and-forget)
            _ = Task.Run(async () =>
            {
                try
                {
                    await _analyticsService.TrackEventAsync("big_number_confirmation_shown", new Dictionary<string, object>
                    {
                        ["current_phase"] = CurrentPhase.ToString(),
                        ["platform"] = DeviceInfo.Idiom.ToString()
                    });
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Analytics error: {ex.Message}");
                }
            });

            var result = await _userNotificationService.ShowConfirmationDialogAsync(
                title, message, confirmText, cancelText);

            // Track user decision (fire-and-forget)
            _ = Task.Run(async () =>
            {
                try
                {
                    await _analyticsService.TrackEventAsync("big_number_confirmation_result", new Dictionary<string, object>
                    {
                        ["user_action"] = result ? "confirmed" : "cancelled",
                        ["current_phase"] = CurrentPhase.ToString()
                    });
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Analytics error: {ex.Message}");
                }
            });

            return result;
        }
        catch (Exception ex)
        {
            // Log error but don't block the flow - default to showing confirmation for safety
            System.Diagnostics.Debug.WriteLine($"Error showing confirmation dialog: {ex.Message}");
            return true; // Safer default - show confirmation
        }
    }

    /// <summary>
    /// Returns context-aware confirmation message based on current game phase.
    /// </summary>
    private string GetContextualConfirmationMessage()
    {
        try
        {
            return CurrentPhase switch
            {
                GamePhase.Memorizing => _localizationService.GetString("BigNumber_ConfirmNewGame_MemorizingMessage"),
                GamePhase.Recall or GamePhase.Stopped => _localizationService.GetString("BigNumber_ConfirmNewGame_RecallMessage"),
                _ => _localizationService.GetString("BigNumber_ConfirmNewGame_MemorizingMessage") // Fallback
            };
        }
        catch (Exception ex)
        {
            // Fallback for any localization issues
            System.Diagnostics.Debug.WriteLine($"Localization error: {ex.Message}");
            return "Are you sure you want to start a new challenge?";
        }
    }

    private void UpdateContextActions()
    {
        var primaryColor = Application.Current?.Resources.TryGetValue("Primary", out var primary) == true ? (Color)primary : Colors.Blue;
        var successColor = Application.Current?.Resources.TryGetValue("Success", out var success) == true ? (Color)success : Colors.Green;
        var secondaryColor = Application.Current?.Resources.TryGetValue("Secondary", out var secondary) == true ? (Color)secondary : Colors.Orange;
        
        switch (CurrentPhase)
        {
            case GamePhase.Setup:
                PrimaryActionText = _localizationService.GetString("BigNumber_StartMemorizing");
                PrimaryActionBackgroundColor = primaryColor;
                ShowPrimaryAction = true;
                ShowSecondaryAction = false;
                IsGuidedPracticeInputVisible = false;
                ShowSessionActions = false;
                break;
                
            case GamePhase.Memorizing:
                PrimaryActionText = _localizationService.GetString("BigNumber_StopHide");
                PrimaryActionBackgroundColor = secondaryColor;
                SecondaryActionText = _localizationService.GetString("BigNumber_NewGame");
                SecondaryActionBackgroundColor = Colors.Gray;
                ShowPrimaryAction = true;
                ShowSecondaryAction = true;
                IsGuidedPracticeInputVisible = false;
                ShowSessionActions = false;
                break;
                
            case GamePhase.Stopped:
                if (IsManualVerificationSelected)
                {
                    PrimaryActionText = _localizationService.GetString("BigNumber_Recall");
                    PrimaryActionBackgroundColor = successColor;
                    SecondaryActionText = _localizationService.GetString("BigNumber_NewGame");
                    SecondaryActionBackgroundColor = Colors.Gray;
                    ShowPrimaryAction = true;
                    ShowSecondaryAction = true;
                    IsGuidedPracticeInputVisible = false;
                }
                else if (IsAppVerificationSelected)
                {
                    // In app verification mode, only show New Game button - input panel should already be visible
                    PrimaryActionText = _localizationService.GetString("BigNumber_NewGame");
                    PrimaryActionBackgroundColor = Colors.Gray;
                    SecondaryActionText = string.Empty;
                    SecondaryActionBackgroundColor = Colors.Transparent;
                    ShowPrimaryAction = true;
                    ShowSecondaryAction = false; // No secondary action in guided mode stopped state
                    // The input panel should be automatically visible via UpdateUIState
                    IsGuidedPracticeInputVisible = IsInputVerificationVisible;
                }
                ShowSessionActions = false;
                break;
                
            case GamePhase.Recall:
                // In manual verification mode (post-completion), hide game management buttons
                // and let the New Challenge + Share buttons (ShowSessionActions) take prominence
                if (IsManualVerificationSelected)
                {
                    // Hide primary and secondary actions - user has completed the exercise
                    // and should see completion actions (New Challenge, Share) instead
                    ShowPrimaryAction = false;
                    ShowSecondaryAction = false;
                }
                else
                {
                    // In app verification mode, keep management buttons visible
                    PrimaryActionText = _localizationService.GetString("BigNumber_NewGame");
                    PrimaryActionBackgroundColor = Colors.Gray;
                    SecondaryActionText = _localizationService.GetString("BigNumber_StopHide");
                    SecondaryActionBackgroundColor = secondaryColor;
                    ShowPrimaryAction = true;
                    ShowSecondaryAction = true;
                }
                IsGuidedPracticeInputVisible = IsAppVerificationSelected && IsInputVerificationVisible;
                ShowSessionActions = true; // Always show completion actions (New Challenge, Share)
                break;
        }
    }

    // Toggle command removed - comparison is always visible after verification

    /// <summary>
    /// Generates and displays the Wordle-style comparison between original number and user's answer
    /// </summary>
    private void GenerateDigitComparison(string originalNumber, string userAnswer)
    {
        if (string.IsNullOrEmpty(originalNumber) || string.IsNullOrEmpty(userAnswer))
            return;

        try
        {
            // Generate comparison results using the comparison service
            _comparisonResult = _digitComparisonService.CompareNumbers(originalNumber, userAnswer);

            // Build tile collections in temporary lists to avoid iOS CollectionView issues
            var originalTiles = new List<DigitComparisonTileViewModel>();
            var userTiles = new List<DigitComparisonTileViewModel>();

            // Generate tile view models for both rows
            foreach (var digitResult in _comparisonResult.DigitResults)
            {
                // Create original number tile
                var originalTile = DigitComparisonTileViewModel.FromComparisonResult(
                    digitResult, 
                    true, // isOriginalRow
                    key => _localizationService.GetString(key));
                originalTiles.Add(originalTile);

                // Create user answer tile
                var userTile = DigitComparisonTileViewModel.FromComparisonResult(
                    digitResult, 
                    false, // isOriginalRow
                    key => _localizationService.GetString(key));
                userTiles.Add(userTile);
            }

            // Replace collections in one operation to avoid iOS CollectionView invalidation issues
            OriginalNumberTiles = new ObservableCollection<DigitComparisonTileViewModel>(originalTiles);
            UserAnswerTiles = new ObservableCollection<DigitComparisonTileViewModel>(userTiles);

            // Show the comparison section immediately when verification happens
            ShowDigitComparison = true;
            IsDigitComparisonExpanded = true; // Always show expanded
            ComparisonToggleText = string.Empty; // No toggle needed anymore
            
            // Setup navigation for long numbers
            SetupNavigationControls();

        }
        catch (Exception ex)
        {
            // Handle any errors gracefully
            System.Diagnostics.Debug.WriteLine($"Error generating digit comparison: {ex.Message}");
            ShowDigitComparison = false;
        }
    }

    /// <summary>
    /// Clears the digit comparison results and UI state
    /// </summary>
    private void ClearDigitComparison()
    {
        // Replace with empty collections to avoid iOS CollectionView issues
        OriginalNumberTiles = new ObservableCollection<DigitComparisonTileViewModel>();
        UserAnswerTiles = new ObservableCollection<DigitComparisonTileViewModel>();
        ShowDigitComparison = false;
        IsDigitComparisonExpanded = true; // Always expanded when shown
        _comparisonResult = null;
        
        // Clear navigation state
        ShowComparisonNavigation = false;
        ShowComparisonLegend = false;
        _currentScrollPosition = 0;

        // Clear legend state
        IsLegendVisible = false;
        LegendToggleText = string.Empty;
    }

    /// <summary>
    /// Sets up navigation controls and indicators based on number length
    /// </summary>
    private void SetupNavigationControls()
    {
        if (_comparisonResult == null) return;

        var digitCount = _comparisonResult.DigitResults.Count;
        
        // Use a more conservative estimate for tiles per screen
        // Account for padding, margins, and varying screen sizes
        int estimatedTilesPerScreen;
        
        if (DeviceInfo.Idiom == DeviceIdiom.Phone)
        {
            estimatedTilesPerScreen = 8;
        }
        else if (DeviceInfo.Idiom == DeviceIdiom.Tablet)
        {
            estimatedTilesPerScreen = 12;
        }
        else
        {
            estimatedTilesPerScreen = 15; // Desktop/other platforms
        }
        
        ShowComparisonNavigation = digitCount > estimatedTilesPerScreen;
        ShowComparisonLegend = true; // Always show legend option regardless of number length

        // Initialize legend state - start collapsed to save screen space
        IsLegendVisible = false;
        LegendToggleText = _localizationService.GetString("BigNumber_Show");

        // Initialize navigation state
        _currentScrollPosition = 0;
        UpdateNavigationState();
        UpdateComparisonSummary();
        
        // Initialize dynamic position indicator
        UpdateDynamicPosition(0);
        
        // Set accuracy color
        var accuracy = _comparisonResult.AccuracyPercentage;
        AccuracyColor = accuracy >= 90 ? Color.FromArgb("#28A745") : // Green
                       accuracy >= 70 ? Color.FromArgb("#FD7E14") : // Orange  
                                       Color.FromArgb("#DC3545");   // Red
    }

    /// <summary>
    /// Updates navigation button states and position text
    /// </summary>
    private void UpdateNavigationState()
    {
        if (_comparisonResult == null)
        {
            CanNavigateLeft = false;
            CanNavigateRight = false;
            ComparisonPositionText = string.Empty;
            return;
        }

        var totalDigits = _comparisonResult.DigitResults.Count;
        var endPosition = Math.Min(_currentScrollPosition + DIGITS_PER_CHUNK, totalDigits);
        
        CanNavigateLeft = _currentScrollPosition > 0;
        CanNavigateRight = endPosition < totalDigits;
        
        ComparisonPositionText = string.Format(
            _localizationService.GetString("BigNumber_PositionIndicator"),
            _currentScrollPosition + 1,
            endPosition,
            totalDigits);
    }
    
    /// <summary>
    /// Gets the total item width including margins and spacing to match XAML layout
    /// This ensures consistent calculations between position tracking and scroll navigation
    /// </summary>
    private double GetTotalItemWidth()
    {
        // Base width from XAML OnIdiom values
        double baseWidth;
        if (DeviceInfo.Idiom == DeviceIdiom.Phone)
            baseWidth = 40.0;
        else if (DeviceInfo.Idiom == DeviceIdiom.Tablet)
            baseWidth = 50.0;
        else if (DeviceInfo.Idiom == DeviceIdiom.Desktop)
            baseWidth = 60.0;
        else
            baseWidth = 40.0;
        
        // XAML: Margin="2" (2px on all sides) + Spacing="4"
        const double margin = 2.0 * 2; // Left + Right margins
        const double spacing = 4.0;    // Spacing between items
        
        return baseWidth + margin + spacing;
    }
    
    /// <summary>
    /// Gets the approximate number of tiles visible per screen based on device type
    /// </summary>
    private int GetTilesPerScreen()
    {
        if (DeviceInfo.Idiom == DeviceIdiom.Phone)
            return 8;
        else if (DeviceInfo.Idiom == DeviceIdiom.Tablet)
            return 12;
        else if (DeviceInfo.Idiom == DeviceIdiom.Desktop)
            return 15;
        else
            return 10;
    }
    
    /// <summary>
    /// Updates the dynamic position indicator based on current scroll position
    /// Called in real-time during scroll events
    /// </summary>
    public void UpdateDynamicPosition(double scrollX)
    {
        if (_comparisonResult == null) return;
        
        var totalDigits = _comparisonResult.DigitResults.Count;
        
        // Use unified width calculation to match scroll navigation
        double itemWidth = GetTotalItemWidth();
        int tilesPerScreen = GetTilesPerScreen();
        
        // Calculate current visible range
        int startPosition = Math.Max(1, (int)(scrollX / itemWidth) + 1);
        int endPosition = Math.Min(totalDigits, startPosition + tilesPerScreen - 1);
        
        // Update position text with real-time values
        DynamicPositionText = string.Format(
            _localizationService.GetString("BigNumber_DynamicPosition"),
            startPosition,
            endPosition,
            totalDigits);
    }

    /// <summary>
    /// Updates the comparison summary text with accuracy and error info
    /// </summary>
    private void UpdateComparisonSummary()
    {
        if (_comparisonResult == null)
        {
            ComparisonSummaryText = string.Empty;
            return;
        }

        var accuracy = _comparisonResult.AccuracyPercentage;
        var errorCount = _comparisonResult.IncorrectCount + _comparisonResult.WrongPositionCount + _comparisonResult.MissingCount;
        
        if (errorCount == 0)
        {
            ComparisonSummaryText = _localizationService.GetString("BigNumber_PerfectScore");
        }
        else
        {
            ComparisonSummaryText = string.Format(
                _localizationService.GetString("BigNumber_AccuracyWithErrors"),
                accuracy.ToString("F1"),
                errorCount);
        }
    }

    /// <summary>
    /// Navigation command to scroll left in comparison
    /// </summary>
    [RelayCommand]
    private void NavigateComparisonLeft()
    {
        if (_comparisonResult != null && _currentScrollPosition > 0)
        {
            _currentScrollPosition = Math.Max(0, _currentScrollPosition - DIGITS_PER_CHUNK);
            UpdateNavigationState();
            ScrollToPositionRequested?.Invoke(this, _currentScrollPosition);
        }
    }

    /// <summary>
    /// Navigation command to scroll right in comparison
    /// </summary>
    [RelayCommand]
    private void NavigateComparisonRight()
    {
        if (_comparisonResult != null)
        {
            var totalDigits = _comparisonResult.DigitResults.Count;
            if (totalDigits <= DIGITS_PER_CHUNK)
            {
                // All digits fit in one chunk, no need to navigate
                return;
            }
            
            var maxPosition = totalDigits - DIGITS_PER_CHUNK;
            _currentScrollPosition = Math.Min(maxPosition, _currentScrollPosition + DIGITS_PER_CHUNK);
            _currentScrollPosition = Math.Max(0, _currentScrollPosition); // Additional safeguard
            UpdateNavigationState();
            ScrollToPositionRequested?.Invoke(this, _currentScrollPosition);
        }
    }

    /// <summary>
    /// Command to center/reset the comparison view
    /// </summary>
    [RelayCommand]
    private void CenterComparison()
    {
        if (_comparisonResult != null)
        {
            _currentScrollPosition = 0;
            UpdateNavigationState();
            ScrollToPositionRequested?.Invoke(this, _currentScrollPosition);
        }
    }

    /// <summary>
    /// Toggles the visibility of the comparison legend
    /// </summary>
    [RelayCommand]
    private void ToggleLegend()
    {
        IsLegendVisible = !IsLegendVisible;
        LegendToggleText = IsLegendVisible
            ? _localizationService.GetString("BigNumber_Hide")
            : _localizationService.GetString("BigNumber_Show");

        // Trigger smooth scroll to legend when expanding (after a brief delay for UI updates)
        if (IsLegendVisible)
        {
            System.Diagnostics.Debug.WriteLine("ToggleLegend: Legend is now visible, scheduling scroll event");

            // Use MainThread.BeginInvokeOnMainThread for MAUI compatibility
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                System.Diagnostics.Debug.WriteLine("ToggleLegend: Inside MainThread, about to delay");
                await Task.Delay(100); // Allow UI to expand first
                System.Diagnostics.Debug.WriteLine($"ToggleLegend: Invoking ScrollToLegendRequested event, has listeners: {ScrollToLegendRequested != null}");
                ScrollToLegendRequested?.Invoke(this, EventArgs.Empty);
                System.Diagnostics.Debug.WriteLine("ToggleLegend: Event invocation completed");
            });
        }
    }

    /// <summary>
    /// Computed property to check if there are errors in the comparison
    /// </summary>
    public bool HasErrors => _comparisonResult != null &&
                            (_comparisonResult.IncorrectCount + _comparisonResult.WrongPositionCount + _comparisonResult.MissingCount) > 0;
}