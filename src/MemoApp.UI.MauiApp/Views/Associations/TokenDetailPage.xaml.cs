using MemoApp.UI.MauiApp.ViewModels.Associations;

namespace MemoApp.UI.MauiApp.Views.Associations;

public partial class TokenDetailPage : ContentPage
{
    public TokenDetailPage(TokenDetailViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}