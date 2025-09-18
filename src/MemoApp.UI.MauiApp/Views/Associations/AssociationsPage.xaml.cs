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

    private void OnCategoryPickerContainerTapped(object? sender, EventArgs e)
    {
        try
        {
            // Focus the category picker to open the dropdown
            CategoryPicker?.Focus();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error focusing category picker: {ex.Message}");
        }
    }
}