using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MemoApp.Core.MajorSystem;

namespace MemoApp.UI.MauiApp.ViewModels;

/// <summary>
/// ViewModel for displaying detailed session statistics.
/// Shows performance data with responsive CollectionView layouts.
/// </summary>
[QueryProperty(nameof(Statistics), "statistics")]
public partial class StatisticsViewModel : BaseViewModel
{
    [ObservableProperty]
    private SessionStatistics? statistics;

    [ObservableProperty]
    private ObservableCollection<NumberPerformance> allPerformances = new();

    [ObservableProperty]
    private ObservableCollection<NumberPerformance> slowestPerformances = new();

    [ObservableProperty]
    private string totalDurationText = "";

    [ObservableProperty]
    private string averageTimeText = "";

    [ObservableProperty]
    private string fastestNumberText = "";

    [ObservableProperty]
    private string slowestNumberText = "";

    public StatisticsViewModel()
    {
        Title = "Session Statistics";
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
        FastestNumberText = $"{Statistics.Value.FastestResponse.Number} ({Statistics.Value.FastestResponse.ResponseTime:ss\\.ff}s)";
        SlowestNumberText = $"{Statistics.Value.SlowestResponse.Number} ({Statistics.Value.SlowestResponse.ResponseTime:ss\\.ff}s)";

        // Load all performances for detailed view (use slowest responses as a subset)
        AllPerformances.Clear();
        foreach (var performance in Statistics.Value.SlowestResponses)
        {
            AllPerformances.Add(performance);
        }

        // Load slowest performances for focus areas
        SlowestPerformances.Clear();
        foreach (var performance in Statistics.Value.SlowestResponses.Take(10))
        {
            SlowestPerformances.Add(performance);
        }
    }
}