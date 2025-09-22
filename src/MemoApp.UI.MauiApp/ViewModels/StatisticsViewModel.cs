using System.Collections.ObjectModel;
using System.Linq;
using System.Text.Json;
using System.Web;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MemoApp.Core.MajorSystem;
using MemoApp.Localization.Services;
using MemoApp.UI.MauiApp.Utilities;

namespace MemoApp.UI.MauiApp.ViewModels;

/// <summary>
/// ViewModel for displaying detailed session statistics.
/// Shows performance data with responsive CollectionView layouts.
/// Uses navigation parameters for receiving statistics data.
/// </summary>
[QueryProperty(nameof(StatisticsJson), "statisticsJson")]
[QueryProperty(nameof(ReturnToSummary), "returnToSummary")]
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
    private string fastPercentageText = "";

    [ObservableProperty]
    private string mediumPercentageText = "";

    [ObservableProperty]
    private string slowPercentageText = "";

    [ObservableProperty]
    private string consistencyScoreText = "";

    [ObservableProperty]
    private bool returnToSummary;

    [ObservableProperty]
    private bool isAllNumbersExpanded = false;

    [ObservableProperty]
    private string allNumbersExpandButtonText = "▼";

    private string? _statisticsJson;
    public string? StatisticsJson
    {
        get => _statisticsJson;
        set
        {
            _statisticsJson = value;
            if (!string.IsNullOrEmpty(value))
            {
                try
                {
                    var decodedJson = HttpUtility.UrlDecode(value);
                    var stats = JsonSerializer.Deserialize<SessionStatistics>(decodedJson);

                    // For structs, we need to check if the deserialized data is valid
                    // by checking if it has meaningful data (e.g., TotalNumbers > 0)
                    if (stats.TotalNumbers > 0 && stats.AllPerformances?.Count > 0)
                    {
                        Statistics = stats;
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("Deserialized statistics has no valid data");
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to deserialize statistics: {ex.Message}");
                }
            }
        }
    }

    public StatisticsViewModel(ILocalizationService localizationService)
    {
        _localizationService = localizationService ?? throw new ArgumentNullException(nameof(localizationService));
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
        if (ReturnToSummary)
        {
            // When returning to summary, pass a flag to indicate we should preserve state
            await Shell.Current.GoToAsync("..", new Dictionary<string, object>
            {
                ["preserveState"] = true
            });
        }
        else
        {
            await Shell.Current.GoToAsync("..");
        }
    }

    [RelayCommand]
    private async Task StartNewSession()
    {
        // Navigate back to main page
        await Shell.Current.GoToAsync("//main");
    }

    [RelayCommand]
    private void ToggleAllNumbersExpansion()
    {
        IsAllNumbersExpanded = !IsAllNumbersExpanded;
        AllNumbersExpandButtonText = IsAllNumbersExpanded ? "▲" : "▼";
    }

    private void LoadStatisticsData()
    {
        if (Statistics == null) return;

        // Format summary statistics
        TotalDurationText = $"{Statistics.Value.TotalDuration:mm\\:ss}";
        AverageTimeText = $"{Statistics.Value.AverageResponseTime:ss\\.ff}s";

        // Calculate response time distribution
        CalculateResponseTimeDistribution();

        // Calculate consistency score
        CalculateConsistencyScore();

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

    private void CalculateResponseTimeDistribution()
    {
        if (Statistics == null || !Statistics.Value.AllPerformances.Any()) return;

        var allPerformances = Statistics.Value.AllPerformances;
        var totalCount = allPerformances.Count;

        // Count performances by category (matching our indicator thresholds)
        var fastCount = allPerformances.Count(p => p.ResponseTime.TotalSeconds < 1.5);
        var mediumCount = allPerformances.Count(p => p.ResponseTime.TotalSeconds >= 1.5 && p.ResponseTime.TotalSeconds <= 4.0);
        var slowCount = allPerformances.Count(p => p.ResponseTime.TotalSeconds > 4.0);

        // Calculate percentages
        var fastPercentage = Math.Round((fastCount * 100.0) / totalCount);
        var mediumPercentage = Math.Round((mediumCount * 100.0) / totalCount);
        var slowPercentage = Math.Round((slowCount * 100.0) / totalCount);

        // Format text
        FastPercentageText = $"{fastPercentage}%";
        MediumPercentageText = $"{mediumPercentage}%";
        SlowPercentageText = $"{slowPercentage}%";
    }

    private void CalculateConsistencyScore()
    {
        if (Statistics == null || !Statistics.Value.AllPerformances.Any()) return;

        var responseTimes = Statistics.Value.AllPerformances.Select(p => p.ResponseTime.TotalSeconds).ToArray();

        if (responseTimes.Length < 2)
        {
            ConsistencyScoreText = "Perfect";
            return;
        }

        // Calculate standard deviation
        var mean = responseTimes.Average();
        var variance = responseTimes.Sum(rt => Math.Pow(rt - mean, 2)) / responseTimes.Length;
        var standardDeviation = Math.Sqrt(variance);

        // Convert to consistency score (lower std dev = higher consistency)
        string consistencyLevel;
        if (standardDeviation < 0.5)
            consistencyLevel = "Excellent";
        else if (standardDeviation < 1.0)
            consistencyLevel = "Good";
        else if (standardDeviation < 2.0)
            consistencyLevel = "Fair";
        else
            consistencyLevel = "Needs Work";

        ConsistencyScoreText = $"{consistencyLevel} (±{standardDeviation:F1}s)";
    }
}