using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MemoApp.Localization.Services;
using MemoApp.UI.MauiApp.Models.Onboarding;
using MemoApp.UI.MauiApp.Services.Onboarding;
using Microsoft.Extensions.Logging;

namespace MemoApp.UI.MauiApp.ViewModels.Onboarding;

/// <summary>
/// ViewModel for the interactive practice quiz view
/// </summary>
public partial class InteractivePracticeViewModel : ObservableObject
{
    private readonly IMajorSystemOnboardingService _onboardingService;
    private readonly ILocalizationService _localizationService;
    private readonly ILogger<InteractivePracticeViewModel> _logger;

    [ObservableProperty]
    private InteractivePractice? _practice;

    [ObservableProperty]
    private string _questionText = string.Empty;

    [ObservableProperty]
    private string _feedbackText = string.Empty;

    [ObservableProperty]
    private Color _feedbackColor = Colors.Transparent;

    [ObservableProperty]
    private bool _isFeedbackVisible;

    [ObservableProperty]
    private bool _isAnswered;

    [ObservableProperty]
    private int _correctAnswerIndex = -1;

    [ObservableProperty]
    private int _selectedAnswerIndex = -1;

    [ObservableProperty]
    private string _option0Text = string.Empty;

    [ObservableProperty]
    private string _option1Text = string.Empty;

    [ObservableProperty]
    private string _option2Text = string.Empty;

    [ObservableProperty]
    private Color _option0Color = Colors.Gray;
    
    [ObservableProperty]
    private Color _option1Color = Colors.Gray;
    
    [ObservableProperty]
    private Color _option2Color = Colors.Gray;

    public InteractivePracticeViewModel(
        IMajorSystemOnboardingService onboardingService,
        ILocalizationService localizationService,
        ILogger<InteractivePracticeViewModel> logger)
    {
        _onboardingService = onboardingService;
        _localizationService = localizationService;
        _logger = logger;

        LoadPractice();
        LoadLocalizedText();
        
        // Subscribe to culture changes
        _localizationService.CultureChanged += OnCultureChanged;
    }

    /// <summary>
    /// Loads the appropriate practice quiz for the current language
    /// </summary>
    public void LoadPractice()
    {
        try
        {
            Practice = _onboardingService.GetInteractivePractice();
            
            // Find the correct answer index
            if (Practice != null)
            {
                CorrectAnswerIndex = Practice.Options.FindIndex(o => o.IsCorrect);
                _logger.LogDebug("Loaded practice for number {Number}, correct answer: {Index}", 
                    Practice.Number, CorrectAnswerIndex);
            }
            
            // Initialize colors to default gray
            var defaultColor = Application.Current?.Resources.TryGetValue("Gray100", out var gray) == true 
                ? (Color)gray 
                : Colors.LightGray;
            Option0Color = defaultColor;
            Option1Color = defaultColor;
            Option2Color = defaultColor;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load interactive practice");
            // Fallback to English practice
            Practice = CreateFallbackPractice();
        }
    }

