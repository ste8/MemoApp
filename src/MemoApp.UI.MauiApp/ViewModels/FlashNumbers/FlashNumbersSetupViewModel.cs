using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MemoApp.Core.MajorSystem;
using MemoApp.Core.FlashNumbers;
using MemoApp.Localization.Services;
using MemoApp.UI.MauiApp.Services;
using MemoApp.UI.MauiApp.Services.Freemium;
using MemoApp.UI.MauiApp.Constants;
using MemoApp.UI.MauiApp.Utilities;
using Microsoft.Extensions.Logging;
using System.Globalization;

namespace MemoApp.UI.MauiApp.ViewModels.FlashNumbers;

/// <summary>
/// ViewModel for Flash Numbers exercise setup page.
/// Handles training configuration, range selection, and navigation to Flash Numbers exercise.
/// </summary>
public partial class FlashNumbersSetupViewModel : BaseViewModel
{
    private readonly ILocalizationService _localizationService;
    private readonly IUserNotificationService _userNotificationService;
    private readonly IFlashNumbersSettingsService _settingsService;
    private readonly ILogger<FlashNumbersSetupViewModel> _logger;
    private readonly IInterstitialAdManager? _interstitialAdManager;

    public FlashNumbersSetupViewModel(
        ILocalizationService localizationService,
        IUserNotificationService userNotificationService,
        IFlashNumbersSettingsService settingsService,
        ILogger<FlashNumbersSetupViewModel> logger,
        IInterstitialAdManager? interstitialAdManager = null)
    {
        _localizationService = localizationService;
        _userNotificationService = userNotificationService;
        _settingsService = settingsService;
        _logger = logger;
        _interstitialAdManager = interstitialAdManager;
        
        _logger.LogInformation("FlashNumbersSetupViewModel constructor called");
        
        Title = _localizationService.GetString("Tab_FlashNumbers");
        
        LoadTrainingOptions();
        LoadNumberOptions();
        
        _logger.LogInformation($"Loaded {TrainingOptions.Count} training options and {NumberOptions.Count} number options");
        
        // Subscribe to culture and number format changes
        _localizationService.CultureChanged += OnCultureChanged;
        _localizationService.NumberFormatChanged += OnNumberFormatChanged;
        
        // Initialize dropdown selected values
        InitializeDropdownValues();

        // Initialize quick training button text
        UpdateQuickTrainingButtonText();

        // Load saved custom range settings
        LoadCustomRangeSettingsAsync();
        
        _logger.LogInformation($"QuickTrainingButtonText set to: {QuickTrainingButtonText}");
    }

    #region Properties
    
    [ObservableProperty]
    private ObservableCollection<TrainingOption> trainingOptions = new();

    [ObservableProperty]
    private ObservableCollection<string> numberOptions = new();

    [ObservableProperty]
    private List<MajorNumber> majorNumbers = new();

    [ObservableProperty]
    private string customRangeStart = "00";

    [ObservableProperty]
    private string customRangeEnd = "09";

    [ObservableProperty]
    private int selectedStartIndex = 0; // "00"

    [ObservableProperty]
    private int selectedEndIndex = 9; // "09"

    [ObservableProperty]
    private string validationMessage = string.Empty;

    [ObservableProperty]
    private bool hasValidationError = false;

    [ObservableProperty]
    private bool isStartDropdownVisible = false;

    [ObservableProperty]
    private bool isEndDropdownVisible = false;

    [ObservableProperty]
    private string? selectedStartNumberOption;

    [ObservableProperty]
    private string? selectedEndNumberOption;

    [ObservableProperty]
    private string quickTrainingButtonText = string.Empty;

    #endregion

    #region Commands

