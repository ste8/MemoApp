namespace MemoApp.UI.MauiApp.Views.Onboarding;

public partial class ExampleView : ContentView
{
    public ExampleView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private async void OnLoaded(object sender, EventArgs e)
    {
        // Animate the conversion steps
        await Task.Delay(500);
        await Step1.FadeTo(1, 500);
        
        await Task.Delay(500);
        await Step2.FadeTo(1, 500);
        
        await Task.Delay(500);
        await Step3.FadeTo(1, 500);
        
        await Task.Delay(800);
        await ResultImage.FadeTo(1, 500);
    }
}