using MemoApp.UI.MauiApp.ViewModels;

namespace MemoApp.UI.MauiApp.Views;

public partial class BigNumberPage : ContentPage
{
    public BigNumberPage(BigNumberViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        
        if (BindingContext is BigNumberViewModel viewModel)
        {
            // Set the container element reference for accurate size measurement
            viewModel.SetContainerElement(NumberDisplayFrame);
            
            await viewModel.InitializeAsync();
        }
    }
}