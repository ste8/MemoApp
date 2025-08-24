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