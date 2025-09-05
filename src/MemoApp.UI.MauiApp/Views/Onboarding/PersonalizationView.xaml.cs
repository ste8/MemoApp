using MemoApp.UI.MauiApp.ViewModels.Onboarding;

namespace MemoApp.UI.MauiApp.Views.Onboarding;

public partial class PersonalizationView : ContentView
{
    public PersonalizationView(PersonalizationViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}