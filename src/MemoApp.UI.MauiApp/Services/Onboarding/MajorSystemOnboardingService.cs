using MemoApp.Localization.Services;
using MemoApp.UI.MauiApp.Models.Onboarding;

namespace MemoApp.UI.MauiApp.Services.Onboarding;

/// <summary>
/// Service implementation for providing language-specific Major System onboarding content
/// </summary>
public class MajorSystemOnboardingService : IMajorSystemOnboardingService
{
    private readonly ILocalizationService _localizationService;

    public MajorSystemOnboardingService(ILocalizationService localizationService)
    {
        _localizationService = localizationService;
    }

    /// <summary>
    /// Gets the Major System example for the current language
    /// </summary>
    /// <returns>The example showing number to word conversion</returns>
    public MajorSystemExample GetExample()
    {
        var currentCulture = _localizationService.CurrentCulture.Name;
        
        if (currentCulture.StartsWith("it", StringComparison.OrdinalIgnoreCase))
        {
            return GetItalianExample();
        }
        
        return GetEnglishExample();
    }

    /// <summary>
    /// Gets the interactive practice quiz for the current language
    /// </summary>
    /// <returns>The quiz with options appropriate for the current language</returns>
    public InteractivePractice GetInteractivePractice()
    {
        var currentCulture = _localizationService.CurrentCulture.Name;
        
        if (currentCulture.StartsWith("it", StringComparison.OrdinalIgnoreCase))
        {
            return GetItalianPractice();
        }
        
        return GetEnglishPractice();
    }

    private static MajorSystemExample GetEnglishExample()
    {
        return new MajorSystemExample
        {
            Number = 41,
            Word = "RAT",
            Emoji = "🐀",
            RememberTextKey = "Onboarding_Example_Remember",
            ConversionSteps = new List<ConversionStep>
            {
                new() { Digit = 4, Letter = "R" },
                new() { Digit = 1, Letter = "T" }
            }
        };
    }

    private static MajorSystemExample GetItalianExample()
    {
        return new MajorSystemExample
        {
            Number = 31,
            Word = "MOTO",
            Emoji = "🏍️",
            RememberTextKey = "Onboarding_Example_Remember",
            ConversionSteps = new List<ConversionStep>
            {
                new() { Digit = 3, Letter = "M" },
                new() { Digit = 1, Letter = "T" }
            }
        };
    }

    private static InteractivePractice GetEnglishPractice()
    {
        return new InteractivePractice
        {
            Number = 32,
            QuestionKey = "Onboarding_Practice_Question",
            CorrectFeedbackKey = "Onboarding_Practice_Correct",
            TryAgainFeedbackKey = "Onboarding_Practice_TryAgain",
            Options = new List<QuizOption>
            {
                new() 
                { 
                    Emoji = "🌙", 
                    TextKey = "Onboarding_Moon", 
                    Word = "Moon",
                    IsCorrect = true 
                },
                new() 
                { 
                    Emoji = "👩", 
                    TextKey = "Onboarding_Mum", 
                    Word = "Mum",
                    IsCorrect = false 
                },
                new() 
                { 
                    Emoji = "🧅", 
                    TextKey = "Onboarding_Onion", 
                    Word = "Onion",
                    IsCorrect = false 
                }
            }
        };
    }

    private static InteractivePractice GetItalianPractice()
    {
        return new InteractivePractice
        {
            Number = 32,
            QuestionKey = "Onboarding_Practice_Question",
            CorrectFeedbackKey = "Onboarding_Practice_Correct",
            TryAgainFeedbackKey = "Onboarding_Practice_TryAgain",
            Options = new List<QuizOption>
            {
                new() 
                { 
                    Emoji = "✋", 
                    TextKey = "Onboarding_Mano", 
                    Word = "Mano",
                    IsCorrect = true 
                },
                new() 
                { 
                    Emoji = "👩", 
                    TextKey = "Onboarding_Mamma", 
                    Word = "Mamma",
                    IsCorrect = false 
                },
                new() 
                { 
                    Emoji = "🐸", 
                    TextKey = "Onboarding_Rana", 
                    Word = "Rana",
                    IsCorrect = false 
                }
            }
        };
    }
}