using MemoApp.UI.MauiApp.Models.Onboarding;

namespace MemoApp.UI.MauiApp.Services.Onboarding;

/// <summary>
/// Service for providing language-specific Major System onboarding content
/// </summary>
public interface IMajorSystemOnboardingService
{
    /// <summary>
    /// Gets the Major System example for the current language
    /// </summary>
    /// <returns>The example showing number to word conversion</returns>
    MajorSystemExample GetExample();
    
    /// <summary>
    /// Gets the interactive practice quiz for the current language
    /// </summary>
    /// <returns>The quiz with options appropriate for the current language</returns>
    InteractivePractice GetInteractivePractice();
}