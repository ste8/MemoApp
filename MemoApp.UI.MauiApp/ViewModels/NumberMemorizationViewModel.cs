using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MemoApp.Core.NumberMemorization;
using MemoApp.Localization.Services;

namespace MemoApp.UI.MauiApp.ViewModels;

public partial class NumberMemorizationViewModel : BaseViewModel
{
    private readonly INumberMemorizationService _gameService;
    private readonly INumberMemorizationSettingsService _settingsService;
    private readonly ILocalizationService _localizationService;
    private IDispatcherTimer? _timer;
    private NumberMemorizationGame _currentGame = new();

    [ObservableProperty]
    private int numberOfDigits = 20;

    [ObservableProperty]
    private int maxPairValue = 99;

    [ObservableProperty]
    private bool showSeparated = false;

    [ObservableProperty]
    private bool showTimer = false;

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

    public NumberMemorizationViewModel(
        INumberMemorizationService gameService,
        INumberMemorizationSettingsService settingsService,
        ILocalizationService localizationService)
    {
        _gameService = gameService ?? throw new ArgumentNullException(nameof(gameService));
        _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
        _localizationService = localizationService ?? throw new ArgumentNullException(nameof(localizationService));
        
        Title = _localizationService.GetString("NumberMemorization_Title");
        
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
        
        IsContinuousSelected = !settings.ShowSeparated;
        IsSeparatedSelected = settings.ShowSeparated;
    }

    partial void OnNumberOfDigitsChanged(int value)
    {
        // Clamp value
        if (value < 1) NumberOfDigits = 1;
        else if (value > 500) NumberOfDigits = 500;
        
        _ = SaveSettingsAsync();
    }

    partial void OnMaxPairValueChanged(int value)
    {
        // Clamp value
        if (value < 10) MaxPairValue = 10;
        else if (value > 99) MaxPairValue = 99;
        
        _ = SaveSettingsAsync();
    }

    partial void OnShowSeparatedChanged(bool value)
    {
        _ = SaveSettingsAsync();
        UpdateDisplayedNumber();
    }

    partial void OnShowTimerChanged(bool value)
    {
        _ = SaveSettingsAsync();
        // Update timer visibility: during memorizing respect setting, when stopped always show
        IsTimerVisible = (value && CurrentPhase == GamePhase.Memorizing) || CurrentPhase == GamePhase.Stopped;
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

    [RelayCommand]
    private void StartMemorizing()
    {
        // Create new game with current settings
        var settings = new NumberMemorizationSettings
        {
            NumberOfDigits = NumberOfDigits,
            MaxPairValue = MaxPairValue,
            ShowSeparated = ShowSeparated,
            ShowTimer = ShowTimer
        };

        _currentGame = _gameService.CreateNewGame(settings);
        _gameService.StartGame(_currentGame);
        
        CurrentPhase = _currentGame.Phase;
        
        // Start timer
        _timer?.Start();
    }

    [RelayCommand]
    private void StopAndHide()
    {
        _gameService.StopGame(_currentGame);
        CurrentPhase = _currentGame.Phase;
        
        // Stop timer
        _timer?.Stop();
        UpdateTimerDisplay();
    }

    [RelayCommand]
    private void ToggleRecall()
    {
        _gameService.ToggleNumberVisibility(_currentGame);
        UpdateDisplayedNumber();
    }

    [RelayCommand]
    private void StartNewGame()
    {
        _timer?.Stop();
        _gameService.ResetGame(_currentGame);
        CurrentPhase = _currentGame.Phase;
        TimerText = "00:00";
    }

    private void UpdateUIState()
    {
        IsSetupSectionVisible = CurrentPhase == GamePhase.Setup;
        IsGameSectionVisible = CurrentPhase != GamePhase.Setup;
        IsStopButtonVisible = CurrentPhase == GamePhase.Memorizing;
        IsRecallButtonVisible = CurrentPhase == GamePhase.Stopped;
        
        // Timer visibility logic:
        // - During memorizing: only show if ShowTimer setting is enabled
        // - When stopped: ALWAYS show the final time regardless of setting
        IsTimerVisible = (ShowTimer && CurrentPhase == GamePhase.Memorizing) || CurrentPhase == GamePhase.Stopped;
        
        UpdateDisplayedNumber();
    }

    private void UpdateDisplayedNumber()
    {
        if (_currentGame.Phase == GamePhase.Setup || !_currentGame.IsNumberVisible)
        {
            DisplayedNumber = string.Empty;
        }
        else
        {
            DisplayedNumber = _gameService.FormatNumber(_currentGame.GeneratedNumber, ShowSeparated);
        }
        
        IsNumberVisible = !string.IsNullOrEmpty(DisplayedNumber);
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

    private async Task SaveSettingsAsync()
    {
        var settings = new NumberMemorizationSettings
        {
            NumberOfDigits = NumberOfDigits,
            MaxPairValue = MaxPairValue,
            ShowSeparated = ShowSeparated,
            ShowTimer = ShowTimer
        };

        await _settingsService.SaveSettingsAsync(settings);
    }
}