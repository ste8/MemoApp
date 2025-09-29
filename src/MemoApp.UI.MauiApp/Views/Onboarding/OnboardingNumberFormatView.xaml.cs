using MemoApp.UI.MauiApp.ViewModels.Onboarding;

namespace MemoApp.UI.MauiApp.Views.Onboarding;

public partial class OnboardingNumberFormatView : ContentView
{
    public OnboardingNumberFormatView(OnboardingNumberFormatViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}