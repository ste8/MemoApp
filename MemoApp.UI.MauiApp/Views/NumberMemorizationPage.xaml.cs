using MemoApp.UI.MauiApp.ViewModels;

namespace MemoApp.UI.MauiApp.Views;

public partial class NumberMemorizationPage : ContentPage
{
    public NumberMemorizationPage(NumberMemorizationViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        
        if (BindingContext is NumberMemorizationViewModel viewModel)
        {
            await viewModel.InitializeAsync();
        }
    }
}