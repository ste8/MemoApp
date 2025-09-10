namespace MemoApp.UI.MauiApp.Controls.Onboarding;

/// <summary>
/// Responsive layout component that adapts to different screen sizes and orientations
/// </summary>
public partial class ResponsiveExampleLayout : ContentView
{
    #region Constants
    private const double PhoneBreakpoint = 600;
    private const double TabletBreakpoint = 1024;
    #endregion
    
    #region Bindable Properties
    
    public static readonly BindableProperty CurrentExampleProperty = 
        BindableProperty.Create(nameof(CurrentExample), typeof(object), typeof(ResponsiveExampleLayout), null,
            propertyChanged: OnCurrentExampleChanged);
    
    public static readonly BindableProperty AlternativeExampleProperty = 
        BindableProperty.Create(nameof(AlternativeExample), typeof(object), typeof(ResponsiveExampleLayout), null);
    
    public static readonly BindableProperty AnimationStepProperty = 
        BindableProperty.Create(nameof(AnimationStep), typeof(int), typeof(ResponsiveExampleLayout), 0,
            propertyChanged: OnAnimationStepChanged);
    
    public object CurrentExample
    {
        get => GetValue(CurrentExampleProperty);
        set => SetValue(CurrentExampleProperty, value);
    }
    
    public object AlternativeExample
    {
        get => GetValue(AlternativeExampleProperty);
        set => SetValue(AlternativeExampleProperty, value);
    }
    
    public int AnimationStep
    {
        get => (int)GetValue(AnimationStepProperty);
        set => SetValue(AnimationStepProperty, value);
    }
    
    #endregion
    
    #region Private Fields
    private LayoutMode _currentLayoutMode = LayoutMode.Phone;
    private Size _lastKnownSize = Size.Zero;
    #endregion
    
    public ResponsiveExampleLayout()
    {
        InitializeComponent();
        
        // Set up responsive behavior
        this.SizeChanged += OnSizeChanged;
        
        // Initialize with phone layout
        UpdateLayoutVisibility();
    }
    
    #region Layout Management
    
    /// <summary>
    /// Determines which layout to use based on screen size
    /// </summary>
    private LayoutMode DetermineLayoutMode(Size size)
    {
        var width = size.Width;
        var height = size.Height;
        
        // Use the larger dimension for layout decisions
        var effectiveWidth = Math.Max(width, height);
        
        if (effectiveWidth >= TabletBreakpoint)
        {
            // Desktop layout for very large screens, tablet for large screens
            return effectiveWidth >= 1400 ? LayoutMode.Desktop : LayoutMode.Tablet;
        }
        
        return effectiveWidth >= PhoneBreakpoint ? LayoutMode.Tablet : LayoutMode.Phone;
    }
    
    /// <summary>
    /// Updates layout visibility based on current mode
    /// </summary>
    private void UpdateLayoutVisibility()
    {
        // Hide all layouts first
        PhoneLayout.IsVisible = false;
        TabletLayout.IsVisible = false;
        DesktopLayout.IsVisible = false;
        
        // Show the appropriate layout
        switch (_currentLayoutMode)
        {
            case LayoutMode.Phone:
                PhoneLayout.IsVisible = true;
                break;
            case LayoutMode.Tablet:
                TabletLayout.IsVisible = true;
                break;
            case LayoutMode.Desktop:
                DesktopLayout.IsVisible = true;
                break;
        }
    }
    
    /// <summary>
    /// Handles responsive layout transitions with smooth animations
    /// </summary>
    private async Task TransitionToLayout(LayoutMode newMode)
    {
        if (_currentLayoutMode == newMode) return;
        
        var previousMode = _currentLayoutMode;
        _currentLayoutMode = newMode;
        
        // Fade out current layout
        await FadeOutCurrentLayout(previousMode);
        
        // Update visibility
        UpdateLayoutVisibility();
        
        // Fade in new layout
        await FadeInCurrentLayout();
    }
    
    /// <summary>
    /// Fades out the currently visible layout
    /// </summary>
    private async Task FadeOutCurrentLayout(LayoutMode mode)
    {
        var layoutToHide = mode switch
        {
            LayoutMode.Phone => PhoneLayout,
            LayoutMode.Tablet => TabletLayout,
            LayoutMode.Desktop => DesktopLayout,
            _ => PhoneLayout
        };
        
        if (layoutToHide.IsVisible)
        {
            await layoutToHide.FadeTo(0, 200, Easing.CubicIn);
        }
    }
    
    /// <summary>
    /// Fades in the new layout
    /// </summary>
    private async Task FadeInCurrentLayout()
    {
        var layoutToShow = _currentLayoutMode switch
        {
            LayoutMode.Phone => PhoneLayout,
            LayoutMode.Tablet => TabletLayout,
            LayoutMode.Desktop => DesktopLayout,
            _ => PhoneLayout
        };
        
        layoutToShow.Opacity = 0;
        await layoutToShow.FadeTo(1, 300, Easing.CubicOut);
    }
    
