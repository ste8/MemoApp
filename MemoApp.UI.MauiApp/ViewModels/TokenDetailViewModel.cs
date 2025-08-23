using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using MemoApp.Core.MajorSystem.Models;
using MemoApp.Core.MajorSystem.Services;
using MemoApp.Localization.Services;
using Microsoft.Extensions.Logging;
using MemoApp.UI.MauiApp.Messages;
using MemoApp.UI.MauiApp.Services;

namespace MemoApp.UI.MauiApp.ViewModels;

/// <summary>
/// ViewModel for the Token Detail page showing all associations for a specific token
/// </summary>
[QueryProperty(nameof(Token), "token")]
[QueryProperty(nameof(Category), "category")]
public partial class TokenDetailViewModel : ObservableObject, IRecipient<CustomAssociationAddedMessage>
{
    private readonly IPackAssociationsService _packAssociationsService;
    private readonly ITokenDefaultsService _tokenDefaultsService;
    private readonly ICustomAssociationsService _customAssociationsService;
    private readonly ILocalizationService _localizationService;
    private readonly IMessenger _messenger;
    private readonly IImageCacheService _imageCacheService;
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

    /// <summary>
    /// Shows empty state when not loading and no associations exist
    /// </summary>
    public bool ShowEmptyState => !IsLoading && Associations.Count == 0;

    /// <summary>
    /// Shows main content when not loading and associations exist
    /// </summary>
    public bool ShowMainContent => !IsLoading && Associations.Count > 0;

    partial void OnIsLoadingChanged(bool value)
    {
        OnPropertyChanged(nameof(ShowEmptyState));
        OnPropertyChanged(nameof(ShowMainContent));
    }

    public TokenDetailViewModel(
        IPackAssociationsService packAssociationsService,
        ITokenDefaultsService tokenDefaultsService,
        ICustomAssociationsService customAssociationsService,
        ILocalizationService localizationService,
        IMessenger messenger,
        IImageCacheService imageCacheService,
        ILogger<TokenDetailViewModel> logger)
    {
        _packAssociationsService = packAssociationsService;
        _tokenDefaultsService = tokenDefaultsService;
        _customAssociationsService = customAssociationsService;
        _localizationService = localizationService;
        _messenger = messenger;
        _imageCacheService = imageCacheService;
        _logger = logger;
        
        // Register for custom association added messages
        _messenger.Register<CustomAssociationAddedMessage>(this);
        
        // Listen to Associations collection changes to update computed properties
        Associations.CollectionChanged += (_, _) =>
        {
            OnPropertyChanged(nameof(ShowEmptyState));
            OnPropertyChanged(nameof(ShowMainContent));
        };
    }

    partial void OnTokenChanged(string value)
    {
        UpdatePageTitle();
        if (!string.IsNullOrEmpty(Token) && !string.IsNullOrEmpty(Category))
        {
            // Set loading state immediately to prevent empty state flicker
            IsLoading = true;
            _ = LoadAssociationsAsync();
        }
    }

