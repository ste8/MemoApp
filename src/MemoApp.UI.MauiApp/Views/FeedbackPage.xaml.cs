using CommunityToolkit.Mvvm.Messaging;
using MemoApp.UI.MauiApp.ViewModels;

namespace MemoApp.UI.MauiApp.Views;

public partial class FeedbackPage : ContentPage
{
    public FeedbackPage(FeedbackViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;

        // Register for scroll to field messages
        WeakReferenceMessenger.Default.Register<ScrollToFieldMessage>(this, async (r, m) =>
        {
            await ScrollToField(m.FieldName);
        });
    }

    private async Task ScrollToField(string fieldName)
    {
        try
        {
            // Find the appropriate field and scroll to it
            View? targetView = fieldName switch
            {
                "Subject" => this.FindByName<View>("SubjectContainer"),
                "Message" => this.FindByName<View>("MessageContainer"),
                "Email" => this.FindByName<View>("EmailContainer"),
                _ => null
            };

            if (targetView != null)
            {
                // Scroll to the target view
                await MainScrollView.ScrollToAsync(targetView, ScrollToPosition.Start, true);

                // Optional: Focus the field if it's an Entry or Editor
                if (targetView is Layout layout && layout.Children.FirstOrDefault() is InputView inputView)
                {
                    await Task.Delay(300); // Allow scroll animation to complete
                    inputView.Focus();
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error scrolling to field: {ex.Message}");
        }
    }

    protected override void OnDisappearing()
    {
        // Unregister messenger to prevent memory leaks
        WeakReferenceMessenger.Default.Unregister<ScrollToFieldMessage>(this);
        base.OnDisappearing();
    }
}