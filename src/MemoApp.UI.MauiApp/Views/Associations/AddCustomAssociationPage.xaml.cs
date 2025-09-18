using MemoApp.UI.MauiApp.ViewModels.Associations;

namespace MemoApp.UI.MauiApp.Views.Associations;

/// <summary>
/// Page for adding custom associations to tokens
/// </summary>
public partial class AddCustomAssociationPage : ContentPage
{
    public AddCustomAssociationPage(AddCustomAssociationViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}