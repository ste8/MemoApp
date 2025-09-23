using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MemoApp.Core.MajorSystem.Models;

namespace MemoApp.UI.MauiApp.ViewModels;

/// <summary>
/// ViewModel for the Major System help and reference page.
/// Provides comprehensive guidance on the Major System technique using the unified mapping service.
/// </summary>
public partial class HelpViewModel : BaseViewModel
{
    private readonly IDigitLetterMappingService _mappingService;

    [ObservableProperty]
    private ObservableCollection<DigitLetterMapping> mappings = new();

    public HelpViewModel(IDigitLetterMappingService mappingService)
    {
        _mappingService = mappingService;
        Title = "Major System Guide";
        LoadMajorSystemData();
    }

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
    private async Task GoToMainPage()
    {
        await Shell.Current.GoToAsync("//main");
    }

    private void LoadMajorSystemData()
    {
        // Clear existing collections to ensure proper binding
        Mappings.Clear();

        // Load localized digit-letter mappings from the service
        var mappingData = _mappingService.GetMappings();

        // Add items individually to trigger proper change notifications
        foreach (var mapping in mappingData)
        {
            Mappings.Add(mapping);
        }
    }
}