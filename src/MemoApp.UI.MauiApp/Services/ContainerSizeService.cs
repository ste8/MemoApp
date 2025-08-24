namespace MemoApp.UI.MauiApp.Services;

/// <summary>
/// Service for getting actual container dimensions from UI elements
/// </summary>
public class ContainerSizeService : IContainerSizeService
{
    private WeakReference<VisualElement>? _containerElementRef;

    public double GetNumberDisplayContainerWidth()
    {
        // Try to get the actual width from the container element
        if (_containerElementRef?.TryGetTarget(out var containerElement) == true)
        {
            // Get the actual allocated width, accounting for padding
            double actualWidth = containerElement.Width;
            
            if (actualWidth > 0)
            {
                // Account for padding if it's a Frame
                if (containerElement is Frame frame)
                {
                    var padding = frame.Padding;
                    actualWidth -= (padding.Left + padding.Right);
                }
                
                return Math.Max(actualWidth, 100); // Ensure minimum width
            }
        }

        // Fallback to platform-specific estimates if no container reference
        return GetPlatformEstimatedWidth();
    }

    public void SetContainerElement(VisualElement containerElement)
    {
        _containerElementRef = new WeakReference<VisualElement>(containerElement);
    }

    private double GetPlatformEstimatedWidth()
    {
        // Fallback estimates based on device idiom (better than hardcoded values)
        var idiom = DeviceInfo.Idiom;
        
        if (idiom == DeviceIdiom.Phone)
        {
            return Math.Min(DeviceDisplay.MainDisplayInfo.Width / DeviceDisplay.MainDisplayInfo.Density * 0.8, 300);
        }
        else if (idiom == DeviceIdiom.Tablet)
        {
            return Math.Min(DeviceDisplay.MainDisplayInfo.Width / DeviceDisplay.MainDisplayInfo.Density * 0.6, 450);
        }
        else
        {
            return Math.Min(DeviceDisplay.MainDisplayInfo.Width / DeviceDisplay.MainDisplayInfo.Density * 0.5, 600);
        }
    }
}