    [RelayCommand]
    private async Task StartQuickTraining()
    {
        // Prepare and show interstitial ad before starting exercise
        if (_interstitialAdManager != null)
        {
            await _interstitialAdManager.PrepareAdForExerciseAsync(ExerciseTypes.FlashNumbers);
            await _interstitialAdManager.ShowAdIfNeededAsync(ExerciseTypes.FlashNumbers);
        }

        var numberFormat = _localizationService.CurrentNumberFormat;

        var parameters = new Dictionary<string, object>
        {
            // In padded mode, use "00-09" (zero-prefixed numbers only)
            // In natural mode, use "0-9" (regular numbers)
            { "rangeStart", numberFormat == NumberFormat.Padded ? "00" : "0" },
            { "rangeEnd", numberFormat == NumberFormat.Padded ? "09" : "9" }
        };

        try
        {
            await Shell.Current.GoToAsync("flashnumbers", parameters);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to navigate to Flash Numbers exercise");
            await _userNotificationService.ShowErrorToastAsync(
                _localizationService.GetString("Navigation_Failed"));
        }
    }

    [RelayCommand]
    private async Task StartCustomTraining()
    {
        if (IsRangeValid())
        {
            // Prepare and show interstitial ad before starting exercise
            if (_interstitialAdManager != null)
            {
                await _interstitialAdManager.PrepareAdForExerciseAsync(ExerciseTypes.FlashNumbers);
                await _interstitialAdManager.ShowAdIfNeededAsync(ExerciseTypes.FlashNumbers);
            }

            var parameters = new Dictionary<string, object>
            {
                { "rangeStart", CustomRangeStart },
                { "rangeEnd", CustomRangeEnd }
            };

            await Shell.Current.GoToAsync("flashnumbers", parameters);
        }
        else
        {
            ValidationMessage = _localizationService.GetString("Validation_RangeError");
            HasValidationError = true;
        }
    }

    [RelayCommand]
    private async Task StartPresetTraining(TrainingOption option)
    {
        // Prepare and show interstitial ad before starting exercise
        if (_interstitialAdManager != null)
        {
            await _interstitialAdManager.PrepareAdForExerciseAsync(ExerciseTypes.FlashNumbers);
            await _interstitialAdManager.ShowAdIfNeededAsync(ExerciseTypes.FlashNumbers);
        }

        var parameters = new Dictionary<string, object>
        {
            { "rangeStart", option.RangeStart },
            { "rangeEnd", option.RangeEnd }
        };

        try
        {
            await Shell.Current.GoToAsync("flashnumbers", parameters);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to navigate to Flash Numbers exercise");
            await _userNotificationService.ShowErrorToastAsync(
                _localizationService.GetString("Navigation_Failed"));
        }
    }

    [RelayCommand]
    private void ToggleStartDropdown()
    {
        IsEndDropdownVisible = false; // Close end dropdown if open
        IsStartDropdownVisible = !IsStartDropdownVisible;
    }

    [RelayCommand]
    private void ToggleEndDropdown()
    {
        IsStartDropdownVisible = false; // Close start dropdown if open
        IsEndDropdownVisible = !IsEndDropdownVisible;
    }

    [RelayCommand]
    private void SelectStartNumberOption(string option)
    {
        if (!string.IsNullOrEmpty(option))
        {
            var selectedIndex = NumberOptions.IndexOf(option);
            if (selectedIndex >= 0)
            {
                SelectedStartIndex = selectedIndex;
                SelectedStartNumberOption = option;
            }
        }
        IsStartDropdownVisible = false;
    }

    [RelayCommand]
    private void SelectEndNumberOption(string option)
    {
        if (!string.IsNullOrEmpty(option))
        {
            var selectedIndex = NumberOptions.IndexOf(option);
            if (selectedIndex >= 0)
            {
                SelectedEndIndex = selectedIndex;
                SelectedEndNumberOption = option;
            }
        }
        IsEndDropdownVisible = false;
    }

    [RelayCommand]
    private async Task SelectStartNumber()
    {
        await ShowNumberSelectionDialog(true);
    }

    [RelayCommand]
    private async Task SelectEndNumber()
    {
        await ShowNumberSelectionDialog(false);
    }

