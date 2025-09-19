using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using MemoApp.Core.Models;
using MemoApp.Localization.Services;
using MemoApp.UI.MauiApp.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.Controls;

namespace MemoApp.UI.MauiApp.ViewModels;

public partial class FeedbackViewModel : BaseViewModel, IDisposable
{
    private readonly IFeedbackService _feedbackService;
    private readonly ILocalizationService _localizationService;
    private readonly ILogger<FeedbackViewModel> _logger;

    [ObservableProperty]
    private string subject = string.Empty;

    [ObservableProperty]
    private string message = string.Empty;

    [ObservableProperty]
    private FeedbackTypeItem selectedType;

    public string SelectedTypeDisplayName => SelectedType?.DisplayName ?? string.Empty;

    partial void OnSelectedTypeChanged(FeedbackTypeItem value)
    {
        OnPropertyChanged(nameof(SelectedTypeDisplayName));
    }

    [ObservableProperty]
    private string userEmail = string.Empty;

    [ObservableProperty]
    private string? userName;

    [ObservableProperty]
    private bool isSubmitting = false;

    [ObservableProperty]
    private bool isAttachingImages = false;

    public bool IsAnyOperationInProgress => IsSubmitting || IsAttachingImages || IsBusy;

    public string LoadingMessage
    {
        get
        {
            try
            {
                if (IsSubmitting)
                    return _localizationService.GetString("Feedback_Submitting");
                if (IsAttachingImages)
                    return _localizationService.GetString("Feedback_AttachingImages");
                return _localizationService.GetString("Common_Loading");
            }
            catch
            {
                // Fallback in case of localization service issues
                if (IsSubmitting) return "Submitting feedback...";
                if (IsAttachingImages) return "Selecting images...";
                return "Loading...";
            }
        }
    }

    [ObservableProperty]
    private bool hasAttachedImages = false;

    [ObservableProperty]
    private int attachedImageCount = 0;

    [ObservableProperty]
    private string? validationError;

    [ObservableProperty]
    private bool isFormValid = false;

    // Field-specific validation errors
    [ObservableProperty]
    private string? subjectError;

    [ObservableProperty]
    private string? messageError;

    [ObservableProperty]
    private string? emailError;

    // Field validation states
    [ObservableProperty]
    private bool isSubjectValid = false;

    [ObservableProperty]
    private bool isMessageValid = false;

    [ObservableProperty]
    private bool isEmailValid = false;

    // Field interaction tracking
    [ObservableProperty]
    private bool hasSubjectBeenFocused = false;

    [ObservableProperty]
    private bool hasMessageBeenFocused = false;

    [ObservableProperty]
    private bool hasEmailBeenFocused = false;

    // Validation summary
    [ObservableProperty]
    private bool hasValidationErrors = false;

    [ObservableProperty]
    private int validationErrorCount = 0;

    [ObservableProperty]
    private bool isValidationSummaryExpanded = false;

    public ObservableCollection<AttachedImage> AttachedImages { get; } = new();
    private readonly Dictionary<string, string> _validationErrors = new();
    private bool _disposed;

    private const int MaxImageCount = 10;

    public ObservableCollection<FeedbackTypeItem> FeedbackTypes { get; }

