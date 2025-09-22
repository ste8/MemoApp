using System.Collections.ObjectModel;
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
    private string fastestNumberText = "";

    [ObservableProperty]
    private string slowestNumberText = "";

    [ObservableProperty]
    private bool returnToSummary;

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