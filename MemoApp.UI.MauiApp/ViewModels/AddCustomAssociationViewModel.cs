using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using MemoApp.Core.MajorSystem.Models;
using MemoApp.Core.MajorSystem.Services;
using MemoApp.Localization.Services;
using Microsoft.Extensions.Logging;
using MemoApp.UI.MauiApp.Messages;

namespace MemoApp.UI.MauiApp.ViewModels;

/// <summary>
/// ViewModel for adding custom associations
/// </summary>
[QueryProperty(nameof(Token), "token")]
[QueryProperty(nameof(Category), "category")]
public partial class AddCustomAssociationViewModel : ObservableObject, IDisposable
{
    private readonly ICustomAssociationsService _customAssociationsService;
    private readonly ICustomImageSourceService _imageSourceService;
    private readonly IPackAssociationsService _packAssociationsService;
    private readonly ITokenDefaultsService _tokenDefaultsService;
    private readonly ILocalizationService _localizationService;
    private readonly IMessenger _messenger;
    private readonly ILogger<AddCustomAssociationViewModel> _logger;

    [ObservableProperty]
    private string _token = string.Empty;

    [ObservableProperty]
    private string _category = string.Empty;

    [ObservableProperty]
    private string _word = string.Empty;

    [ObservableProperty]
    private string _pageTitle = string.Empty;

    [ObservableProperty]
    private bool _setAsDefault = true;

    [ObservableProperty]
    private bool _hasSelectedImage;

    [ObservableProperty]
    private string _selectedImagePath = string.Empty;

    [ObservableProperty]
    private bool _isAIGenerationAvailable;

    [ObservableProperty]
    private int _remainingCredits;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _validationMessage = string.Empty;

    private Stream? _selectedImageStream;

    /// <summary>
    /// Gets whether the save operation can be performed
    /// </summary>
    public bool CanSave => !string.IsNullOrWhiteSpace(Word) && HasSelectedImage && !IsLoading;

    public AddCustomAssociationViewModel(
        ICustomAssociationsService customAssociationsService,
        ICustomImageSourceService imageSourceService,
        IPackAssociationsService packAssociationsService,
        ITokenDefaultsService tokenDefaultsService,
        ILocalizationService localizationService,
        IMessenger messenger,
        ILogger<AddCustomAssociationViewModel> logger)
    {
        _customAssociationsService = customAssociationsService;
        _imageSourceService = imageSourceService;
        _packAssociationsService = packAssociationsService;
        _tokenDefaultsService = tokenDefaultsService;
        _localizationService = localizationService;
        _messenger = messenger;
        _logger = logger;

        // Initialize AI availability
        _ = CheckAIAvailabilityAsync();
    }

    /// <summary>
    /// Initializes the ViewModel with the token and category
    /// </summary>
    /// <param name="token">The token to create association for</param>
    /// <param name="category">The category key</param>
    public async Task InitializeAsync(string token, string category)
    {
        Token = token;
        Category = category;
        PageTitle = string.Format(_localizationService.GetString("AddCustomAssociation_Title"), token);
        
        await CheckAIAvailabilityAsync();
        
        // Clear any previous validation messages
        ValidationMessage = string.Empty;
        
        _logger.LogInformation("Initialized AddCustomAssociation for token {Token} in category {Category}", token, category);
    }

