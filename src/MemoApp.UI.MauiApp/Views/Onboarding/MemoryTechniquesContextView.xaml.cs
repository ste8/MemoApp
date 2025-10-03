using MemoApp.UI.MauiApp.ViewModels.Onboarding;

namespace MemoApp.UI.MauiApp.Views.Onboarding;

public partial class MemoryTechniquesContextView : ContentView
{
    public MemoryTechniquesContextView(MemoryTechniquesContextViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
