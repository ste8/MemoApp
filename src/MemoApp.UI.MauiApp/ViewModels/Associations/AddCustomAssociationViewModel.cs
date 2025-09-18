using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using MemoApp.Core.MajorSystem.Models;
using MemoApp.Core.MajorSystem.Services;
using MemoApp.Localization.Services;
using MemoApp.UI.MauiApp.Services;
using Microsoft.Extensions.Logging;
using MemoApp.UI.MauiApp.Messages;

namespace MemoApp.UI.MauiApp.ViewModels.Associations;

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
    private readonly IUserNotificationService _userNotificationService;
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
    private ImageSource? _selectedImageSource;

    [ObservableProperty]
    private bool _isAIGenerationAvailable;

    [ObservableProperty]
    private int _remainingCredits;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _validationMessage = string.Empty;

    [ObservableProperty]
    private string _wordError = string.Empty;

    [ObservableProperty]
    private string _imageError = string.Empty;

    [ObservableProperty]
    private bool _isWordValid;

    [ObservableProperty]
    private bool _isImageValid;

    [ObservableProperty]
    private bool _hasValidationErrors;

    [ObservableProperty]
    private int _validationErrorCount;

    [ObservableProperty]
    private bool _isValidationSummaryExpanded;

    private Stream? _selectedImageStream;

    /// <summary>
    /// Gets whether the save operation can be performed
    /// </summary>
    public bool CanSave => !string.IsNullOrWhiteSpace(Word) && HasSelectedImage && !IsLoading;

    /// <summary>
    /// Gets whether the form is completely valid
    /// </summary>
    public bool IsFormValid => IsWordValid && IsImageValid && !IsLoading;

    public AddCustomAssociationViewModel(
        ICustomAssociationsService customAssociationsService,
        ICustomImageSourceService imageSourceService,
        IPackAssociationsService packAssociationsService,
        ITokenDefaultsService tokenDefaultsService,
        ILocalizationService localizationService,
        IUserNotificationService userNotificationService,
        IMessenger messenger,
        ILogger<AddCustomAssociationViewModel> logger)
    {
        _customAssociationsService = customAssociationsService;
        _imageSourceService = imageSourceService;
        _packAssociationsService = packAssociationsService;
        _tokenDefaultsService = tokenDefaultsService;
        _localizationService = localizationService;
        _userNotificationService = userNotificationService;
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
        ValidateAllFields();
        
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
                // Dispose previous stream
                _selectedImageStream?.Dispose();
                _selectedImageStream = imageStream;

                // Convert to ImageSource on main thread
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    try
                    {
                        // Reset position for ImageSource consumption
                        imageStream.Position = 0;
                        SelectedImageSource = ImageSource.FromStream(() =>
                        {
                            imageStream.Position = 0;
                            return imageStream;
                        });

                        HasSelectedImage = true;
                        SelectedImagePath = "Selected from device";

                        // Clear validation when image is selected
                        ValidateImageSelection();

                        OnPropertyChanged(nameof(CanSave));
                        OnPropertyChanged(nameof(IsFormValid));

                        _logger.LogInformation("Image selected successfully for token {Token}", Token);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to create ImageSource for token {Token}", Token);
                        ResetImageSelection();
                    }
                });
            }
            else
            {
                _logger.LogInformation("Image selection cancelled by user for token {Token}", Token);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error selecting image for token {Token}", Token);
            
            var errorMessage = _localizationService.GetString("AddCustomAssociation_ErrorSelectingImage");
            await _userNotificationService.ShowErrorToastAsync(errorMessage);
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
                ValidateWordInput();
                return;
            }

            if (!IsAIGenerationAvailable)
            {
                var errorMessage = _localizationService.GetString("AIService_NotAvailable");
                await _userNotificationService.ShowErrorToastAsync(errorMessage);
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
                await _userNotificationService.ShowErrorToastAsync(errorMessage);
                return;
            }

            if (_selectedImageStream == null)
            {
                ValidateImageSelection();
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
            TokenAssociation association;
            try
            {
                association = await _customAssociationsService.CreateCustomAssociationAsync(
                    currentPack.Id, Category, Token, Word.Trim(), _selectedImageStream, SetAsDefault);
            }
            catch (Exception ex) when (ex.Message.Contains("image processing") || ex.Message.Contains("thumbnail"))
            {
                _logger.LogError(ex, "Image processing failed for custom association: {Token} -> {Word}", Token, Word);
                var errorMessage = _localizationService.GetString("ImageProcessing_Failed");
                await _userNotificationService.ShowErrorToastAsync(errorMessage);
                return;
            }
            catch (Exception ex) when (ex.Message.Contains("invalid format") || ex.Message.Contains("unsupported"))
            {
                _logger.LogError(ex, "Invalid image format for custom association: {Token} -> {Word}", Token, Word);
                var errorMessage = _localizationService.GetString("ImageProcessing_InvalidFormat");
                await _userNotificationService.ShowErrorToastAsync(errorMessage);
                return;
            }

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
            
            var errorMessage = _localizationService.GetString("CustomAssociation_CreationFailed");
            await _userNotificationService.ShowErrorToastAsync(errorMessage);
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
        OnPropertyChanged(nameof(IsFormValid));

        // Real-time validation as user types
        ValidateWordInput();
    }

    partial void OnHasSelectedImageChanged(bool value)
    {
        OnPropertyChanged(nameof(CanSave));
        OnPropertyChanged(nameof(IsFormValid));

        // Real-time validation when image status changes
        ValidateImageSelection();
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
            
            // Don't show error to user for AI availability check - it's non-critical
        }
    }

    private void UpdateValidationMessage()
    {
        ValidateAllFields();
    }

    /// <summary>
    /// Validates the word input field
    /// </summary>
    private void ValidateWordInput()
    {
        if (string.IsNullOrWhiteSpace(Word))
        {
            WordError = _localizationService.GetString("AddCustomAssociation_WordRequired");
            IsWordValid = false;
        }
        else if (Word.Trim().Length < 2)
        {
            WordError = _localizationService.GetString("AddCustomAssociation_WordTooShort");
            IsWordValid = false;
        }
        else
        {
            WordError = string.Empty;
            IsWordValid = true;
        }

        UpdateValidationSummary();
    }

    /// <summary>
    /// Validates the image selection
    /// </summary>
    private void ValidateImageSelection()
    {
        if (!HasSelectedImage)
        {
            ImageError = _localizationService.GetString("AddCustomAssociation_ImageRequired");
            IsImageValid = false;
        }
        else
        {
            ImageError = string.Empty;
            IsImageValid = true;
        }

        UpdateValidationSummary();
    }

    /// <summary>
    /// Validates all form fields
    /// </summary>
    private void ValidateAllFields()
    {
        ValidateWordInput();
        ValidateImageSelection();
    }

    /// <summary>
    /// Updates the validation summary information
    /// </summary>
    private void UpdateValidationSummary()
    {
        var errorCount = 0;

        if (!string.IsNullOrEmpty(WordError)) errorCount++;
        if (!string.IsNullOrEmpty(ImageError)) errorCount++;

        ValidationErrorCount = errorCount;
        HasValidationErrors = errorCount > 0;

        // Update legacy validation message for existing UI
        if (HasValidationErrors)
        {
            if (!string.IsNullOrEmpty(WordError))
            {
                ValidationMessage = WordError;
            }
            else if (!string.IsNullOrEmpty(ImageError))
            {
                ValidationMessage = ImageError;
            }
        }
        else
        {
            ValidationMessage = string.Empty;
        }
    }

    [RelayCommand]
    private async Task ScrollToFirstErrorAsync()
    {
        if (!HasValidationErrors) return;

        try
        {
            // Navigate to first error field
            if (!string.IsNullOrEmpty(WordError))
            {
                // Focus will be handled by the UI when this command is triggered
                _logger.LogInformation("Scrolling to word input field due to validation error");
            }
            else if (!string.IsNullOrEmpty(ImageError))
            {
                // Scroll to image selection area
                _logger.LogInformation("Scrolling to image selection area due to validation error");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error scrolling to first validation error");
        }
    }

    [RelayCommand]
    private void ToggleValidationSummary()
    {
        IsValidationSummaryExpanded = !IsValidationSummaryExpanded;
    }

    /// <summary>
    /// Helper method to reset image selection state
    /// </summary>
    private void ResetImageSelection()
    {
        SelectedImageSource = null;
        HasSelectedImage = false;
        SelectedImagePath = string.Empty;
        _selectedImageStream?.Dispose();
        _selectedImageStream = null;
    }

    public void Dispose()
    {
        _selectedImageStream?.Dispose();
    }
}