using System;
using System.Globalization;
using MemoApp.Localization.Services;

namespace MemoApp.UI.MauiApp.Services.FlashNumbers;

/// <summary>
/// Default implementation of Flash Numbers performance threshold service.
/// Centralizes all Flash Numbers performance categorization logic.
/// </summary>
public class FlashNumbersPerformanceService : IFlashNumbersPerformanceService
{
    private readonly ILocalizationService _localizationService;

    // Single source of truth for performance thresholds
    private const double FAST_THRESHOLD_SECONDS = 1.0;   // Changed from 1.5
    private const double SLOW_THRESHOLD_SECONDS = 3.0;   // Changed from 4.0

    public TimeSpan FastThreshold => TimeSpan.FromSeconds(FAST_THRESHOLD_SECONDS);
    public TimeSpan SlowThreshold => TimeSpan.FromSeconds(SLOW_THRESHOLD_SECONDS);

    public FlashNumbersPerformanceService(ILocalizationService localizationService)
    {
        _localizationService = localizationService ?? throw new ArgumentNullException(nameof(localizationService));
    }

    public PerformanceCategory CategorizeResponseTime(TimeSpan responseTime)
    {
        var seconds = responseTime.TotalSeconds;

        return seconds switch
        {
            < FAST_THRESHOLD_SECONDS => PerformanceCategory.Fast,
            <= SLOW_THRESHOLD_SECONDS => PerformanceCategory.Medium,
            _ => PerformanceCategory.Slow
        };
    }

    public string GetThresholdDisplayText(PerformanceCategory category)
    {
        // Use current culture to determine decimal separator
        var culture = CultureInfo.CurrentCulture;
        var isItalian = culture.TwoLetterISOLanguageName == "it";
        var decimalSeparator = isItalian ? "," : ".";

        return category switch
        {
            PerformanceCategory.Fast =>
                $"(< {FAST_THRESHOLD_SECONDS.ToString("0.#", CultureInfo.InvariantCulture).Replace(".", decimalSeparator)}s)",

            PerformanceCategory.Medium =>
                $"({FAST_THRESHOLD_SECONDS.ToString("0.#", CultureInfo.InvariantCulture).Replace(".", decimalSeparator)}-" +
                $"{SLOW_THRESHOLD_SECONDS.ToString("0.#", CultureInfo.InvariantCulture).Replace(".", decimalSeparator)}s)",

            PerformanceCategory.Slow =>
                $"(> {SLOW_THRESHOLD_SECONDS.ToString("0.#", CultureInfo.InvariantCulture).Replace(".", decimalSeparator)}s)",

            _ => throw new ArgumentOutOfRangeException(nameof(category))
        };
    }
}