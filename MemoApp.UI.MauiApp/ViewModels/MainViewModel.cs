using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MemoApp.Core.MajorSystem;
using MemoApp.Localization.Services;
using System.Globalization;

namespace MemoApp.UI.MauiApp.ViewModels;

/// <summary>
/// ViewModel for the main menu page.
/// Provides navigation options and quick training configurations.
/// </summary>
public partial class MainViewModel : BaseViewModel
{
    private readonly ILocalizationService _localizationService;
    private const string RangeValidationErrorMessage = "Start number must be less than or equal to end number.";
    
    public MainViewModel(ILocalizationService localizationService)
    {
        _localizationService = localizationService;
        Title = "Major System Training";
        LoadTrainingOptions();
        LoadNumberOptions();
        
        // Subscribe to culture changes to update validation message
        _localizationService.CultureChanged += OnCultureChanged;
        
        // Initialize dropdown selected values
        InitializeDropdownValues();
    }
    
    private void InitializeDropdownValues()
    {
        if (NumberOptions.Count > 0)
        {
            SelectedStartNumberOption = NumberOptions.ElementAtOrDefault(SelectedStartIndex);
            SelectedEndNumberOption = NumberOptions.ElementAtOrDefault(SelectedEndIndex);
        }
    }

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


    [RelayCommand]
    private async Task StartQuickTraining()
    {
        var parameters = new Dictionary<string, object>
        {
            { "rangeStart", "00" },
            { "rangeEnd", "09" }
        };
        
        await Shell.Current.GoToAsync("training", parameters);
    }

    [RelayCommand]
    private async Task StartCustomTraining()
    {
        if (IsRangeValid())
        {
            var parameters = new Dictionary<string, object>
            {
                { "rangeStart", CustomRangeStart },
                { "rangeEnd", CustomRangeEnd }
            };
            
            await Shell.Current.GoToAsync("training", parameters);
        }
        else
        {
            ValidationMessage = _localizationService.GetString("Validation_RangeError");
            HasValidationError = true;
        }
    }

    [RelayCommand]
    private async Task ShowHelp()
    {
        await Shell.Current.GoToAsync("//help");
    }


    [RelayCommand]
    private async Task StartPresetTraining(TrainingOption option)
    {
        var parameters = new Dictionary<string, object>
        {
            { "rangeStart", option.RangeStart },
            { "rangeEnd", option.RangeEnd }
        };
        
        await Shell.Current.GoToAsync("training", parameters);
    }

    [RelayCommand]
    private void ToggleStartDropdown()
    {
        System.Diagnostics.Debug.WriteLine($"ToggleStartDropdown called, current state: {IsStartDropdownVisible}");
        
        IsEndDropdownVisible = false; // Close end dropdown if open
        IsStartDropdownVisible = !IsStartDropdownVisible;
        
        System.Diagnostics.Debug.WriteLine($"Start dropdown now: {IsStartDropdownVisible}");
    }

    [RelayCommand]
    private void ToggleEndDropdown()
    {
        System.Diagnostics.Debug.WriteLine($"ToggleEndDropdown called, current state: {IsEndDropdownVisible}");
        
        IsStartDropdownVisible = false; // Close start dropdown if open
        IsEndDropdownVisible = !IsEndDropdownVisible;
        
        System.Diagnostics.Debug.WriteLine($"End dropdown now: {IsEndDropdownVisible}");
    }

    [RelayCommand]
    private void SelectStartNumberOption(string option)
    {
        System.Diagnostics.Debug.WriteLine($"SelectStartNumberOption called with: {option}");
        
        if (!string.IsNullOrEmpty(option))
        {
            var selectedIndex = NumberOptions.IndexOf(option);
            System.Diagnostics.Debug.WriteLine($"Found index: {selectedIndex} for option: {option}");
            
            if (selectedIndex >= 0)
            {
                SelectedStartIndex = selectedIndex;
                SelectedStartNumberOption = option;
                System.Diagnostics.Debug.WriteLine($"Updated SelectedStartIndex to: {selectedIndex}, CustomRangeStart: {CustomRangeStart}");
            }
        }
        IsStartDropdownVisible = false;
    }

    [RelayCommand]
    private void SelectEndNumberOption(string option)
    {
        System.Diagnostics.Debug.WriteLine($"SelectEndNumberOption called with: {option}");
        
        if (!string.IsNullOrEmpty(option))
        {
            var selectedIndex = NumberOptions.IndexOf(option);
            System.Diagnostics.Debug.WriteLine($"Found index: {selectedIndex} for option: {option}");
            
            if (selectedIndex >= 0)
            {
                SelectedEndIndex = selectedIndex;
                SelectedEndNumberOption = option;
                System.Diagnostics.Debug.WriteLine($"Updated SelectedEndIndex to: {selectedIndex}, CustomRangeEnd: {CustomRangeEnd}");
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
            System.Diagnostics.Debug.WriteLine($"Number selection error: {ex.Message}");
        }
    }

    private void LoadTrainingOptions()
    {
        TrainingOptions = new ObservableCollection<TrainingOption>
        {
            new("Beginner", "00", "09", "Zero-prefixed numbers (00-09)"),
            new("Single Digits", "0", "9", "Basic single digits (0-9)"),
            new("Teens", "10", "19", "Teen numbers (10-19)"),
            new("Twenties", "20", "29", "Twenty range (20-29)"),
            new("First 50", "00", "49", "Extended practice (00-49)"),
            new("Full Range", "00", "99", "Complete Major System (00-99)")
        };
    }

    private void LoadNumberOptions()
    {
        NumberOptions.Clear();
        MajorNumbers.Clear();

        // Add zero-prefixed numbers (00-09)
        for (int i = 0; i <= 9; i++)
        {
            var majorNumber = new MajorNumber(i, true);
            MajorNumbers.Add(majorNumber);
            NumberOptions.Add(majorNumber.Display);
        }

        // Add regular numbers (0-99)
        for (int i = 0; i <= 99; i++)
        {
            var majorNumber = new MajorNumber(i, false);
            MajorNumbers.Add(majorNumber);
            NumberOptions.Add(majorNumber.Display);
        }
    }

    partial void OnSelectedStartIndexChanged(int value)
    {
        if (value >= 0 && value < MajorNumbers.Count)
        {
            CustomRangeStart = MajorNumbers[value].Display;
            UpdateValidation();
        }
    }

    partial void OnSelectedEndIndexChanged(int value)
    {
        if (value >= 0 && value < MajorNumbers.Count)
        {
            CustomRangeEnd = MajorNumbers[value].Display;
            UpdateValidation();
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
        });
    }

}

/// <summary>
/// Represents a preset training configuration option.
/// </summary>
public record TrainingOption(string Name, string RangeStart, string RangeEnd, string Description);

