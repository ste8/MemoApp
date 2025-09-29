using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MemoApp.Core.MajorSystem;
using MemoApp.Localization.Services;
using Microsoft.Extensions.Logging;

namespace MemoApp.UI.MauiApp.ViewModels.Onboarding;

public partial class OnboardingNumberFormatViewModel : BaseViewModel
{
    private readonly ILocalizationService _localizationService;
    private readonly ILogger<OnboardingNumberFormatViewModel> _logger;

    [ObservableProperty]
    private bool isPaddedFormatSelected = true; // Default to recommended

    [ObservableProperty]
    private bool isNaturalFormatSelected;

    public OnboardingNumberFormatViewModel(
        ILocalizationService localizationService,
        ILogger<OnboardingNumberFormatViewModel> logger)
    {
        _localizationService = localizationService;
        _logger = logger;
        Title = localizationService.GetString("OnboardingNumberFormat_Title");

        // Initialize with current number format preference (if any)
        InitializeCurrentSelection();
    }

    private void InitializeCurrentSelection()
    {
        try
        {
            var currentFormat = _localizationService.CurrentNumberFormat;
            IsPaddedFormatSelected = currentFormat == NumberFormat.Padded;
            IsNaturalFormatSelected = currentFormat == NumberFormat.Natural;
            _logger.LogInformation("Initialized number format selection with current preference: {Format}", currentFormat);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize number format selection, using defaults");
            // Keep defaults: Padded = true, Natural = false
        }
    }

    [RelayCommand]
    private void SelectPaddedFormat()
    {
        _logger.LogInformation("User selected padded number format (00-99)");
        IsPaddedFormatSelected = true;
        IsNaturalFormatSelected = false;

        // Save the preference
        SaveNumberFormatPreference(NumberFormat.Padded);
    }

    [RelayCommand]
    private void SelectNaturalFormat()
    {
        _logger.LogInformation("User selected natural number format (0-99)");
        IsPaddedFormatSelected = false;
        IsNaturalFormatSelected = true;

        // Save the preference
        SaveNumberFormatPreference(NumberFormat.Natural);
    }

    private async void SaveNumberFormatPreference(NumberFormat format)
    {
        try
        {
            // Save through localization service to ensure consistency with settings
            _localizationService.SetNumberFormat(format);
            await _localizationService.SaveNumberFormatPreferenceAsync();
            _logger.LogInformation("Number format preference saved: {Format}", format);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save number format preference");
        }
    }

    public NumberFormat GetSelectedFormat()
    {
        return IsPaddedFormatSelected ? NumberFormat.Padded : NumberFormat.Natural;
    }
}