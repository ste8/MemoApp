using MemoApp.Core.MajorSystem;
using MemoApp.Localization.Services;
using MemoApp.UI.MauiApp.Utilities;

namespace MemoApp.UI.MauiApp.ViewModels;

/// <summary>
/// Wrapper class for NumberPerformance that provides properly formatted number display
/// based on the current localization service settings.
/// </summary>
public class NumberPerformanceDisplay
{
    private readonly NumberPerformance _performance;
    private readonly ILocalizationService _localizationService;

    public NumberPerformanceDisplay(NumberPerformance performance, ILocalizationService localizationService)
    {
        _performance = performance;
        _localizationService = localizationService;
    }

    /// <summary>
    /// Gets the formatted number display based on current number format setting.
    /// </summary>
    public string Number => NumberFormatHelper.FormatNumber(_performance.Number, _localizationService);

    /// <summary>
    /// Gets the response time.
    /// </summary>
    public TimeSpan ResponseTime => _performance.ResponseTime;

    /// <summary>
    /// Gets the formatted response time for display.
    /// </summary>
    public string FormattedResponseTime
    {
        get
        {
            var timeSpan = _performance.ResponseTime;

            // Format: "2.45s" for times under 60 seconds
            if (timeSpan.TotalSeconds < 60)
            {
                return $"{timeSpan.TotalSeconds:F2}s";
            }

            // Format: "1:23.45" for longer times
            return $"{timeSpan:mm\\:ss\\.ff}";
        }
    }

    /// <summary>
    /// Gets the performance indicator shape based on response time.
    /// Fast (< 1.5s): Circle ●, Medium (1.5-4s): Triangle ▲, Slow (> 4s): Square ■
    /// </summary>
    public string PerformanceIndicator
    {
        get
        {
            var seconds = _performance.ResponseTime.TotalSeconds;
            return seconds switch
            {
                < 1.5 => "●",    // Fast
                <= 4.0 => "▲",   // Medium
                _ => "■"         // Slow
            };
        }
    }

    /// <summary>
    /// Gets the performance indicator color based on response time.
    /// </summary>
    public Color PerformanceColor
    {
        get
        {
            var seconds = _performance.ResponseTime.TotalSeconds;
            return seconds switch
            {
                < 1.5 => Color.FromArgb("#10B981"),  // Fast - Green
                <= 4.0 => Color.FromArgb("#F59E0B"), // Medium - Yellow/Orange
                _ => Color.FromArgb("#EF4444")       // Slow - Red
            };
        }
    }

    /// <summary>
    /// Gets the accessibility description for the performance indicator.
    /// </summary>
    public string PerformanceDescription
    {
        get
        {
            var seconds = _performance.ResponseTime.TotalSeconds;
            return seconds switch
            {
                < 1.5 => _localizationService.GetString("Statistics_FastIndicator"),
                <= 4.0 => _localizationService.GetString("Statistics_MediumIndicator"),
                _ => _localizationService.GetString("Statistics_SlowIndicator")
            };
        }
    }

    /// <summary>
    /// Gets the presentation timestamp.
    /// </summary>
    public DateTime PresentedAt => _performance.PresentedAt;

    /// <summary>
    /// Gets the response timestamp.
    /// </summary>
    public DateTime RespondedAt => _performance.RespondedAt;

    /// <summary>
    /// Gets the underlying NumberPerformance object.
    /// </summary>
    public NumberPerformance Performance => _performance;
}