    public FeedbackViewModel(
        IFeedbackService feedbackService,
        ILocalizationService localizationService,
        ILogger<FeedbackViewModel> logger)
    {
        _feedbackService = feedbackService ?? throw new ArgumentNullException(nameof(feedbackService));
        _localizationService = localizationService ?? throw new ArgumentNullException(nameof(localizationService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        Title = _localizationService.GetString("Feedback_Title");

        // Initialize feedback types
        FeedbackTypes = new ObservableCollection<FeedbackTypeItem>
        {
            new(FeedbackType.Bug, _localizationService.GetString("Feedback_Type_Bug")),
            new(FeedbackType.Feature, _localizationService.GetString("Feedback_Type_Feature")),
            new(FeedbackType.Other, _localizationService.GetString("Feedback_Type_Other"))
        };

        // Set default selection
        SelectedType = FeedbackTypes.First(t => t.Type == FeedbackType.Other);

        // Set up property change notifications for validation
        PropertyChanged += OnPropertyChanged;

        _logger.LogInformation("FeedbackViewModel initialized");

        // Initialize validation to show errors immediately on page load per user requirement
        InitializeValidation();
    }

    /// <summary>
    /// Initializes validation to show errors immediately when the page opens
    /// This makes validation errors visible from page start rather than waiting for user interaction
    /// </summary>
    private void InitializeValidation()
    {
        // Set all fields as "focused" so validation errors are visible immediately
        HasSubjectBeenFocused = true;
        HasMessageBeenFocused = true;
        HasEmailBeenFocused = true;

        // Run initial validation
        ValidateSubject();
        ValidateMessage();
        ValidateEmail();
        UpdateOverallValidation();
    }

    private void OnPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(Subject):
                HasSubjectBeenFocused = true;
                ValidateSubject();
                UpdateOverallValidation();
                break;
            case nameof(Message):
                HasMessageBeenFocused = true;
                ValidateMessage();
                UpdateOverallValidation();
                break;
            case nameof(UserEmail):
                HasEmailBeenFocused = true;
                ValidateEmail();
                UpdateOverallValidation();
                break;
            case nameof(SelectedType):
                ValidateForm();
                break;
            case nameof(IsSubmitting):
            case nameof(IsAttachingImages):
            case nameof(IsBusy):
                OnPropertyChanged(nameof(IsAnyOperationInProgress));
                OnPropertyChanged(nameof(LoadingMessage));
                break;
        }
    }

    private void UpdateOverallValidation()
    {
        _validationErrors.Clear();

        if (!string.IsNullOrEmpty(SubjectError))
            _validationErrors["Subject"] = SubjectError;
        if (!string.IsNullOrEmpty(MessageError))
            _validationErrors["Message"] = MessageError;
        if (!string.IsNullOrEmpty(EmailError))
            _validationErrors["Email"] = EmailError;

        IsFormValid = IsSubjectValid && IsMessageValid && IsEmailValid;
        HasValidationErrors = _validationErrors.Count > 0;
        ValidationErrorCount = _validationErrors.Count;

        // Show first validation error in summary
        ValidationError = _validationErrors.Count > 0 ? _validationErrors.Values.First() : null;

        // Update submit command state
        SubmitFeedbackCommand.NotifyCanExecuteChanged();
    }

