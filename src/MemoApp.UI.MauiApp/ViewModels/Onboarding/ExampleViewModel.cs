using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using MemoApp.Localization.Services;
using MemoApp.UI.MauiApp.Models.Onboarding;
using MemoApp.UI.MauiApp.Services.Onboarding;
using Microsoft.Extensions.Logging;

namespace MemoApp.UI.MauiApp.ViewModels.Onboarding;

/// <summary>
/// ViewModel for the Major System example demonstration view
/// </summary>
public partial class ExampleViewModel : ObservableObject
{
    private readonly IMajorSystemOnboardingService _onboardingService;
    private readonly ILocalizationService _localizationService;
    private readonly ILogger<ExampleViewModel> _logger;

    [ObservableProperty]
    private MajorSystemExample? _example;

    [ObservableProperty]
    private string _rememberText = string.Empty;

    [ObservableProperty]
    private bool _isAnimating;

    public ExampleViewModel(
        IMajorSystemOnboardingService onboardingService, 
        ILocalizationService localizationService,
        ILogger<ExampleViewModel> logger)
    {
        _onboardingService = onboardingService;
        _localizationService = localizationService;
        _logger = logger;

        LoadExample();
        LoadLocalizedText();
        
        // Subscribe to culture changes
        _localizationService.CultureChanged += OnCultureChanged;
    }

    /// <summary>
    /// Loads the appropriate example for the current language
    /// </summary>
    public void LoadExample()
    {
        try
        {
            Example = _onboardingService.GetExample();
            _logger.LogDebug("Loaded example for number {Number} -> {Word}", Example.Number, Example.Word);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load Major System example");
            // Fallback to English example
            Example = new MajorSystemExample
            {
                Number = 41,
                Word = "RAT",
                Emoji = "🐀",
                RememberTextKey = "Onboarding_Example_Remember",
                ConversionSteps = new List<ConversionStep>
                {
                    new() { Digit = 4, Letter = "R" },
                    new() { Digit = 1, Letter = "T" }
                }
            };
        }
    }

    /// <summary>
    /// Loads the localized remember text
    /// </summary>
    public void LoadLocalizedText()
    {
        try
        {
            if (Example != null)
            {
                RememberText = _localizationService.GetString(Example.RememberTextKey);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load localized text");
            RememberText = "Remember this image!";
        }
    }

    /// <summary>
    /// Handles culture change events to reload content
    /// </summary>
    private void OnCultureChanged(object? sender, CultureInfo e)
    {
        try
        {
            LoadExample();
            LoadLocalizedText();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to reload content after culture change");
        }
    }

    ~ExampleViewModel()
    {
        _localizationService.CultureChanged -= OnCultureChanged;
    }
}