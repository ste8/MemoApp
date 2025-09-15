using MemoApp.UI.MauiApp.ViewModels;

namespace MemoApp.UI.MauiApp.Views;

public partial class FeedbackPage : ContentPage
{
    public FeedbackPage(FeedbackViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}