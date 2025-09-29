using MemoApp.UI.MauiApp.ViewModels.Onboarding;

namespace MemoApp.UI.MauiApp.Views.Onboarding;

public partial class OnboardingSettingsView : ContentView
{
    public OnboardingSettingsView(OnboardingSettingsViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}