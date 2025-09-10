using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MemoApp.Localization.Services;
using MemoApp.UI.MauiApp.Services;
using Microsoft.Extensions.Logging;

namespace MemoApp.UI.MauiApp.ViewModels;

/// <summary>
/// ViewModel for the main welcome page.
/// Provides navigation to different app sections.
/// </summary>
public partial class MainViewModel : BaseViewModel
{
    private readonly ILocalizationService _localizationService;
    private readonly IUserNotificationService _userNotificationService;
    private readonly ILogger<MainViewModel> _logger;
    
    public MainViewModel(
        ILocalizationService localizationService,
        IUserNotificationService userNotificationService,
        ILogger<MainViewModel> logger)
    {
        _localizationService = localizationService;
        _userNotificationService = userNotificationService;
        _logger = logger;
        Title = _localizationService.GetString("MainPage_WelcomeTitle");
    }

    [RelayCommand]
    private async Task NavigateToFlashNumbers()
    {
        try
        {
            await Shell.Current.GoToAsync("//flashnumberssetup");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to navigate to Flash Numbers setup");
            await _userNotificationService.ShowErrorToastAsync(
                _localizationService.GetString("Navigation_Failed"));
        }
    }

    [RelayCommand]
    private async Task NavigateToBigNumber()
    {
        try
        {
            await Shell.Current.GoToAsync("//bignumber");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to navigate to Big Number");
            await _userNotificationService.ShowErrorToastAsync(
                _localizationService.GetString("Navigation_Failed"));
        }
    }

    [RelayCommand]
    private async Task NavigateToAssociations()
    {
        try
        {
            await Shell.Current.GoToAsync("//associations");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to navigate to Associations");
            await _userNotificationService.ShowErrorToastAsync(
                _localizationService.GetString("Navigation_Failed"));
        }
    }

    [RelayCommand]
    private async Task NavigateToSettings()
    {
        try
        {
            await Shell.Current.GoToAsync("//settings");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to navigate to Settings");
            await _userNotificationService.ShowErrorToastAsync(
                _localizationService.GetString("Navigation_Failed"));
        }
    }

    [RelayCommand]
    private async Task ShowHelp()
    {
        try
        {
            await Shell.Current.GoToAsync("//help");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to navigate to help page");
            await _userNotificationService.ShowErrorToastAsync(
                _localizationService.GetString("Navigation_Failed"));
        }
    }
}