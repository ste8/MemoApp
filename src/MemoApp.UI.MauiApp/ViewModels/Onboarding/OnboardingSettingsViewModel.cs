using MemoApp.Localization.Services;

namespace MemoApp.UI.MauiApp.ViewModels.Onboarding;

public partial class OnboardingSettingsViewModel : BaseViewModel
{
    public OnboardingSettingsViewModel(ILocalizationService localizationService)
    {
        Title = localizationService.GetString("OnboardingSettings_Title");
    }
}