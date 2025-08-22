using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MemoApp.Core.MajorSystem.Models;
using MemoApp.Core.MajorSystem.Services;
using MemoApp.Localization.Services;
using Microsoft.Extensions.Logging;

namespace MemoApp.UI.MauiApp.ViewModels;

/// <summary>
/// ViewModel for the Token Detail page showing all associations for a specific token
/// </summary>
[QueryProperty(nameof(Token), "token")]
[QueryProperty(nameof(Category), "category")]
public partial class TokenDetailViewModel : ObservableObject
{
    private readonly IPackAssociationsService _packAssociationsService;
    private readonly ITokenDefaultsService _tokenDefaultsService;
    private readonly ILocalizationService _localizationService;
    private readonly ILogger<TokenDetailViewModel> _logger;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _token = string.Empty;

    [ObservableProperty]
    private string _category = string.Empty;

    [ObservableProperty]
    private string _pageTitle = string.Empty;

    [ObservableProperty]
    private ObservableCollection<TokenAssociation> _associations = new();

    [ObservableProperty]
    private TokenAssociation? _selectedAssociation;

    public TokenDetailViewModel(
        IPackAssociationsService packAssociationsService,
        ITokenDefaultsService tokenDefaultsService,
        ILocalizationService localizationService,
        ILogger<TokenDetailViewModel> logger)
    {
        _packAssociationsService = packAssociationsService;
        _tokenDefaultsService = tokenDefaultsService;
        _localizationService = localizationService;
        _logger = logger;
    }

    partial void OnTokenChanged(string value)
    {
        UpdatePageTitle();
        if (!string.IsNullOrEmpty(Token) && !string.IsNullOrEmpty(Category))
        {
            _ = LoadAssociationsAsync();
        }
    }

    partial void OnCategoryChanged(string value)
    {
        UpdatePageTitle();
        if (!string.IsNullOrEmpty(Token) && !string.IsNullOrEmpty(Category))
        {
            _ = LoadAssociationsAsync();
        }
    }

    private void UpdatePageTitle()
    {
        if (!string.IsNullOrEmpty(Token))
        {
            PageTitle = string.Format(_localizationService.GetString("TokenDetail_Title"), Token);
        }
        else
        {
            PageTitle = _localizationService.GetString("TokenDetail_Title_Generic");
        }
    }

    [RelayCommand]
    private async Task LoadAssociationsAsync()
    {
        if (string.IsNullOrEmpty(Token) || string.IsNullOrEmpty(Category))
            return;

        try
        {
            IsLoading = true;
            _logger.LogInformation("Loading associations for token {Token} in category {Category}", Token, Category);

            var associations = await _packAssociationsService.GetTokenAssociationsAsync(Category, Token);
            
            _logger.LogInformation("Found {Count} associations for token {Token}", associations.Count, Token);

            // Sort associations to show default item first, then alphabetically by word
            var sortedAssociations = associations
                .OrderByDescending(a => a.IsDefault)  // Default items first
                .ThenBy(a => a.Word)                  // Then alphabetically
                .ToList();

            var defaultAssociation = sortedAssociations.FirstOrDefault(a => a.IsDefault);
            if (defaultAssociation != null)
            {
                _logger.LogInformation("Default association '{Word}' will be displayed first for token {Token}", 
                    defaultAssociation.Word, Token);
            }

            Associations.Clear();
            foreach (var association in sortedAssociations)
            {
                Associations.Add(association);
                _logger.LogDebug("Added association: {Token} -> {Word} (Default: {IsDefault})", 
                    association.Token, association.Word, association.IsDefault);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading associations for token {Token}", Token);
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task SetAsDefaultAsync(TokenAssociation association)
    {
        if (association == null || _packAssociationsService.CurrentPack == null)
            return;

        // If already default, show informational message
        if (association.IsDefault)
        {
            var infoTitle = _localizationService.GetString("TokenDetail_AlreadyDefaultTitle");
            var infoMessage = string.Format(
                _localizationService.GetString("TokenDetail_AlreadyDefaultMessage"), 
                association.Word, 
                association.Token);
            var okButton = _localizationService.GetString("Common_OK");

            await Application.Current.MainPage.DisplayAlert(
                infoTitle,
                infoMessage,
                okButton);

            _logger.LogInformation("User tapped already-default item {Token} -> {Word}", association.Token, association.Word);
            return;
        }

        // Show confirmation dialog for non-default items
        var confirmTitle = _localizationService.GetString("TokenDetail_SetAsDefaultConfirmTitle");
        var confirmMessage = string.Format(
            _localizationService.GetString("TokenDetail_SetAsDefaultConfirmMessage"), 
            association.Word, 
            association.Token);
        var confirmButton = _localizationService.GetString("Common_Confirm");
        var cancelButton = _localizationService.GetString("Common_Cancel");

        var result = await Application.Current.MainPage.DisplayAlert(
            confirmTitle,
            confirmMessage,
            confirmButton,
            cancelButton);

        if (!result)
        {
            _logger.LogInformation("User cancelled setting {Token} -> {Word} as default", association.Token, association.Word);
            return;
        }

        try
        {
            _logger.LogInformation("Setting {Token} -> {Word} as default", association.Token, association.Word);

            await _tokenDefaultsService.SetTokenDefaultAsync(
                _packAssociationsService.CurrentPack.Id,
                association.Category,
                association.Token,
                association.FileName);

            // Refresh the associations to show updated default status
            await LoadAssociationsAsync();

            _logger.LogInformation("Successfully set {Token} -> {Word} as default", association.Token, association.Word);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting token default for {Token} -> {Word}", association.Token, association.Word);
        }
    }

    [RelayCommand]
    private async Task ViewFullImageAsync(TokenAssociation association)
    {
        if (association == null || string.IsNullOrEmpty(association.ImagePath))
            return;

        try
        {
            SelectedAssociation = association;
            _logger.LogInformation("Navigating to full image viewer for {Token} -> {Word}", association.Token, association.Word);
            
            // Navigate to the full-screen image viewer
            var navigationParameter = $"ImageViewerPage?imagePath={Uri.EscapeDataString(association.ImagePath)}&token={association.Token}&word={Uri.EscapeDataString(association.Word)}";
            await Shell.Current.GoToAsync(navigationParameter);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error viewing full image for {Token} -> {Word}", association.Token, association.Word);
        }
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        await LoadAssociationsAsync();
    }
}