    [RelayCommand]
    private async Task SelectFeedbackTypeAsync()
    {
        try
        {
            var typeNames = FeedbackTypes.Select(t => t.DisplayName).ToArray();
            var selectedTypeName = await Shell.Current.DisplayActionSheet(
                _localizationService.GetString("Feedback_TypeLabel"),
                _localizationService.GetString("Common_Cancel"),
                null,
                typeNames);

            if (!string.IsNullOrEmpty(selectedTypeName) &&
                selectedTypeName != _localizationService.GetString("Common_Cancel"))
            {
                var feedbackType = FeedbackTypes.FirstOrDefault(t => t.DisplayName == selectedTypeName);
                if (feedbackType != null)
                {
                    SelectedType = feedbackType;
                    _logger.LogInformation("Feedback type changed to: {TypeName}", selectedTypeName);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error selecting feedback type");
        }
    }

    [RelayCommand]
    private async Task AttachImagesAsync()
    {
        if (IsSubmitting || IsBusy) return;

        // Check if we've reached the maximum image count
        if (AttachedImages.Count >= MaxImageCount)
        {
            ValidationError = _localizationService.GetString("Feedback_TooManyImages");
            return;
        }

        try
        {
            _logger.LogInformation("Starting image attachment process");

            // Preserve scroll position before showing loading state
            WeakReferenceMessenger.Default.Send(new PreserveScrollPositionMessage());

            IsAttachingImages = true;
            ValidationError = null;

            _logger.LogInformation("Calling FeedbackService.PickImagesAsync()");
            var selectedImages = await _feedbackService.PickImagesAsync();
            _logger.LogInformation("PickImagesAsync returned {Count} images", selectedImages?.Count ?? 0);

            if (selectedImages?.Count > 0)
            {
                var availableSlots = MaxImageCount - AttachedImages.Count;
                var imagesToAdd = selectedImages.Take(availableSlots).ToList();

                foreach (var image in imagesToAdd)
                {
                    // Create ImageSource for UI display on main thread
                    await MainThread.InvokeOnMainThreadAsync(() =>
                    {
                        try
                        {
                            // Create a persistent MemoryStream for the ImageSource
                            var imageStream = new MemoryStream(image.Data);
                            image.ImageSource = ImageSource.FromStream(() => imageStream);
                            _logger.LogInformation("Created ImageSource for {FileName}, Data length: {Length}",
                                image.FileName, image.Data?.Length ?? 0);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Failed to create ImageSource for {FileName}", image.FileName);
                            // Set a placeholder or leave null - the UI should handle gracefully
                            image.ImageSource = null;
                        }
                    });

                    AttachedImages.Add(image);
                }

                UpdateAttachmentState();
                _logger.LogInformation("Images attached successfully: {Count} images", imagesToAdd.Count);

                // Warn if some images were skipped due to limit
                if (selectedImages.Count > imagesToAdd.Count)
                {
                    var skippedCount = selectedImages.Count - imagesToAdd.Count;
                    ValidationError = _localizationService.GetString("Feedback_SomeImagesSkipped")
                        .Replace("{0}", skippedCount.ToString())
                        .Replace("{1}", MaxImageCount.ToString());
                }
            }
            else
            {
                _logger.LogInformation("No images selected for attachment");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error attaching images");
            ValidationError = _localizationService.GetString("Feedback_AttachmentError");
        }
        finally
        {
            _logger.LogInformation("Setting IsAttachingImages = false");
            IsAttachingImages = false;
            _logger.LogInformation("IsAttachingImages is now: {Value}", IsAttachingImages);

            // Smart scroll behavior: if images were added, scroll to show them; otherwise restore position
            await Task.Delay(200); // Allow UI to update
            if (HasAttachedImages && AttachedImages.Count > 0)
            {
                // Images were successfully attached, scroll to show them
                WeakReferenceMessenger.Default.Send(new ScrollToAttachedImagesMessage());
            }
            else
            {
                // No images attached (user cancelled or error), restore original position
                WeakReferenceMessenger.Default.Send(new RestoreScrollPositionMessage());
            }
        }
    }

    [RelayCommand]
    private void RemoveImage(AttachedImage image)
    {
        if (AttachedImages.Contains(image))
        {
            // Dispose the ImageSource if it implements IDisposable
            if (image.ImageSource is IDisposable disposable)
            {
                disposable.Dispose();
            }
            image.ImageSource = null;

            AttachedImages.Remove(image);
            UpdateAttachmentState();
            _logger.LogInformation("Image removed: {FileName}", image.FileName);
        }
    }

    [RelayCommand]
    private void ClearAllImages()
    {
        // Dispose all image sources before clearing
        foreach (var image in AttachedImages.ToList())
        {
            if (image.ImageSource is IDisposable disposable)
            {
                disposable.Dispose();
            }
            image.ImageSource = null;
        }

        AttachedImages.Clear();
        UpdateAttachmentState();
        _logger.LogInformation("All attached images cleared");
    }

    private void UpdateAttachmentState()
    {
        AttachedImageCount = AttachedImages.Count;
        HasAttachedImages = AttachedImages.Count > 0;
    }

    [RelayCommand(CanExecute = nameof(CanSubmit))]
    private async Task SubmitFeedbackAsync()
    {
        if (IsSubmitting || !IsFormValid) return;

        try
        {
            IsSubmitting = true;
            IsBusy = true;
            ValidationError = null;

            // Create a copy of attached images without the ImageSource for API submission
            var apiImages = AttachedImages.Select(img => new AttachedImage
            {
                Data = img.Data,
                FileName = img.FileName,
                ContentType = img.ContentType,
                FileSize = img.FileSize,
                AttachedAt = img.AttachedAt
                // Deliberately exclude ImageSource as it's only for UI
            }).ToList();

            var feedback = new FeedbackModel
            {
                Subject = Subject.Trim(),
                Message = Message.Trim(),
                Type = SelectedType.Type,
                UserEmail = UserEmail.Trim(),
                UserName = string.IsNullOrWhiteSpace(UserName) ? null : UserName.Trim(),
                AttachedImages = apiImages,
                CreatedAt = DateTime.UtcNow
            };

            var success = await _feedbackService.SubmitFeedbackAsync(feedback);

            if (success)
            {
                await Shell.Current.DisplayAlert(
                    _localizationService.GetString("Feedback_Title"),
                    _localizationService.GetString("Feedback_SubmissionSuccess"),
                    _localizationService.GetString("Common_OK"));

                // Clear form after successful submission
                ResetForm();

                // Navigate back
                await Shell.Current.GoToAsync("..");
            }
            else
            {
                ValidationError = _localizationService.GetString("Feedback_SubmissionError");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error submitting feedback");
            ValidationError = _localizationService.GetString("Feedback_NetworkError");
        }
        finally
        {
            IsSubmitting = false;
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void SelectType(FeedbackType type)
    {
        SelectedType = FeedbackTypes.First(t => t.Type == type);
    }

    [RelayCommand]
    private async Task ScrollToFirstErrorAsync()
    {
        // This will be handled by the View to scroll to the first field with an error
        if (HasValidationErrors)
        {
            // Trigger haptic feedback on mobile platforms
#if ANDROID || IOS
            try
            {
                HapticFeedback.Default.Perform(HapticFeedbackType.Click);
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Haptic feedback not available");
            }
#endif

            // Send a message to the view to scroll to the first error
            var firstErrorField = GetFirstErrorField();
            if (!string.IsNullOrEmpty(firstErrorField))
            {
                WeakReferenceMessenger.Default.Send(new ScrollToFieldMessage(firstErrorField));
            }
        }
    }

    private string GetFirstErrorField()
    {
        if (!string.IsNullOrEmpty(SubjectError)) return "Subject";
        if (!string.IsNullOrEmpty(MessageError)) return "Message";
        if (!string.IsNullOrEmpty(EmailError)) return "Email";
        return string.Empty;
    }

    [RelayCommand]
    private async Task CancelAsync()
    {
        var hasContent = !string.IsNullOrWhiteSpace(Subject) || !string.IsNullOrWhiteSpace(Message);

        if (hasContent)
        {
            var result = await Shell.Current.DisplayAlert(
                _localizationService.GetString("Feedback_Title"),
                _localizationService.GetString("Common_UnsavedChanges"),
                _localizationService.GetString("Common_Discard"),
                _localizationService.GetString("Feedback_CancelButton"));

            if (!result) return; // User chose to stay
        }

        await Shell.Current.GoToAsync("..");
    }

    [RelayCommand]
    private void ToggleValidationSummary()
    {
        IsValidationSummaryExpanded = !IsValidationSummaryExpanded;
    }

    private void ValidateForm()
    {
        // Validate all fields at once (used for submit button)
        HasSubjectBeenFocused = true;
        HasMessageBeenFocused = true;
        HasEmailBeenFocused = true;

        ValidateSubject();
        ValidateMessage();
        ValidateEmail();
        UpdateOverallValidation();
    }

    private void ValidateSubject()
    {
        IsSubjectValid = false;
        SubjectError = null;

        if (!HasSubjectBeenFocused && string.IsNullOrWhiteSpace(Subject))
        {
            // Don't show error if field hasn't been focused yet
            return;
        }

        if (string.IsNullOrWhiteSpace(Subject))
        {
            SubjectError = _localizationService.GetString("Feedback_ValidationSubjectRequired");
        }
        else if (Subject.Trim().Length < 5)
        {
            SubjectError = _localizationService.GetString("Feedback_ValidationSubjectTooShort");
        }
        else if (Subject.Trim().Length > 200)
        {
            SubjectError = _localizationService.GetString("Feedback_ValidationSubjectTooLong");
        }
        else
        {
            SubjectError = null;
            IsSubjectValid = true;
        }
    }

    private void ValidateMessage()
    {
        IsMessageValid = false;
        MessageError = null;

        if (!HasMessageBeenFocused && string.IsNullOrWhiteSpace(Message))
        {
            // Don't show error if field hasn't been focused yet
            return;
        }

        if (string.IsNullOrWhiteSpace(Message))
        {
            MessageError = _localizationService.GetString("Feedback_ValidationMessageRequired");
        }
        else if (Message.Trim().Length < 10)
        {
            MessageError = _localizationService.GetString("Feedback_ValidationMessageTooShort");
        }
        else if (Message.Trim().Length > 2000)
        {
            MessageError = _localizationService.GetString("Feedback_ValidationMessageTooLong");
        }
        else
        {
            MessageError = null;
            IsMessageValid = true;
        }
    }

    private void ValidateEmail()
    {
        IsEmailValid = false;
        EmailError = null;

        if (!HasEmailBeenFocused && string.IsNullOrWhiteSpace(UserEmail))
        {
            // Don't show error if field hasn't been focused yet
            return;
        }

        if (string.IsNullOrWhiteSpace(UserEmail))
        {
            EmailError = _localizationService.GetString("Feedback_ValidationEmailRequired");
        }
        else
        {
            if (!IsValidEmailAddress(UserEmail))
            {
                EmailError = _localizationService.GetString("Feedback_ValidationEmailInvalid");
            }
            else
            {
                EmailError = null;
                IsEmailValid = true;
            }
        }
    }

    private static bool IsValidEmailAddress(string email)
    {
        try
        {
            // Trim whitespace
            email = email.Trim();

            // Basic format check: must contain @ and have parts before and after
            if (!email.Contains('@') || email.StartsWith('@') || email.EndsWith('@'))
                return false;

            // Split into local and domain parts
            var parts = email.Split('@');
            if (parts.Length != 2)
                return false;

            var localPart = parts[0];
            var domainPart = parts[1];

            // Validate local part (before @)
            if (string.IsNullOrWhiteSpace(localPart) || localPart.Length > 64)
                return false;

            // Local part cannot start or end with dot
            if (localPart.StartsWith('.') || localPart.EndsWith('.'))
                return false;

            // Local part cannot have consecutive dots
            if (localPart.Contains(".."))
                return false;

            // Validate domain part (after @)
            if (string.IsNullOrWhiteSpace(domainPart) || domainPart.Length > 253)
                return false;

            // Domain must contain at least one dot and cannot start/end with dot or hyphen
            if (!domainPart.Contains('.') || domainPart.StartsWith('.') || domainPart.EndsWith('.') ||
                domainPart.StartsWith('-') || domainPart.EndsWith('-'))
                return false;

            // Split domain into labels (parts separated by dots)
            var domainLabels = domainPart.Split('.');

            // Each domain label must be valid
            foreach (var label in domainLabels)
            {
                if (string.IsNullOrWhiteSpace(label) || label.Length > 63)
                    return false;

                // Label cannot start or end with hyphen
                if (label.StartsWith('-') || label.EndsWith('-'))
                    return false;

                // Label must contain only letters, digits, and hyphens
                foreach (char c in label)
                {
                    if (!char.IsLetterOrDigit(c) && c != '-')
                        return false;
                }
            }

            // Top-level domain (last label) must be at least 2 characters and contain only letters
            var tld = domainLabels[^1];
            if (tld.Length < 2)
                return false;

            foreach (char c in tld)
            {
                if (!char.IsLetter(c))
                    return false;
            }

            return true;
        }
        catch
        {
            return false;
        }
    }

    private bool CanSubmit() => IsFormValid && !IsSubmitting && !IsBusy;

    private void ResetForm()
    {
        Subject = string.Empty;
        Message = string.Empty;
        SelectedType = FeedbackTypes.First(t => t.Type == FeedbackType.Other);
        UserEmail = string.Empty;
        UserName = null;
        ClearAllImages();
        ValidationError = null;
        _validationErrors.Clear();
        IsFormValid = false;

        // Reset field-specific validation state
        SubjectError = null;
        MessageError = null;
        EmailError = null;
        IsSubjectValid = false;
        IsMessageValid = false;
        IsEmailValid = false;
        HasSubjectBeenFocused = false;
        HasMessageBeenFocused = false;
        HasEmailBeenFocused = false;
        HasValidationErrors = false;
        ValidationErrorCount = 0;
        IsValidationSummaryExpanded = false;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed && disposing)
        {
            // Dispose all image sources before clearing
            foreach (var image in AttachedImages.ToList())
            {
                if (image.ImageSource is IDisposable disposable)
                {
                    disposable.Dispose();
                }
                image.ImageSource = null;
            }

            // Clear attached images to free memory
            AttachedImages.Clear();
            HasAttachedImages = false;
            AttachedImageCount = 0;

            // Clear form data
            Subject = string.Empty;
            Message = string.Empty;
            UserEmail = null;
            UserName = null;

            // Remove event handlers
            PropertyChanged -= OnPropertyChanged;

            _disposed = true;
        }
    }
}

public record FeedbackTypeItem(FeedbackType Type, string DisplayName);

// Message for scroll to field communication
public record ScrollToFieldMessage(string FieldName);
public record PreserveScrollPositionMessage();
public record RestoreScrollPositionMessage();
public record ScrollToAttachedImagesMessage();