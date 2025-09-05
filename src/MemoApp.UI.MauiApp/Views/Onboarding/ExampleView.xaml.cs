using MemoApp.UI.MauiApp.ViewModels.Onboarding;

namespace MemoApp.UI.MauiApp.Views.Onboarding;

public partial class ExampleView : ContentView
{
    public ExampleView(ExampleViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
        
        // Initialize the example data
        viewModel.LoadExample();
        viewModel.LoadLocalizedText();
        
        Loaded += OnLoaded;
    }

    private async void OnLoaded(object sender, EventArgs e)
    {
        // Animate the conversion steps - first animate individual step frames
        var conversionSteps = ConversionSteps.Children.OfType<Frame>().ToList();
        
        await Task.Delay(500);
        foreach (var step in conversionSteps)
        {
            await step.FadeTo(1, 500);
            await Task.Delay(300);
        }
        
        // Then animate the final word step
        await Task.Delay(200);
        await Step3.FadeTo(1, 500);
        
        // Finally show the result image
        await Task.Delay(800);
        await ResultImage.FadeTo(1, 500);
    }
}