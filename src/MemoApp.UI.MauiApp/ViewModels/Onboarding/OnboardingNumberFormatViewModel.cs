using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MemoApp.Core.MajorSystem;
using MemoApp.Localization.Services;
using Microsoft.Extensions.Logging;

namespace MemoApp.UI.MauiApp.ViewModels.Onboarding;

public partial class OnboardingNumberFormatViewModel : BaseViewModel
{
    private readonly ILogger<OnboardingNumberFormatViewModel> _logger;

    [ObservableProperty]
    private bool isPaddedFormatSelected = true; // Default to recommended

    [ObservableProperty]
    private bool isNaturalFormatSelected;

    public OnboardingNumberFormatViewModel(
        ILocalizationService localizationService,
        ILogger<OnboardingNumberFormatViewModel> logger)
    {
        _logger = logger;
        Title = localizationService.GetString("OnboardingNumberFormat_Title");
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

    private void SaveNumberFormatPreference(NumberFormat format)
    {
        try
        {
            // Save to preferences
            Preferences.Set("NumberFormat", format.ToString());
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