    [RelayCommand]
    private async Task SelectImageAsync()
    {
        try
        {
            _logger.LogInformation("User requested to select image for token {Token}", Token);
            
            var imageStream = await _imageSourceService.PickImageFromDeviceAsync();
            if (imageStream != null)
            {
                _selectedImageStream?.Dispose();
                _selectedImageStream = imageStream;
                HasSelectedImage = true;
                SelectedImagePath = "Selected from device"; // Placeholder text
                
                // Clear validation message when image is selected
                if (string.IsNullOrWhiteSpace(ValidationMessage))
                    ValidationMessage = string.Empty;
                
                OnPropertyChanged(nameof(CanSave));
                _logger.LogInformation("Image selected successfully for token {Token}", Token);
            }
            else
            {
                _logger.LogInformation("Image selection cancelled by user for token {Token}", Token);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error selecting image for token {Token}", Token);
            
            var errorTitle = _localizationService.GetString("Common_Error");
            var errorMessage = _localizationService.GetString("AddCustomAssociation_ErrorSelectingImage");
            
            await Application.Current.MainPage.DisplayAlert(errorTitle, errorMessage, _localizationService.GetString("Common_OK"));
        }
    }

    [RelayCommand]
    private async Task GenerateWithAIAsync()
    {
        try
        {
            _logger.LogInformation("User requested AI generation for token {Token} with word '{Word}'", Token, Word);
            
            if (string.IsNullOrWhiteSpace(Word))
            {
                ValidationMessage = _localizationService.GetString("AddCustomAssociation_WordRequired");
                return;
            }

            if (!IsAIGenerationAvailable)
            {
                var errorTitle = _localizationService.GetString("Common_Error");
                var errorMessage = _localizationService.GetString("AddCustomAssociation_AINotAvailable");
                await Application.Current.MainPage.DisplayAlert(errorTitle, errorMessage, _localizationService.GetString("Common_OK"));
                return;
            }

            // TODO: Open AI Generation form/modal
            // For now, show a placeholder message
            var infoTitle = _localizationService.GetString("Common_Info");
            var infoMessage = _localizationService.GetString("AddCustomAssociation_AIComingSoon");
            await Application.Current.MainPage.DisplayAlert(infoTitle, infoMessage, _localizationService.GetString("Common_OK"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error with AI generation for token {Token}", Token);
        }
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (!CanSave)
        {
            UpdateValidationMessage();
            return;
        }

        try
        {
            IsLoading = true;
            _logger.LogInformation("Saving custom association: {Token} -> {Word} (Default: {SetAsDefault})", 
                Token, Word, SetAsDefault);

            var currentPack = _packAssociationsService.CurrentPack;
            if (currentPack == null)
            {
                var errorMessage = _localizationService.GetString("AddCustomAssociation_NoPackError");
                await Application.Current.MainPage.DisplayAlert(
                    _localizationService.GetString("Common_Error"), 
                    errorMessage, 
                    _localizationService.GetString("Common_OK"));
                return;
            }

            if (_selectedImageStream == null)
            {
                ValidationMessage = _localizationService.GetString("AddCustomAssociation_ImageRequired");
                return;
            }

            // Get the current default before creating the association (if we're setting as default)
            string? previousDefault = null;
            if (SetAsDefault)
            {
                previousDefault = await _tokenDefaultsService.GetEffectiveDefaultAsync(
                    currentPack.Id, Category, Token, null);
            }

            // Create the custom association
            var association = await _customAssociationsService.CreateCustomAssociationAsync(
                currentPack.Id, Category, Token, Word.Trim(), _selectedImageStream, SetAsDefault);

            _logger.LogInformation("Successfully created custom association: {Token} -> {Word}", Token, Word);

            // Send message to notify that a custom association was added
            var addedMessage = new CustomAssociationAddedMessage(
                currentPack.Id,
                Category,
                Token,
                Word.Trim(),
                SetAsDefault);
            
            _messenger.Send(addedMessage);
            _logger.LogInformation("Sent CustomAssociationAddedMessage for {Token} -> {Word}", Token, Word);

            // If this association was set as default, also send a TokenDefaultChangedMessage
            if (SetAsDefault)
            {
                var defaultChangedMessage = new TokenDefaultChangedMessage(
                    currentPack.Id,
                    Category,
                    Token,
                    association.FileName,
                    previousDefault);
                
                _messenger.Send(defaultChangedMessage);
                _logger.LogInformation("Sent TokenDefaultChangedMessage for new custom association {Token} -> {Word}", Token, Word);
            }

            // Show success message
            var successTitle = _localizationService.GetString("Common_Success");
            var successMessage = string.Format(_localizationService.GetString("AddCustomAssociation_SuccessMessage"), Word, Token);
            await Application.Current.MainPage.DisplayAlert(successTitle, successMessage, _localizationService.GetString("Common_OK"));

            // Navigate back
            await Shell.Current.GoToAsync("..");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving custom association: {Token} -> {Word}", Token, Word);
            
            var errorTitle = _localizationService.GetString("Common_Error");
            var errorMessage = _localizationService.GetString("AddCustomAssociation_SaveError");
            await Application.Current.MainPage.DisplayAlert(errorTitle, errorMessage, _localizationService.GetString("Common_OK"));
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task CancelAsync()
    {
        _logger.LogInformation("User cancelled adding custom association for token {Token}", Token);
        await Shell.Current.GoToAsync("..");
    }

    partial void OnWordChanged(string value)
    {
        OnPropertyChanged(nameof(CanSave));
        
        // Clear validation message when word is entered
        if (!string.IsNullOrWhiteSpace(value) && HasSelectedImage)
        {
            ValidationMessage = string.Empty;
        }
    }

    partial void OnHasSelectedImageChanged(bool value)
    {
        OnPropertyChanged(nameof(CanSave));
    }

    partial void OnTokenChanged(string value)
    {
        CheckAndInitialize();
    }

    partial void OnCategoryChanged(string value)
    {
        CheckAndInitialize();
    }

    private void CheckAndInitialize()
    {
        if (!string.IsNullOrEmpty(Token) && !string.IsNullOrEmpty(Category))
        {
            _ = InitializeAsync(Token, Category);
        }
    }

    private async Task CheckAIAvailabilityAsync()
    {
        try
        {
            IsAIGenerationAvailable = await _imageSourceService.IsAIGenerationAvailableAsync();
            RemainingCredits = await _imageSourceService.GetRemainingCreditsAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking AI availability");
            IsAIGenerationAvailable = false;
            RemainingCredits = 0;
        }
    }

    private void UpdateValidationMessage()
    {
        if (string.IsNullOrWhiteSpace(Word))
        {
            ValidationMessage = _localizationService.GetString("AddCustomAssociation_WordRequired");
        }
        else if (!HasSelectedImage)
        {
            ValidationMessage = _localizationService.GetString("AddCustomAssociation_ImageRequired");
        }
        else
        {
            ValidationMessage = string.Empty;
        }
    }

    public void Dispose()
    {
        _selectedImageStream?.Dispose();
    }
}