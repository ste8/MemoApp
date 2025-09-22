using MemoApp.Core.MajorSystem;
using MemoApp.Localization.Services;
using MemoApp.UI.MauiApp.Services.FlashNumbers;
using MemoApp.UI.MauiApp.Utilities;
using Microsoft.Maui.Graphics;

namespace MemoApp.UI.MauiApp.ViewModels;

/// <summary>
/// Wrapper class for NumberPerformance that provides properly formatted number display
/// based on the current localization service settings.
/// </summary>
public class NumberPerformanceDisplay
{
    private readonly NumberPerformance _performance;
    private readonly ILocalizationService _localizationService;
    private readonly IFlashNumbersPerformanceService _performanceService;

    public NumberPerformanceDisplay(NumberPerformance performance, ILocalizationService localizationService, IFlashNumbersPerformanceService performanceService)
    {
        _performance = performance;
        _localizationService = localizationService;
        _performanceService = performanceService;
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
    /// Fast (< 1.0s): Circle ●, Medium (1.0-3.0s): Triangle ▲, Slow (> 3.0s): Square ■
    /// </summary>
    public string PerformanceIndicator
    {
        get
        {
            var category = _performanceService.CategorizeResponseTime(_performance.ResponseTime);
            return category switch
            {
                PerformanceCategory.Fast => "●",    // Fast - Circle
                PerformanceCategory.Medium => "▲",   // Medium - Triangle
                PerformanceCategory.Slow => "■",     // Slow - Square
                _ => "?"
            };
        }
    }

    /// <summary>
    /// Gets the performance indicator color based on response time.
    /// Uses theme-aware colors for optimal contrast and accessibility.
    /// </summary>
    public Color PerformanceColor
    {
        get
        {
            var category = _performanceService.CategorizeResponseTime(_performance.ResponseTime);
            return category switch
            {
                PerformanceCategory.Fast => GetThemeColor("PerformanceFast"),   // Fast - Green
                PerformanceCategory.Medium => GetMediumPerformanceColor(),       // Medium - Yellow
                PerformanceCategory.Slow => GetThemeColor("PerformanceSlow"),    // Slow - Red
                _ => GetThemeColor("Gray500")
            };
        }
    }

    /// <summary>
    /// Gets the theme-aware medium performance color with optimal contrast.
    /// </summary>
    private Color GetMediumPerformanceColor()
    {
        // Use theme-aware colors for optimal contrast against panel backgrounds
        var isDarkTheme = Application.Current?.RequestedTheme == AppTheme.Dark;
        return GetThemeColor(isDarkTheme ? "PerformanceMediumDark" : "PerformanceMediumLight");
    }

    /// <summary>
    /// Gets a color from the application's resource dictionary.
    /// </summary>
    private static Color GetThemeColor(string colorKey)
    {
        if (Application.Current?.Resources.TryGetValue(colorKey, out var color) == true && color is Color themeColor)
        {
            return themeColor;
        }

        // Fallback colors if resource lookup fails
        return colorKey switch
        {
            "PerformanceFast" => Color.FromArgb("#10B981"),      // Emerald-500
            "PerformanceMediumLight" => Color.FromArgb("#CC5500"), // Deep Orange
            "PerformanceMediumDark" => Color.FromArgb("#FF7F00"),  // Bright Orange
            "PerformanceSlow" => Color.FromArgb("#EF4444"),        // Red-500
            _ => Color.FromArgb("#6B7280")                         // Gray fallback
        };
    }

    /// <summary>
    /// Gets the accessibility description for the performance indicator.
    /// </summary>
    public string PerformanceDescription
    {
        get
        {
            var category = _performanceService.CategorizeResponseTime(_performance.ResponseTime);
            return category switch
            {
                PerformanceCategory.Fast => _localizationService.GetString("Statistics_FastIndicator"),
                PerformanceCategory.Medium => _localizationService.GetString("Statistics_MediumIndicator"),
                PerformanceCategory.Slow => _localizationService.GetString("Statistics_SlowIndicator"),
                _ => _localizationService.GetString("Statistics_UnknownIndicator")
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