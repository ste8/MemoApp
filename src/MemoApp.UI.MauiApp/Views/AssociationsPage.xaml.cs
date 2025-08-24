using MemoApp.UI.MauiApp.ViewModels;

namespace MemoApp.UI.MauiApp.Views;

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