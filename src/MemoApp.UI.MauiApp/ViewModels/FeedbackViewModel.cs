using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MemoApp.Core.Models;
using MemoApp.Localization.Services;
using MemoApp.UI.MauiApp.Services;
using Microsoft.Extensions.Logging;

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

    [ObservableProperty]
    private string userEmail = string.Empty;

    [ObservableProperty]
    private string? userName;

    [ObservableProperty]
    private bool includeScreenshot = false;

    [ObservableProperty]
    private bool isSubmitting = false;

    [ObservableProperty]
    private bool hasScreenshot = false;

    [ObservableProperty]
    private string? screenshotFileName;

    [ObservableProperty]
    private string? validationError;

    [ObservableProperty]
    private bool isFormValid = false;

    private byte[]? _screenshotData;
    private readonly Dictionary<string, string> _validationErrors = new();
    private bool _disposed;

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
    }

    private void OnPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(Subject) or nameof(Message) or nameof(UserEmail) or nameof(SelectedType))
        {
            ValidateForm();
        }
    }

    [RelayCommand]
    private async Task CaptureScreenshotAsync()
    {
        if (IsSubmitting || IsBusy) return;

        try
        {
            IsBusy = true;
            ValidationError = null;

            var result = await _feedbackService.CaptureScreenshotAsync();
            if (result.HasValue)
            {
                _screenshotData = result.Value.Data;
                ScreenshotFileName = result.Value.FileName;
                HasScreenshot = true;
                IncludeScreenshot = true;

                _logger.LogInformation("Screenshot captured successfully: {FileName}", ScreenshotFileName);
            }
            else
            {
                ValidationError = _localizationService.GetString("Feedback_ScreenshotError");
                HasScreenshot = false;
                IncludeScreenshot = false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error capturing screenshot");
            ValidationError = _localizationService.GetString("Feedback_ScreenshotError");
            HasScreenshot = false;
            IncludeScreenshot = false;
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void RemoveScreenshot()
    {
        _screenshotData = null;
        ScreenshotFileName = null;
        HasScreenshot = false;
        IncludeScreenshot = false;

        _logger.LogInformation("Screenshot removed");
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

            var feedback = new FeedbackModel
            {
                Subject = Subject.Trim(),
                Message = Message.Trim(),
                Type = SelectedType.Type,
                UserEmail = UserEmail.Trim(),
                UserName = string.IsNullOrWhiteSpace(UserName) ? null : UserName.Trim(),
                IncludeScreenshot = IncludeScreenshot && HasScreenshot,
                ScreenshotData = IncludeScreenshot && HasScreenshot ? _screenshotData : null,
                ScreenshotFileName = IncludeScreenshot && HasScreenshot ? ScreenshotFileName : null,
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

    private void ValidateForm()
    {
        _validationErrors.Clear();
        ValidationError = null;

        // Validate subject
        if (string.IsNullOrWhiteSpace(Subject))
        {
            _validationErrors["Subject"] = _localizationService.GetString("Feedback_ValidationSubjectRequired");
        }
        else if (Subject.Trim().Length < 5 || Subject.Trim().Length > 200)
        {
            _validationErrors["Subject"] = _localizationService.GetString("Feedback_ValidationSubjectLength");
        }

        // Validate message
        if (string.IsNullOrWhiteSpace(Message))
        {
            _validationErrors["Message"] = _localizationService.GetString("Feedback_ValidationMessageRequired");
        }
        else if (Message.Trim().Length < 10 || Message.Trim().Length > 2000)
        {
            _validationErrors["Message"] = _localizationService.GetString("Feedback_ValidationMessageLength");
        }

        // Validate email (required)
        if (string.IsNullOrWhiteSpace(UserEmail))
        {
            _validationErrors["Email"] = _localizationService.GetString("Feedback_ValidationEmailRequired");
        }
        else
        {
            var emailValidator = new EmailAddressAttribute();
            if (!emailValidator.IsValid(UserEmail))
            {
                _validationErrors["Email"] = _localizationService.GetString("Feedback_ValidationEmailInvalid");
            }
        }

        // Update validation state
        IsFormValid = _validationErrors.Count == 0;

        // Show first validation error
        if (_validationErrors.Count > 0)
        {
            ValidationError = _validationErrors.Values.First();
        }

        // Update submit command state
        SubmitFeedbackCommand.NotifyCanExecuteChanged();
    }

    private bool CanSubmit() => IsFormValid && !IsSubmitting && !IsBusy;

    private void ResetForm()
    {
        Subject = string.Empty;
        Message = string.Empty;
        SelectedType = FeedbackTypes.First(t => t.Type == FeedbackType.Other);
        UserEmail = string.Empty;
        UserName = null;
        IncludeScreenshot = false;
        RemoveScreenshot();
        ValidationError = null;
        _validationErrors.Clear();
        IsFormValid = false;
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
            // Clear screenshot data to free memory
            _screenshotData = null;
            ScreenshotFileName = null;
            HasScreenshot = false;

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