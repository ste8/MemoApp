namespace MemoApp.UI.MauiApp.Controls.Onboarding;

/// <summary>
/// Simple animated arrow component for indicating flow direction
/// </summary>
public partial class AnimatedArrow : ContentView
{
    #region Bindable Properties
    
    public static readonly BindableProperty DirectionProperty = 
        BindableProperty.Create(nameof(Direction), typeof(ArrowDirection), typeof(AnimatedArrow), ArrowDirection.Down,
            propertyChanged: OnDirectionChanged);
    
    public static readonly BindableProperty SizeProperty = 
        BindableProperty.Create(nameof(Size), typeof(double), typeof(AnimatedArrow), 32.0,
            propertyChanged: OnSizeChanged);
    
    public static readonly BindableProperty ColorProperty = 
        BindableProperty.Create(nameof(Color), typeof(Color), typeof(AnimatedArrow), Colors.Gray,
            propertyChanged: OnColorChanged);
    
    public ArrowDirection Direction
    {
        get => (ArrowDirection)GetValue(DirectionProperty);
        set => SetValue(DirectionProperty, value);
    }
    
    public double Size
    {
        get => (double)GetValue(SizeProperty);
        set => SetValue(SizeProperty, value);
    }
    
    public Color Color
    {
        get => (Color)GetValue(ColorProperty);
        set => SetValue(ColorProperty, value);
    }
    
    #endregion
    
    public AnimatedArrow()
    {
        InitializeComponent();
        UpdateAppearance();
        
        // Start subtle animation
        _ = StartAnimation();
    }
    
    #region Public Methods
    
    /// <summary>
    /// Starts the arrow animation sequence
    /// </summary>
    public async Task StartAnimation()
    {
        while (this.IsVisible)
        {
            try
            {
                await this.FadeTo(0.6, 1000, Easing.SinInOut);
                await this.FadeTo(1.0, 1000, Easing.SinInOut);
                
                // Small pause between cycles
                await Task.Delay(500);
            }
            catch (ObjectDisposedException)
            {
                // Control was disposed, exit animation loop
                break;
            }
        }
    }
    
    /// <summary>
    /// Stops the arrow animation
    /// </summary>
    public void StopAnimation()
    {
        this.AbortAnimation("ArrowAnimation");
        this.Opacity = 1.0;
    }
    
    #endregion
    
    #region Private Methods
    
    /// <summary>
    /// Updates arrow appearance based on current properties
    /// </summary>
    private void UpdateAppearance()
    {
        // Set arrow character based on direction
        ArrowLabel.Text = Direction switch
        {
            ArrowDirection.Up => "↑",
            ArrowDirection.Down => "↓",
            ArrowDirection.Left => "←",
            ArrowDirection.Right => "→",
            _ => "↓"
        };
        
        ArrowLabel.FontSize = Size;
        ArrowLabel.TextColor = Color;
    }
    
    #endregion
    
    #region Property Changed Handlers
    
    private static void OnDirectionChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is AnimatedArrow arrow)
        {
            arrow.UpdateAppearance();
        }
    }
    
    private static void OnSizeChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is AnimatedArrow arrow)
        {
            arrow.ArrowLabel.FontSize = (double)newValue;
        }
    }
    
    private static void OnColorChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is AnimatedArrow arrow)
        {
            arrow.ArrowLabel.TextColor = (Color)newValue;
        }
    }
    
    #endregion
}

/// <summary>
/// Arrow direction enumeration
/// </summary>
public enum ArrowDirection
{
    Up,
    Down,
    Left,
    Right
}