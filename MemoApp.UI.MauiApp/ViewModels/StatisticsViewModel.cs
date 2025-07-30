using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MemoApp.Core.MajorSystem;
using MemoApp.Localization.Services;
using MemoApp.UI.MauiApp.Utilities;

namespace MemoApp.UI.MauiApp.ViewModels;

/// <summary>
/// ViewModel for displaying detailed session statistics.
/// Shows performance data with responsive CollectionView layouts.
/// </summary>
public partial class StatisticsViewModel : BaseViewModel
{
    private readonly ILocalizationService _localizationService;
    [ObservableProperty]
    private SessionStatistics? statistics;

    [ObservableProperty]
    private ObservableCollection<NumberPerformanceDisplay> allPerformances = new();

    [ObservableProperty]
    private ObservableCollection<NumberPerformanceDisplay> slowestPerformances = new();

    [ObservableProperty]
    private string totalDurationText = "";

    [ObservableProperty]
    private string averageTimeText = "";

    [ObservableProperty]
    private string fastestNumberText = "";

    [ObservableProperty]
    private string slowestNumberText = "";

    private static SessionStatistics? _pendingStatistics;
    
    public StatisticsViewModel(ILocalizationService localizationService)
    {
        _localizationService = localizationService ?? throw new ArgumentNullException(nameof(localizationService));
        Title = "Session Statistics";
        
        // Check if there are pending statistics to load
        if (_pendingStatistics.HasValue)
        {
            Statistics = _pendingStatistics;
            _pendingStatistics = null;
        }
    }
    
    public static void SetPendingStatistics(SessionStatistics statistics)
    {
        _pendingStatistics = statistics;
    }

    partial void OnStatisticsChanged(SessionStatistics? value)
    {
        if (value == null) return;

        LoadStatisticsData();
    }

    [RelayCommand]
    private async Task GoBack()
    {
        await Shell.Current.GoToAsync("..");
    }

    [RelayCommand]
    private async Task StartNewSession()
    {
        // Navigate back to main page
        await Shell.Current.GoToAsync("//main");
    }

    private void LoadStatisticsData()
    {
        if (Statistics == null) return;

        // Format summary statistics
        TotalDurationText = $"{Statistics.Value.TotalDuration:mm\\:ss}";
        AverageTimeText = $"{Statistics.Value.AverageResponseTime:ss\\.ff}s";
        FastestNumberText = $"{NumberFormatHelper.FormatNumber(Statistics.Value.FastestResponse.Number, _localizationService)} ({Statistics.Value.FastestResponse.ResponseTime:ss\\.ff}s)";
        SlowestNumberText = $"{NumberFormatHelper.FormatNumber(Statistics.Value.SlowestResponse.Number, _localizationService)} ({Statistics.Value.SlowestResponse.ResponseTime:ss\\.ff}s)";

        // Load all performances for detailed view
        AllPerformances.Clear();
        foreach (var performance in Statistics.Value.AllPerformances)
        {
            AllPerformances.Add(new NumberPerformanceDisplay(performance, _localizationService));
        }

        // Load slowest performances for focus areas
        SlowestPerformances.Clear();
        foreach (var performance in Statistics.Value.SlowestResponses)
        {
            SlowestPerformances.Add(new NumberPerformanceDisplay(performance, _localizationService));
        }
    }
}