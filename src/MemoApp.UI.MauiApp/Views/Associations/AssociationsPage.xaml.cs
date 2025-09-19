using MemoApp.UI.MauiApp.ViewModels.Associations;

namespace MemoApp.UI.MauiApp.Views.Associations;

/// <summary>
/// Code-behind for the Associations page
/// </summary>
public partial class AssociationsPage : ContentPage
{
    public AssociationsPage(AssociationsViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

}