using System.Collections.ObjectModel;
using System.Linq;
using System.Text.Json;
using System.Web;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MemoApp.Core.MajorSystem;
using MemoApp.Core.Models;
using MemoApp.Localization.Services;
using MemoApp.UI.MauiApp.Services.FlashNumbers;
using MemoApp.UI.MauiApp.Utilities;

namespace MemoApp.UI.MauiApp.ViewModels.FlashNumbers;

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
    private readonly IFlashNumbersPerformanceService _performanceService;

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
    private string allNumbersExpandButtonText = "︾";

    [ObservableProperty]
    private string fastThresholdText = "";

    [ObservableProperty]
    private string mediumThresholdText = "";

    [ObservableProperty]
    private string slowThresholdText = "";

    [ObservableProperty]
    private ShareableResult? sessionShareData;

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

    public StatisticsViewModel(ILocalizationService localizationService, IFlashNumbersPerformanceService performanceService)
    {
        _localizationService = localizationService ?? throw new ArgumentNullException(nameof(localizationService));
        _performanceService = performanceService ?? throw new ArgumentNullException(nameof(performanceService));
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
        AllNumbersExpandButtonText = IsAllNumbersExpanded ? "︽" : "︾";
    }

    private void LoadStatisticsData()
    {
        if (Statistics == null) return;

        // Format summary statistics
        TotalDurationText = $"{Statistics.Value.TotalDuration:mm\\:ss}";
        AverageTimeText = $"{Statistics.Value.AverageResponseTime:ss\\.ff}s";

        // Set threshold display texts
        FastThresholdText = _performanceService.GetThresholdDisplayText(PerformanceCategory.Fast);
        MediumThresholdText = _performanceService.GetThresholdDisplayText(PerformanceCategory.Medium);
        SlowThresholdText = _performanceService.GetThresholdDisplayText(PerformanceCategory.Slow);

        // Calculate response time distribution
        CalculateResponseTimeDistribution();

        // Calculate consistency score

        // Load all performances for detailed view
        AllPerformances.Clear();
        foreach (var performance in Statistics.Value.AllPerformances)
        {
            AllPerformances.Add(new NumberPerformanceDisplay(performance, _localizationService, _performanceService));
        }

        // Load slowest performances for focus areas
        SlowestPerformances.Clear();
        foreach (var performance in Statistics.Value.SlowestResponses)
        {
            SlowestPerformances.Add(new NumberPerformanceDisplay(performance, _localizationService, _performanceService));
        }

        // Create shareable result for the ShareButtonComponent
        SessionShareData = new FlashNumbersShareResult
        {
            ExerciseType = "FlashNumbers",
            Duration = Statistics.Value.TotalDuration,
            TotalNumbers = Statistics.Value.TotalNumbers,
            NumberRange = $"Detailed Statistics", // Could extract range from session data if available
            AverageTimePerNumber = Statistics.Value.AverageResponseTime,
            FastestTime = Statistics.Value.AllPerformances.Any() ? Statistics.Value.AllPerformances.Min(p => p.ResponseTime) : TimeSpan.Zero
        };
    }

    private void CalculateResponseTimeDistribution()
    {
        if (Statistics == null || !Statistics.Value.AllPerformances.Any()) return;

        var allPerformances = Statistics.Value.AllPerformances;
        var totalCount = allPerformances.Count;

        // Count performances by category using centralized thresholds
        var fastCount = allPerformances.Count(p => _performanceService.CategorizeResponseTime(p.ResponseTime) == PerformanceCategory.Fast);
        var mediumCount = allPerformances.Count(p => _performanceService.CategorizeResponseTime(p.ResponseTime) == PerformanceCategory.Medium);
        var slowCount = allPerformances.Count(p => _performanceService.CategorizeResponseTime(p.ResponseTime) == PerformanceCategory.Slow);

        // Calculate percentages
        var fastPercentage = Math.Round((fastCount * 100.0) / totalCount);
        var mediumPercentage = Math.Round((mediumCount * 100.0) / totalCount);
        var slowPercentage = Math.Round((slowCount * 100.0) / totalCount);

        // Format text
        FastPercentageText = $"{fastPercentage}%";
        MediumPercentageText = $"{mediumPercentage}%";
        SlowPercentageText = $"{slowPercentage}%";
    }

}