    partial void OnCategoryChanged(string value)
    {
        UpdatePageTitle();
        if (!string.IsNullOrEmpty(Token) && !string.IsNullOrEmpty(Category))
        {
            // Set loading state immediately to prevent empty state flicker
            IsLoading = true;
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
            
            // Also load custom associations for this token
            var currentPack = _packAssociationsService.CurrentPack;
            var customAssociations = currentPack != null 
                ? await _customAssociationsService.GetCustomAssociationsForTokenAsync(currentPack.Id, Category, Token)
                : new List<TokenAssociation>();
            
            // Apply default status to custom associations
            if (customAssociations.Count > 0 && currentPack != null)
            {
                var currentDefault = await _tokenDefaultsService.GetEffectiveDefaultAsync(currentPack.Id, Category, Token, null);
                foreach (var customAssoc in customAssociations)
                {
                    // Check if this custom association is the default based on filename pattern
                    var customFilename = Path.GetFileName(customAssoc.ImagePath);
                    customAssoc.IsDefault = !string.IsNullOrEmpty(currentDefault) && currentDefault.EndsWith(customFilename);
                }
            }
            
            // Combine pack and custom associations
            var allAssociations = new List<TokenAssociation>(associations);
            allAssociations.AddRange(customAssociations);
            
            _logger.LogInformation("Found {PackCount} pack associations and {CustomCount} custom associations for token {Token}", 
                associations.Count, customAssociations.Count, Token);
                
            // Debug custom associations
            foreach (var customAssoc in customAssociations)
            {
                _logger.LogInformation("Custom association: {Token} -> {Word}, ImagePath: {ImagePath}, IsDefault: {IsDefault}, IsCustom: {IsCustom}", 
                    customAssoc.Token, customAssoc.Word, customAssoc.ImagePath, customAssoc.IsDefault, customAssoc.IsCustom);
            }

            // Sort associations to show default item first, then alphabetically by word
            var sortedAssociations = allAssociations
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
            
            // Skip preloading for now to test synchronous loading
            // _ = PreloadAssociationImagesAsync(sortedAssociations);
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

            // Get the current default before changing it
            var currentPack = _packAssociationsService.CurrentPack;
            var previousDefault = await _tokenDefaultsService.GetEffectiveDefaultAsync(
                currentPack.Id, association.Category, association.Token, null);

            await _tokenDefaultsService.SetTokenDefaultAsync(
                currentPack.Id,
                association.Category,
                association.Token,
                association.FileName);

            // Send message to notify other views about the default change
            var message = new TokenDefaultChangedMessage(
                currentPack.Id,
                association.Category,
                association.Token,
                association.FileName,
                previousDefault);
            
            _messenger.Send(message);
            _logger.LogInformation("Sent TokenDefaultChangedMessage for {Token} -> {Word}", association.Token, association.Word);

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

    [RelayCommand]
    private async Task AddCustomAssociationAsync()
    {
        if (string.IsNullOrEmpty(Token) || string.IsNullOrEmpty(Category))
            return;

        try
        {
            _logger.LogInformation("Navigating to add custom association for token {Token}", Token);
            
            // Navigate to the add custom association page
            var navigationParameter = $"AddCustomAssociationPage?token={Token}&category={Uri.EscapeDataString(Category)}";
            await Shell.Current.GoToAsync(navigationParameter);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error navigating to add custom association for token {Token}", Token);
        }
    }

    [RelayCommand]
    private async Task DeleteCustomAssociationAsync(TokenAssociation association)
    {
        if (association == null || !association.IsCustom)
            return;

        try
        {
            _logger.LogInformation("User requested to delete custom association {Token} -> {Word}", association.Token, association.Word);

            // Show confirmation dialog
            var confirmTitle = _localizationService.GetString("TokenDetail_DeleteCustomTitle");
            var confirmMessage = string.Format(
                _localizationService.GetString("TokenDetail_DeleteCustomMessage"), 
                association.Word, 
                association.Token);
            var confirmButton = _localizationService.GetString("Common_Delete");
            var cancelButton = _localizationService.GetString("Common_Cancel");

            var result = await Application.Current.MainPage.DisplayAlert(
                confirmTitle,
                confirmMessage,
                confirmButton,
                cancelButton);

            if (!result)
            {
                _logger.LogInformation("User cancelled deletion of custom association {Token} -> {Word}", association.Token, association.Word);
                return;
            }

            // Store the default status before deletion
            var wasDefault = association.IsDefault;
            var currentPack = _packAssociationsService.CurrentPack;

            // Delete the custom association
            await _customAssociationsService.DeleteCustomAssociationAsync(association);

            _logger.LogInformation("Successfully deleted custom association {Token} -> {Word}, was default: {WasDefault}", 
                association.Token, association.Word, wasDefault);

            // Send message to notify other views about the deletion
            if (currentPack != null)
            {
                var message = new CustomAssociationDeletedMessage(
                    currentPack.Id,
                    association.Category,
                    association.Token,
                    association.Word,
                    wasDefault);
                
                _messenger.Send(message);
                _logger.LogInformation("Sent CustomAssociationDeletedMessage for {Token} -> {Word}", association.Token, association.Word);
            }

            // Show success message
            var successTitle = _localizationService.GetString("Common_Success");
            var successMessage = string.Format(_localizationService.GetString("TokenDetail_DeleteCustomSuccess"), association.Word, association.Token);
            await Application.Current.MainPage.DisplayAlert(successTitle, successMessage, _localizationService.GetString("Common_OK"));

            // Refresh the associations list
            await LoadAssociationsAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting custom association {Token} -> {Word}", association.Token, association.Word);
            
            var errorTitle = _localizationService.GetString("Common_Error");
            var errorMessage = _localizationService.GetString("TokenDetail_DeleteCustomError");
            await Application.Current.MainPage.DisplayAlert(errorTitle, errorMessage, _localizationService.GetString("Common_OK"));
        }
    }

    /// <summary>
    /// Receives CustomAssociationAddedMessage to refresh associations when a new custom association is added
    /// </summary>
    public async void Receive(CustomAssociationAddedMessage message)
    {
        try
        {
            // Only refresh if the message is for the current token
            if (message.Category == Category && message.Token == Token)
            {
                _logger.LogInformation("Received CustomAssociationAddedMessage for {Token} -> {Word}, refreshing associations", 
                    message.Token, message.Word);
                
                await LoadAssociationsAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling CustomAssociationAddedMessage for {Token}", Token);
        }
    }

    /// <summary>
    /// Preloads association images asynchronously for better performance
    /// </summary>
    private async Task PreloadAssociationImagesAsync(List<TokenAssociation> associations)
    {
        try
        {
            // Extract all image paths that need preloading
            var imagePaths = associations
                .SelectMany(a => new[] { a.ThumbnailPath, a.ImagePath })
                .Where(path => !string.IsNullOrEmpty(path))
                .Distinct()
                .ToList();

            if (imagePaths.Count > 0)
            {
                _logger.LogInformation("Preloading {Count} images for token detail view", imagePaths.Count);
                
                // Preload in background with shorter timeout for detail view
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
                await _imageCacheService.PreloadImagesAsync(imagePaths, cts.Token);
                
                _logger.LogInformation("Completed preloading images for token detail");
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Image preloading was cancelled for token detail");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error during image preloading in token detail");
        }
    }
}