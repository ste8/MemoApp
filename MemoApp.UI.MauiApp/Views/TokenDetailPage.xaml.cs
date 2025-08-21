using MemoApp.UI.MauiApp.ViewModels;

namespace MemoApp.UI.MauiApp.Views;

public partial class TokenDetailPage : ContentPage
{
    public TokenDetailPage(TokenDetailViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}