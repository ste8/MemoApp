using CommunityToolkit.Mvvm.Messaging;
using MemoApp.UI.MauiApp.ViewModels;

namespace MemoApp.UI.MauiApp.Views;

public partial class FeedbackPage : ContentPage
{
    private double _savedScrollPosition = 0;

    public FeedbackPage(FeedbackViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;

        // Register for scroll to field messages
        WeakReferenceMessenger.Default.Register<ScrollToFieldMessage>(this, async (r, m) =>
        {
            await ScrollToField(m.FieldName);
        });

        // Register for scroll position preservation messages
        WeakReferenceMessenger.Default.Register<PreserveScrollPositionMessage>(this, (r, m) =>
        {
            _savedScrollPosition = MainScrollView.ScrollY;
        });

        WeakReferenceMessenger.Default.Register<RestoreScrollPositionMessage>(this, async (r, m) =>
        {
            await Task.Delay(100); // Allow UI to stabilize
            await MainScrollView.ScrollToAsync(0, _savedScrollPosition, true);
        });

        WeakReferenceMessenger.Default.Register<ScrollToAttachedImagesMessage>(this, async (r, m) =>
        {
            await ScrollToAttachedImages();
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

    private async Task ScrollToAttachedImages()
    {
        try
        {
            // Find the attached images gallery section
            var imagesSection = this.FindByName<View>("AttachedImagesGallery");
            if (imagesSection != null)
            {
                await MainScrollView.ScrollToAsync(imagesSection, ScrollToPosition.MakeVisible, true);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error scrolling to attached images: {ex.Message}");
        }
    }


    protected override void OnDisappearing()
    {
        // Unregister messenger to prevent memory leaks
        WeakReferenceMessenger.Default.Unregister<ScrollToFieldMessage>(this);
        WeakReferenceMessenger.Default.Unregister<PreserveScrollPositionMessage>(this);
        WeakReferenceMessenger.Default.Unregister<RestoreScrollPositionMessage>(this);
        WeakReferenceMessenger.Default.Unregister<ScrollToAttachedImagesMessage>(this);
        base.OnDisappearing();
    }
}