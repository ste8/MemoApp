using MemoApp.UI.MauiApp.ViewModels.Associations;

namespace MemoApp.UI.MauiApp.Views.Associations;

/// <summary>
/// Page for adding custom associations to tokens
/// </summary>
public partial class AddCustomAssociationPage : ContentPage
{
    private readonly AddCustomAssociationViewModel _viewModel;

    public AddCustomAssociationPage(AddCustomAssociationViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;

        // Subscribe to scroll to first error command
        _viewModel.ScrollToFirstErrorCommand.CanExecuteChanged += OnScrollToFirstErrorCanExecuteChanged;
    }

    private void OnScrollToFirstErrorCanExecuteChanged(object? sender, EventArgs e)
    {
        // Handle scroll to first error when command is executed
        if (_viewModel.ScrollToFirstErrorCommand.CanExecute(null))
        {
            _ = ScrollToFirstErrorAsync();
        }
    }

    private async Task ScrollToFirstErrorAsync()
    {
        try
        {
            // Scroll to the first field with an error
            if (!string.IsNullOrEmpty(_viewModel.WordError))
            {
                // Scroll to word container and focus the entry
                await MainScrollView.ScrollToAsync(WordContainer, ScrollToPosition.Start, true);

                // Try to focus the word entry
                if (WordEntry != null)
                {
                    WordEntry.Focus();
                }
            }
            else if (!string.IsNullOrEmpty(_viewModel.ImageError))
            {
                // Scroll to image container
                await MainScrollView.ScrollToAsync(ImageContainer, ScrollToPosition.Start, true);
            }
        }
        catch (Exception)
        {
            // Silently handle any scrolling errors
        }
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _viewModel.ScrollToFirstErrorCommand.CanExecuteChanged -= OnScrollToFirstErrorCanExecuteChanged;
    }
}