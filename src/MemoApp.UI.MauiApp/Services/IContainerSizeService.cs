namespace MemoApp.UI.MauiApp.Services;

/// <summary>
/// Service for getting actual container dimensions from UI elements
/// </summary>
public interface IContainerSizeService
{
    /// <summary>
    /// Gets the available width for text display in the number memorization container
    /// </summary>
    /// <returns>The available width in device-independent pixels</returns>
    double GetNumberDisplayContainerWidth();
    
    /// <summary>
    /// Sets the reference to the container element to measure
    /// </summary>
    /// <param name="containerElement">The container element (Frame or Label)</param>
    void SetContainerElement(VisualElement containerElement);
}