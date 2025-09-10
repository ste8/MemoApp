using System.ComponentModel;

namespace MemoApp.UI.MauiApp.Controls.Onboarding;

/// <summary>
/// Custom control for animated Major System conversion steps
/// </summary>
public partial class AnimatedConversionStep : ContentView
{
    #region Bindable Properties
    
    public static readonly BindableProperty DisplayTextProperty = 
        BindableProperty.Create(nameof(DisplayText), typeof(string), typeof(AnimatedConversionStep), string.Empty);
    
    public static readonly BindableProperty IsHighlightedProperty = 
        BindableProperty.Create(nameof(IsHighlighted), typeof(bool), typeof(AnimatedConversionStep), false,
            propertyChanged: OnHighlightChanged);
    
    public static readonly BindableProperty IsVisibleAnimatedProperty = 
        BindableProperty.Create(nameof(IsVisibleAnimated), typeof(bool), typeof(AnimatedConversionStep), false,
            propertyChanged: OnVisibilityChanged);
    
    public string DisplayText
    {
        get => (string)GetValue(DisplayTextProperty);
        set => SetValue(DisplayTextProperty, value);
    }
    
    public bool IsHighlighted
    {
        get => (bool)GetValue(IsHighlightedProperty);
        set => SetValue(IsHighlightedProperty, value);
    }
    
    public bool IsVisibleAnimated
    {
        get => (bool)GetValue(IsVisibleAnimatedProperty);
        set => SetValue(IsVisibleAnimatedProperty, value);
    }
    
    #endregion
    
    public AnimatedConversionStep()
    {
        InitializeComponent();
        
        // Set initial state
        this.Opacity = 0;
        this.Scale = 0.8;
        this.IsVisible = false;
    }
    
    #region Animation Methods
    
    /// <summary>
    /// Animates the step appearing with a smooth entrance effect
    /// </summary>
    public async Task AnimateIn(int delayMs = 0)
    {
        if (delayMs > 0)
            await Task.Delay(delayMs);
        
        this.IsVisible = true;
        
        // Animate entrance with scale and fade
        var fadeTask = this.FadeTo(1, 400, Easing.CubicOut);
        var scaleTask = this.ScaleTo(1, 400, Easing.CubicOut);
        
        await Task.WhenAll(fadeTask, scaleTask);
    }
    
    /// <summary>
    /// Animates the step disappearing
    /// </summary>
    public async Task AnimateOut()
    {
        var fadeTask = this.FadeTo(0, 300, Easing.CubicIn);
        var scaleTask = this.ScaleTo(0.8, 300, Easing.CubicIn);
        
        await Task.WhenAll(fadeTask, scaleTask);
        
        this.IsVisible = false;
    }
    
    /// <summary>
    /// Highlights the step with a pulsing effect
    /// </summary>
    public async Task AnimateHighlight()
    {
        if (!IsHighlighted) return;
        
        // Pulse effect
        await this.ScaleTo(1.1, 150, Easing.CubicOut);
        await this.ScaleTo(1.0, 150, Easing.CubicIn);
    }
    
    /// <summary>
    /// Removes highlight with smooth transition
    /// </summary>
    public async Task AnimateUnhighlight()
    {
        if (IsHighlighted) return;
        
        // Smooth color transition is handled by the Frame style changes
        await Task.CompletedTask;
    }
    
    #endregion
    
    #region Property Changed Handlers
    
    private static async void OnHighlightChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is AnimatedConversionStep control)
        {
            var isHighlighted = (bool)newValue;
            
            if (isHighlighted)
            {
                await control.AnimateHighlight();
            }
            else
            {
                await control.AnimateUnhighlight();
            }
        }
    }
    
    private static async void OnVisibilityChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is AnimatedConversionStep control)
        {
            var isVisible = (bool)newValue;
            
            if (isVisible)
            {
                await control.AnimateIn();
            }
            else
            {
                await control.AnimateOut();
            }
        }
    }
    
    #endregion
}