    /// <summary>
    /// Loads the localized question text and option texts
    /// </summary>
    public void LoadLocalizedText()
    {
        try
        {
            if (Practice != null)
            {
                QuestionText = _localizationService.GetString(Practice.QuestionKey);
                
                // Load option texts
                if (Practice.Options.Count >= 3)
                {
                    Option0Text = _localizationService.GetString(Practice.Options[0].TextKey);
                    Option1Text = _localizationService.GetString(Practice.Options[1].TextKey);
                    Option2Text = _localizationService.GetString(Practice.Options[2].TextKey);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load localized text");
            QuestionText = "Which image represents this number?";
            Option0Text = "Option 1";
            Option1Text = "Option 2";
            Option2Text = "Option 3";
        }
    }

    /// <summary>
    /// Command to handle option selection
    /// </summary>
    [RelayCommand]
    public async Task SelectOption(object parameter)
    {
        _logger.LogDebug("SelectOption called with parameter: {Parameter} (Type: {Type})", 
            parameter, parameter?.GetType()?.Name ?? "null");
            
        if (IsAnswered || Practice == null)
        {
            _logger.LogDebug("SelectOption ignored - IsAnswered: {IsAnswered}, Practice null: {PracticeNull}", 
                IsAnswered, Practice == null);
            return;
        }
            
        // Handle both string and int parameters
        int optionIndex;
        if (parameter is string strParam)
        {
            if (!int.TryParse(strParam, out optionIndex))
            {
                _logger.LogWarning("Failed to parse parameter as int: {Parameter}", strParam);
                return;
            }
        }
        else if (parameter is int intParam)
        {
            optionIndex = intParam;
        }
        else
        {
            _logger.LogWarning("Parameter is not string or int: {Type}", parameter?.GetType()?.Name);
            return;
        }
        
        _logger.LogDebug("Processing option selection: Index={Index}", optionIndex);

        try
        {
            if (optionIndex < 0 || optionIndex >= Practice.Options.Count)
            {
                _logger.LogWarning("Invalid option index: {Index}", optionIndex);
                return;
            }

            IsAnswered = true;
            SelectedAnswerIndex = optionIndex;
            var selectedOption = Practice.Options[optionIndex];
            
            _logger.LogDebug("Option selected: {Index}, IsCorrect: {IsCorrect}", 
                optionIndex, selectedOption.IsCorrect);

            // Set the color for the selected option
            var selectedColor = selectedOption.IsCorrect ? Colors.Green : Colors.Red;
            switch (optionIndex)
            {
                case 0:
                    Option0Color = selectedColor;
                    break;
                case 1:
                    Option1Color = selectedColor;
                    break;
                case 2:
                    Option2Color = selectedColor;
                    break;
            }

            if (selectedOption.IsCorrect)
            {
                await HandleCorrectAnswer();
            }
            else
            {
                await HandleIncorrectAnswer();
                
                // After showing incorrect feedback, highlight the correct answer
                await Task.Delay(1000);
                switch (CorrectAnswerIndex)
                {
                    case 0:
                        Option0Color = Colors.Green;
                        break;
                    case 1:
                        Option1Color = Colors.Green;
                        break;
                    case 2:
                        Option2Color = Colors.Green;
                        break;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling option selection");
            await HandleFallbackFeedback();
        }
    }

    /// <summary>
    /// Gets the localized text for an option
    /// </summary>
    public string GetOptionText(QuizOption option)
    {
        try
        {
            return _localizationService.GetString(option.TextKey);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get option text for key: {Key}", option.TextKey);
            return option.Word; // Fallback to the word itself
        }
    }

    /// <summary>
    /// Resets the quiz state
    /// </summary>
    [RelayCommand]
    public void Reset()
    {
        IsAnswered = false;
        IsFeedbackVisible = false;
        FeedbackText = string.Empty;
        FeedbackColor = Colors.Transparent;
        SelectedAnswerIndex = -1;
        
        // Reset colors to default
        var defaultColor = Application.Current?.Resources.TryGetValue("Gray100", out var gray) == true 
            ? (Color)gray 
            : Colors.LightGray;
        Option0Color = defaultColor;
        Option1Color = defaultColor;
        Option2Color = defaultColor;
    }

    private async Task HandleCorrectAnswer()
    {
        if (Practice == null) return;

        FeedbackText = _localizationService.GetString(Practice.CorrectFeedbackKey);
        FeedbackColor = Colors.Green;
        IsFeedbackVisible = true;
        
        await Task.Delay(100); // Small delay for UI update
    }

    private async Task HandleIncorrectAnswer()
    {
        if (Practice == null) return;

        FeedbackText = _localizationService.GetString(Practice.TryAgainFeedbackKey);
        FeedbackColor = Colors.Red;
        IsFeedbackVisible = true;
        
        // Show correct answer after a delay
        await Task.Delay(1000);
        
        // The UI will handle highlighting the correct answer based on CorrectAnswerIndex
    }

    private async Task HandleFallbackFeedback()
    {
        FeedbackText = _localizationService.GetString("Onboarding_Practice_FallbackFeedback");
        FeedbackColor = Colors.Blue;
        IsFeedbackVisible = true;
        
        await Task.Delay(100);
    }

    private static InteractivePractice CreateFallbackPractice()
    {
        return new InteractivePractice
        {
            Number = 32,
            QuestionKey = "Onboarding_Practice_Question",
            CorrectFeedbackKey = "Onboarding_Practice_Correct",
            TryAgainFeedbackKey = "Onboarding_Practice_TryAgain",
            Options = new List<QuizOption>
            {
                new() { Emoji = "🌙", TextKey = "Onboarding_Moon", Word = "Moon", IsCorrect = true },
                new() { Emoji = "👩", TextKey = "Onboarding_Mum", Word = "Mum", IsCorrect = false },
                new() { Emoji = "🧅", TextKey = "Onboarding_Onion", Word = "Onion", IsCorrect = false }
            }
        };
    }

    /// <summary>
    /// Handles culture change events to reload content
    /// </summary>
    private void OnCultureChanged(object? sender, CultureInfo e)
    {
        try
        {
            Reset();
            LoadPractice();
            LoadLocalizedText();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to reload content after culture change");
        }
    }

    ~InteractivePracticeViewModel()
    {
        _localizationService.CultureChanged -= OnCultureChanged;
    }
}