using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MemoApp.Core.MajorSystem;
using MemoApp.Localization.Services;
using MemoApp.UI.MauiApp.Utilities;

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
    private GameSession? _gameSession;

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

    public TrainingSessionViewModel(ILocalizationService localizationService)
    {
        _localizationService = localizationService ?? throw new ArgumentNullException(nameof(localizationService));
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
    private void StartSession()
    {
        if (_gameSession == null) return;

        _gameSession.StartSession();
        UpdateSessionStatus();
        IsSessionActive = true;
        IsSessionCompleted = false;
    }

    [RelayCommand]
    private void AdvanceToNext()
    {
        if (_gameSession == null || !IsSessionActive) return;

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

    private void CompleteSession()
    {
        if (_gameSession == null) return;

        IsSessionActive = false;
        IsSessionCompleted = true;
        SessionStatistics = _gameSession.GetStatistics();
        Title = "Session Complete!";
    }
}