    #endregion
    
    #region Animation Management
    
    /// <summary>
    /// Handles animation step changes across all layouts
    /// </summary>
    private async Task HandleAnimationStepChange(int step)
    {
        switch (_currentLayoutMode)
        {
            case LayoutMode.Phone:
                await AnimatePhoneStep(step);
                break;
            case LayoutMode.Tablet:
                await AnimateTabletStep(step);
                break;
            case LayoutMode.Desktop:
                await AnimateDesktopStep(step);
                break;
        }
    }
    
    /// <summary>
    /// Handles phone layout animations
    /// </summary>
    private async Task AnimatePhoneStep(int step)
    {
        switch (step)
        {
            case 1:
                await PhoneStep1.AnimateHighlight();
                break;
            case 2:
                await PhoneStep2.AnimateHighlight();
                break;
            case 3:
                await PhoneStep3.AnimateHighlight();
                break;
            case 4:
                await PhoneStep4.AnimateHighlight();
                break;
        }
    }
    
    /// <summary>
    /// Handles tablet layout animations
    /// </summary>
    private async Task AnimateTabletStep(int step)
    {
        switch (step)
        {
            case 1:
                await TabletStep1.AnimateHighlight();
                break;
            case 2:
                await TabletStep2.AnimateHighlight();
                break;
            case 3:
                await TabletStep3.AnimateHighlight();
                break;
            case 4:
                await TabletStep4.AnimateHighlight();
                break;
        }
    }
    
    /// <summary>
    /// Handles desktop layout animations
    /// </summary>
    private async Task AnimateDesktopStep(int step)
    {
        // Desktop shows both current and alternative examples
        var currentTask = step switch
        {
            1 => DesktopCurrentStep1.AnimateHighlight(),
            2 => DesktopCurrentStep2.AnimateHighlight(),
            _ => Task.CompletedTask
        };
        
        var altTask = step switch
        {
            1 => DesktopAltStep1.AnimateHighlight(),
            2 => DesktopAltStep2.AnimateHighlight(),
            _ => Task.CompletedTask
        };
        
        await Task.WhenAll(currentTask, altTask);
    }
    
    #endregion
    
    #region Event Handlers
    
    private async void OnSizeChanged(object? sender, EventArgs e)
    {
        var newSize = this.Bounds.Size;
        
        // Avoid unnecessary layout changes for minor size differences
        if (Math.Abs(newSize.Width - _lastKnownSize.Width) < 50 && 
            Math.Abs(newSize.Height - _lastKnownSize.Height) < 50)
            return;
        
        _lastKnownSize = newSize;
        
        var newMode = DetermineLayoutMode(newSize);
        await TransitionToLayout(newMode);
    }
    
    private static void OnCurrentExampleChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is ResponsiveExampleLayout layout)
        {
            // Update content based on new example
            layout.UpdateExampleContent();
        }
    }
    
    private static async void OnAnimationStepChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is ResponsiveExampleLayout layout && newValue is int step)
        {
            await layout.HandleAnimationStepChange(step);
        }
    }
    
    #endregion
    
    #region Content Management
    
    /// <summary>
    /// Updates content across all layouts when example changes
    /// </summary>
    private void UpdateExampleContent()
    {
        // This would be implemented to update step cards with new example data
        // Implementation depends on the structure of the example data
    }
    
    #endregion
    
    #region Accessibility
    
    /// <summary>
    /// Sets up accessibility features for the current layout
    /// </summary>
    private void SetupAccessibility()
    {
        // Set semantic properties for screen readers
        this.SetSemanticFocus();
        
        // Configure accessibility descriptions based on layout mode
        var description = _currentLayoutMode switch
        {
            LayoutMode.Phone => "Mobile view of Major System example demonstration",
            LayoutMode.Tablet => "Tablet view with two-column layout of Major System example",
            LayoutMode.Desktop => "Desktop view comparing multiple Major System examples",
            _ => "Major System example demonstration"
        };
        
        SemanticProperties.SetDescription(this, description);
    }
    
    #endregion
}

/// <summary>
/// Enumeration for different layout modes
/// </summary>
public enum LayoutMode
{
    Phone,
    Tablet,
    Desktop
}

/// <summary>
/// Extension methods for step card animations
/// </summary>
public static class StepCardExtensions
{
    public static async Task AnimateHighlight(this VisualElement element)
    {
        if (element == null) return;
        
        // Pulse effect
        await element.ScaleTo(1.05, 200, Easing.CubicOut);
        await element.ScaleTo(1.0, 200, Easing.CubicIn);
    }
}