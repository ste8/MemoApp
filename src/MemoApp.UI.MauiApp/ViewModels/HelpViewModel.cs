using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MemoApp.Core.MajorSystem.Models;
using MemoApp.Localization.Services;
using Microsoft.Maui.ApplicationModel;

namespace MemoApp.UI.MauiApp.ViewModels;

/// <summary>
/// ViewModel for the Major System help and reference page.
/// Provides comprehensive guidance on the Major System technique using the unified mapping service.
/// </summary>
public partial class HelpViewModel : BaseViewModel
{
    private readonly IDigitLetterMappingService _mappingService;
    private readonly ILocalizationService _localizationService;

    [ObservableProperty]
    private ObservableCollection<DigitLetterMapping> mappings = new();

    public HelpViewModel(IDigitLetterMappingService mappingService, ILocalizationService localizationService)
    {
        _mappingService = mappingService;
        _localizationService = localizationService;
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
        await Shell.Current.GoToAsync("//associations");
    }

    [RelayCommand]
    private async Task OpenWikipedia()
    {
        try
        {
            // Determine Wikipedia URL based on current culture
            var currentCulture = _localizationService.CurrentCulture;
            var wikipediaUrl = currentCulture.TwoLetterISOLanguageName == "it"
                ? "https://it.wikipedia.org/wiki/Conversione_fonetica"
                : "https://en.wikipedia.org/wiki/Mnemonic_major_system";

            // Open the URL in the default browser
            await Launcher.OpenAsync(new Uri(wikipediaUrl));
        }
        catch (Exception ex)
        {
            // Log error but don't crash the app
            System.Diagnostics.Debug.WriteLine($"Failed to open Wikipedia: {ex.Message}");
        }
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