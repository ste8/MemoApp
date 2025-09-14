/*
 * Example code-behind for ShareButtonComponent usage
 * This demonstrates how to initialize the component with ViewModels
 *
 * NOTE: This is an example file for documentation purposes only.
 * The actual integration is shown in BigNumberPage.xaml.cs.
 */

using MemoApp.UI.MauiApp.ViewModels.Components;

namespace MemoApp.UI.MauiApp.Views.Components;

public partial class ExampleManualVerificationUsage : ContentView
{
    public ExampleManualVerificationUsage()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Example of how to initialize ShareButton components with ViewModels
    /// This would typically be called from a parent view's constructor or OnAppearing
    /// </summary>
    /// <param name="serviceProvider">The service provider for dependency injection</param>
    public void InitializeShareButtons(IServiceProvider serviceProvider)
    {
        // Example: Get ShareButtonViewModel from service provider
        var shareVM = serviceProvider.GetService<ShareButtonViewModel>();

        if (shareVM != null && shareVM != null)
        {
            // Note: In the actual XAML, you would give the ShareButtonComponent a name (x:Name)
            // Then you could call SetViewModel on it like this:

            // ExampleShareButton?.SetViewModel(shareVM);

            // The component will automatically apply the ShareData, DisplayStyle, and Context
            // properties that were set via binding in the XAML
        }
    }
}