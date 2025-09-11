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
    private IDispatcherTimer? _timer;
    private BigNumberGame _currentGame = new();
    private string _originalNumber = string.Empty;
    
    // Analytics tracking
    private DateTime _exerciseStartTime;
    private DateTime _memorizationStartTime;
    private DateTime _recallStartTime;

    [ObservableProperty]
    private int numberOfDigits = 20;

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
    private bool showAssociations = false;

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
    private string userInput = string.Empty;

    [ObservableProperty]
    private string formattedUserInput = string.Empty;

    [ObservableProperty]
    private double inputProgress = 0.0;

    [ObservableProperty]
    private string inputProgressText = string.Empty;

    [ObservableProperty]
    private bool showFormattedInput = false;

    [ObservableProperty]
    private bool canVerifyInput = false;

    [ObservableProperty]
    private bool showVerificationResults = false;

    [ObservableProperty]
    private bool showRetryButton = false;

    [ObservableProperty]
    private string accuracyTitle = string.Empty;

    [ObservableProperty]
    private string accuracyPercentageText = string.Empty;

    [ObservableProperty]
    private Color accuracyColor = Colors.Black;

    [ObservableProperty]
    private Color accuracyBorderColor = Colors.Gray;

    [ObservableProperty]
    private string accuracyIcon = string.Empty;

    [ObservableProperty]
    private int correctDigitsCount = 0;

    public string CurrentFormatExample => GenerateExampleNumber(ShowSeparated);

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
        ShowAssociations = settings.ShowAssociations;
        EnableInputVerification = settings.EnableInputVerification;
        
        IsContinuousSelected = !settings.ShowSeparated;
        IsSeparatedSelected = settings.ShowSeparated;
        
        UpdateNumberFontSize();
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

    partial void OnShowAssociationsChanged(bool value)
    {
        _ = SaveSettingsAsync();
        UpdateAssociationsVisibility();
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
    }

    partial void OnEnableInputVerificationChanged(bool value)
    {
        _ = SaveSettingsAsync();
        UpdateUIState();
        
        if (!value)
        {
            // Clear input verification state when disabled
            ClearInputVerificationState();
        }
    }

    partial void OnUserInputChanged(string value)
    {
        // Format input in real-time
        FormattedUserInput = FormatNumberInput(value);
        ShowFormattedInput = !string.IsNullOrEmpty(value);
        
        // Update progress
        var digitsEntered = value?.Where(char.IsDigit).Count() ?? 0;
        InputProgress = NumberOfDigits > 0 ? (double)digitsEntered / NumberOfDigits : 0;
        InputProgressText = string.Format(
            CultureInfo.CurrentCulture,
            _localizationService.GetString("BigNumber_InputProgress"),
            digitsEntered,
            NumberOfDigits);
        
        // Update verification button state
        CanVerifyInput = digitsEntered > 0;
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
    private async void StartMemorizing()
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
    private async void StopAndHide()
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
    }

    [RelayCommand]
    private async void StartRecall()
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
            
            // Initialize associations visibility based on settings
            IsAssociationsVisible = ShowAssociations;
            AssociationsToggleText = IsAssociationsVisible 
                ? _localizationService.GetString("BigNumber_HideAssociations") 
                : _localizationService.GetString("BigNumber_ShowAssociations_Button");
            AssociationsToggleColor = IsAssociationsVisible ? Colors.Red : Colors.Green;
            
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
    private async void StartNewGame()
    {
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
        
        // Enable sharing when memorization is completed (stopped or in recall)
        CanShareResults = CurrentPhase == GamePhase.Stopped || CurrentPhase == GamePhase.Recall;
        
        // Timer visibility logic:
        // - During memorizing: only show if ShowTimer setting is enabled
        // - When stopped or recall: ALWAYS show the final time regardless of setting
        IsTimerVisible = (ShowTimer && CurrentPhase == GamePhase.Memorizing) || CurrentPhase == GamePhase.Stopped || CurrentPhase == GamePhase.Recall;
        
        // Input verification visibility: show when enabled OR on-demand active, and in stopped phase
        IsInputVerificationVisible = (EnableInputVerification || IsOnDemandVerificationActive) && CurrentPhase == GamePhase.Stopped;
        
        // On-demand verify button: show when NOT pre-enabled, in stopped phase, and not already active
        ShowOnDemandVerifyButton = !EnableInputVerification && CurrentPhase == GamePhase.Stopped && !IsOnDemandVerificationActive;
        
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
            ShowAssociations = ShowAssociations,
            EnableInputVerification = EnableInputVerification
        };

        await _settingsService.SaveSettingsAsync(settings);
    }

    [RelayCommand]
    private void ToggleAssociations()
    {
        ShowAssociations = !ShowAssociations;
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
        // This is for runtime toggle during recall phase
        if (CurrentPhase == GamePhase.Recall)
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
        else
        {
            // Clear associations when not in recall phase
            IsAssociationsVisible = false;
            NumberAssociations.Clear();
            _allAssociations.Clear();
        }
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
    private async Task ShareResultsAsync()
    {
        try
        {
            if (_memorizationStartTime == default || _recallStartTime == default)
            {
                await _userNotificationService.ShowErrorDialogAsync(
                    _localizationService.GetString("Error_Title"), 
                    _localizationService.GetString("Error_NoResultsToShare"));
                return;
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
        UpdateUIState(); // This will hide the on-demand button and show input verification interface
    }

    [RelayCommand]
    private void ClearInput()
    {
        UserInput = string.Empty;
        ShowVerificationResults = false;
        ShowRetryButton = false;
    }

    [RelayCommand]
    private async Task VerifyInput()
    {
        if (string.IsNullOrEmpty(UserInput) || string.IsNullOrEmpty(_originalNumber))
            return;

        try
        {
            // Extract digits from user input
            var userDigits = new string(UserInput.Where(char.IsDigit).ToArray());
            var originalDigits = new string(_originalNumber.Where(char.IsDigit).ToArray());

            // Calculate accuracy
            var correctDigits = 0;
            var minLength = Math.Min(userDigits.Length, originalDigits.Length);
            
            for (int i = 0; i < minLength; i++)
            {
                if (userDigits[i] == originalDigits[i])
                    correctDigits++;
            }

            CorrectDigitsCount = correctDigits;
            var accuracy = originalDigits.Length > 0 ? (double)correctDigits / originalDigits.Length * 100 : 0;
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
            ShowRetryButton = accuracy < 100;

            // Haptic feedback
            if (_hapticFeedbackService?.IsSupported == true)
            {
                if (accuracy >= 95)
                    _hapticFeedbackService.PerformSuccessFeedback();
                else
                    _hapticFeedbackService.PerformSelectionFeedback();
            }

            // Track analytics
            await _analyticsService.TrackEventAsync("input_verification_completed", new Dictionary<string, object>
            {
                ["exercise_type"] = "BigNumber",
                ["accuracy_percentage"] = accuracy,
                ["correct_digits"] = correctDigits,
                ["total_digits"] = originalDigits.Length,
                ["user_input_length"] = userDigits.Length
            });
        }
        catch (Exception ex)
        {
            await _userNotificationService.ShowErrorDialogAsync(
                _localizationService.GetString("Error_Title"), 
                $"Failed to verify input: {ex.Message}");
        }
    }

    [RelayCommand]
    private void RetryInput()
    {
        UserInput = string.Empty;
        ShowVerificationResults = false;
        ShowRetryButton = false;
    }

    // Helper methods
    private void ClearInputVerificationState()
    {
        UserInput = string.Empty;
        ShowVerificationResults = false;
        ShowRetryButton = false;
        InputProgress = 0;
        InputProgressText = string.Empty;
        ShowFormattedInput = false;
        CanVerifyInput = false;
        IsOnDemandVerificationActive = false;
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
}