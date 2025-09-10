using MemoApp.UI.MauiApp.ViewModels.Onboarding;

namespace MemoApp.UI.MauiApp.Views.Onboarding;

public partial class MajorSystemExampleView : ContentView
{
    public MajorSystemExampleView(MajorSystemExampleViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    protected override async void OnBindingContextChanged()
    {
        base.OnBindingContextChanged();
        
        if (BindingContext is MajorSystemExampleViewModel viewModel)
        {
            await viewModel.InitializeAsync();
        }
    }
}