    [RelayCommand]
    private async Task ShowHelp()
    {
        try
        {
            await Shell.Current.GoToAsync("help");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to navigate to help page");
            await _userNotificationService.ShowErrorToastAsync(
                _localizationService.GetString("Navigation_Failed"));
        }
    }

    #endregion

    #region Private Methods

    private void InitializeDropdownValues()
    {
        if (NumberOptions.Count > 0)
        {
            SelectedStartNumberOption = NumberOptions.ElementAtOrDefault(SelectedStartIndex);
            SelectedEndNumberOption = NumberOptions.ElementAtOrDefault(SelectedEndIndex);
        }
    }

    private async Task ShowNumberSelectionDialog(bool isStartNumber)
    {
        try
        {
            var options = NumberOptions.ToArray();
            var title = isStartNumber ? 
                _localizationService.GetString("MainPage_StartNumber") : 
                _localizationService.GetString("MainPage_EndNumber");
            var cancel = _localizationService.GetString("Common_Cancel");
            
            var result = await Shell.Current.DisplayActionSheet(title, cancel, null, options);

            if (result != null && result != cancel)
            {
                var selectedIndex = NumberOptions.IndexOf(result);
                if (selectedIndex >= 0)
                {
                    if (isStartNumber)
                    {
                        SelectedStartIndex = selectedIndex;
                    }
                    else
                    {
                        SelectedEndIndex = selectedIndex;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Number selection error");
            await _userNotificationService.ShowErrorToastAsync(
                _localizationService.GetString("Settings_SaveFailed"));
        }
    }

    private void LoadTrainingOptions()
    {
        var numberFormat = _localizationService.CurrentNumberFormat;
        
        TrainingOptions.Clear();
        
        if (numberFormat == NumberFormat.Padded)
        {
            // Preset ranges starting from 00
            TrainingOptions.Add(new("00-19", "00", "19", "Numbers 00 to 19"));
            TrainingOptions.Add(new("00-29", "00", "29", "Numbers 00 to 29"));
            TrainingOptions.Add(new("00-39", "00", "39", "Numbers 00 to 39"));
            TrainingOptions.Add(new("00-49", "00", "49", "Numbers 00 to 49"));
            TrainingOptions.Add(new("00-59", "00", "59", "Numbers 00 to 59"));
            TrainingOptions.Add(new("00-69", "00", "69", "Numbers 00 to 69"));
            TrainingOptions.Add(new("00-79", "00", "79", "Numbers 00 to 79"));
            TrainingOptions.Add(new("00-89", "00", "89", "Numbers 00 to 89"));
            TrainingOptions.Add(new("00-99", "00", "99", "Complete Major System 00 to 99"));
        }
        else // Natural format
        {
            // Preset ranges starting from 0
            TrainingOptions.Add(new("0-19", "0", "19", "Numbers 0 to 19"));
            TrainingOptions.Add(new("0-29", "0", "29", "Numbers 0 to 29"));
            TrainingOptions.Add(new("0-39", "0", "39", "Numbers 0 to 39"));
            TrainingOptions.Add(new("0-49", "0", "49", "Numbers 0 to 49"));
            TrainingOptions.Add(new("0-59", "0", "59", "Numbers 0 to 59"));
            TrainingOptions.Add(new("0-69", "0", "69", "Numbers 0 to 69"));
            TrainingOptions.Add(new("0-79", "0", "79", "Numbers 0 to 79"));
            TrainingOptions.Add(new("0-89", "0", "89", "Numbers 0 to 89"));
            TrainingOptions.Add(new("0-99", "0", "99", "Complete Major System 0 to 99"));
        }
    }

    private void LoadNumberOptions()
    {
        NumberOptions.Clear();
        MajorNumbers.Clear();

        // Get numbers based on current format setting
        var allNumbers = NumberFormatHelper.GetAllNumbersInOrder(_localizationService).ToList();
        
        foreach (var majorNumber in allNumbers)
        {
            MajorNumbers.Add(majorNumber);
            NumberOptions.Add(NumberFormatHelper.FormatNumber(majorNumber, _localizationService));
        }
    }

    partial void OnSelectedStartIndexChanged(int value)
    {
        if (value >= 0 && value < MajorNumbers.Count)
        {
            CustomRangeStart = MajorNumbers[value].Display;
            UpdateValidation();
            SaveCustomRangeSettingsAsync();
        }
    }

    partial void OnSelectedEndIndexChanged(int value)
    {
        if (value >= 0 && value < MajorNumbers.Count)
        {
            CustomRangeEnd = MajorNumbers[value].Display;
            UpdateValidation();
            SaveCustomRangeSettingsAsync();
        }
    }

    private void UpdateValidation()
    {
        if (IsRangeValid())
        {
            ClearValidationError();
        }
        else
        {
            ValidationMessage = _localizationService.GetString("Validation_RangeError");
            HasValidationError = true;
        }
    }

    private bool IsRangeValid()
    {
        // Check if indexes are within bounds and start <= end
        return SelectedStartIndex >= 0 && SelectedEndIndex >= 0 && 
               SelectedStartIndex < MajorNumbers.Count && SelectedEndIndex < MajorNumbers.Count &&
               SelectedStartIndex <= SelectedEndIndex;
    }

    private void ClearValidationError()
    {
        HasValidationError = false;
        ValidationMessage = string.Empty;
    }

    private void OnCultureChanged(object? sender, CultureInfo culture)
    {
        // Update UI when culture changes
        MainThread.BeginInvokeOnMainThread(() =>
        {
            // Update validation message if there's an error
            if (HasValidationError)
            {
                ValidationMessage = _localizationService.GetString("Validation_RangeError");
            }
            
            // Update quick training button text for new language
            UpdateQuickTrainingButtonText();
        });
    }

    private void OnNumberFormatChanged(object? sender, NumberFormat numberFormat)
    {
        // Update UI when number format changes
        MainThread.BeginInvokeOnMainThread(() =>
        {
            // Reload number options with new format
            LoadNumberOptions();
            
            // Reset selected indexes to default (first items)
            SelectedStartIndex = 0;
            SelectedEndIndex = Math.Min(9, NumberOptions.Count - 1);
            
            // Update training options to reflect new format
            LoadTrainingOptions();
            
            // Update quick training button text
            UpdateQuickTrainingButtonText();
        });
    }

    private void UpdateQuickTrainingButtonText()
    {
        var numberFormat = _localizationService.CurrentNumberFormat;
        var rangeText = numberFormat == NumberFormat.Padded ? "00-09" : "0-9";

        // Get the base localized text without the hardcoded range
        var baseText = _localizationService.GetString("MainPage_BeginTrainingBase");
        QuickTrainingButtonText = $"{baseText} ({rangeText})";
    }

    private async void LoadCustomRangeSettingsAsync()
    {
        try
        {
            var settings = await _settingsService.LoadSettingsAsync();

            // Restore saved values
            SelectedStartIndex = settings.CustomRangeStartIndex;
            SelectedEndIndex = settings.CustomRangeEndIndex;
            CustomRangeStart = settings.CustomRangeStart;
            CustomRangeEnd = settings.CustomRangeEnd;

            _logger.LogInformation($"Loaded custom range settings: {CustomRangeStart}-{CustomRangeEnd}");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load Flash Numbers settings, using defaults");
        }
    }

    private async void SaveCustomRangeSettingsAsync()
    {
        try
        {
            var settings = new FlashNumbersSettings
            {
                CustomRangeStartIndex = SelectedStartIndex,
                CustomRangeEndIndex = SelectedEndIndex,
                CustomRangeStart = CustomRangeStart,
                CustomRangeEnd = CustomRangeEnd
            };

            await _settingsService.SaveSettingsAsync(settings);
            _logger.LogDebug($"Saved custom range settings: {CustomRangeStart}-{CustomRangeEnd}");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to save Flash Numbers settings");
        }
    }

    #endregion
}

/// <summary>
/// Represents a preset training configuration option.
/// </summary>
public record TrainingOption(string Name, string RangeStart, string RangeEnd, string Description);