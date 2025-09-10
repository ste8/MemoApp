using MemoApp.UI.MauiApp.Models.Onboarding;
using System.Collections.ObjectModel;

namespace MemoApp.UI.MauiApp.Controls.Onboarding;

/// <summary>
/// Reusable step card component for Major System example demonstration
/// </summary>
public partial class MajorSystemStepCard : ContentView
{
    #region Bindable Properties
    
    public static readonly BindableProperty StepNumberProperty = 
        BindableProperty.Create(nameof(StepNumber), typeof(string), typeof(MajorSystemStepCard), "1");
    
    public static readonly BindableProperty TitleProperty = 
        BindableProperty.Create(nameof(Title), typeof(string), typeof(MajorSystemStepCard), string.Empty);
    
    public static readonly BindableProperty DescriptionProperty = 
        BindableProperty.Create(nameof(Description), typeof(string), typeof(MajorSystemStepCard), string.Empty);
    
    public static readonly BindableProperty IsHighlightedProperty = 
        BindableProperty.Create(nameof(IsHighlighted), typeof(bool), typeof(MajorSystemStepCard), false,
            propertyChanged: OnHighlightChanged);
    
    public static readonly BindableProperty IsEmojiProperty = 
        BindableProperty.Create(nameof(IsEmoji), typeof(bool), typeof(MajorSystemStepCard), false);
    
    public static readonly BindableProperty ShowNumberProperty = 
        BindableProperty.Create(nameof(ShowNumber), typeof(bool), typeof(MajorSystemStepCard), false);
    
    public static readonly BindableProperty ShowConversionProperty = 
        BindableProperty.Create(nameof(ShowConversion), typeof(bool), typeof(MajorSystemStepCard), false);
    
    public static readonly BindableProperty ShowWordFormationProperty = 
        BindableProperty.Create(nameof(ShowWordFormation), typeof(bool), typeof(MajorSystemStepCard), false);
    
    public static readonly BindableProperty NumberTextProperty = 
        BindableProperty.Create(nameof(NumberText), typeof(string), typeof(MajorSystemStepCard), string.Empty);
    
    public static readonly BindableProperty FinalWordProperty = 
        BindableProperty.Create(nameof(FinalWord), typeof(string), typeof(MajorSystemStepCard), string.Empty);
    
    public static readonly BindableProperty EmojiTextProperty = 
        BindableProperty.Create(nameof(EmojiText), typeof(string), typeof(MajorSystemStepCard), string.Empty);
    
    public static readonly BindableProperty MemoryAssociationProperty = 
        BindableProperty.Create(nameof(MemoryAssociation), typeof(string), typeof(MajorSystemStepCard), string.Empty);
    
    public static readonly BindableProperty ConversionStepsProperty = 
        BindableProperty.Create(nameof(ConversionSteps), typeof(ObservableCollection<ConversionStep>), 
            typeof(MajorSystemStepCard), null, propertyChanged: OnConversionStepsChanged);
    
    public static readonly BindableProperty CardStyleProperty = 
        BindableProperty.Create(nameof(CardStyle), typeof(Style), typeof(MajorSystemStepCard), null,
            propertyChanged: OnCardStyleChanged);
    
    public static readonly BindableProperty TitleStyleProperty = 
        BindableProperty.Create(nameof(TitleStyle), typeof(Style), typeof(MajorSystemStepCard), null,
            propertyChanged: OnTitleStyleChanged);
    
    // Public Properties
    public string StepNumber
    {
        get => (string)GetValue(StepNumberProperty);
        set => SetValue(StepNumberProperty, value);
    }
    
    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }
    
    public string Description
    {
        get => (string)GetValue(DescriptionProperty);
        set => SetValue(DescriptionProperty, value);
    }
    
    public bool IsHighlighted
    {
        get => (bool)GetValue(IsHighlightedProperty);
        set => SetValue(IsHighlightedProperty, value);
    }
    
    public bool IsEmoji
    {
        get => (bool)GetValue(IsEmojiProperty);
        set => SetValue(IsEmojiProperty, value);
    }
    
    public bool ShowNumber
    {
        get => (bool)GetValue(ShowNumberProperty);
        set => SetValue(ShowNumberProperty, value);
    }
    
    public bool ShowConversion
    {
        get => (bool)GetValue(ShowConversionProperty);
        set => SetValue(ShowConversionProperty, value);
    }
    
    public bool ShowWordFormation
    {
        get => (bool)GetValue(ShowWordFormationProperty);
        set => SetValue(ShowWordFormationProperty, value);
    }
    
    public string NumberText
    {
        get => (string)GetValue(NumberTextProperty);
        set => SetValue(NumberTextProperty, value);
    }
    
    public string FinalWord
    {
        get => (string)GetValue(FinalWordProperty);
        set => SetValue(FinalWordProperty, value);
    }
    
    public string EmojiText
    {
        get => (string)GetValue(EmojiTextProperty);
        set => SetValue(EmojiTextProperty, value);
    }
    
    public string MemoryAssociation
    {
        get => (string)GetValue(MemoryAssociationProperty);
        set => SetValue(MemoryAssociationProperty, value);
    }
    
    public ObservableCollection<ConversionStep> ConversionSteps
    {
        get => (ObservableCollection<ConversionStep>)GetValue(ConversionStepsProperty);
        set => SetValue(ConversionStepsProperty, value);
    }
    
    public Style CardStyle
    {
        get => (Style)GetValue(CardStyleProperty);
        set => SetValue(CardStyleProperty, value);
    }
    
    public Style TitleStyle
    {
        get => (Style)GetValue(TitleStyleProperty);
        set => SetValue(TitleStyleProperty, value);
    }
    
    // Computed Properties
    public bool HasDescription => !string.IsNullOrEmpty(Description);
    
    #endregion
    
    public MajorSystemStepCard()
    {
        InitializeComponent();
        
        // Set initial state
        UpdateCardAppearance();
    }
    
    #region Public Methods
    
    /// <summary>
    /// Animates the card highlighting with a smooth pulse effect
    /// </summary>
    public async Task AnimateHighlight()
    {
        // Scale and glow effect
        var scaleTask = this.ScaleTo(1.05, 200, Easing.CubicOut);
        var pulseTask = StepNumberFrame.ScaleTo(1.2, 200, Easing.CubicOut);
        
        await Task.WhenAll(scaleTask, pulseTask);
        
        // Return to normal
        var returnScaleTask = this.ScaleTo(1.0, 200, Easing.CubicIn);
        var returnPulseTask = StepNumberFrame.ScaleTo(1.0, 200, Easing.CubicIn);
        
        await Task.WhenAll(returnScaleTask, returnPulseTask);
    }
    
    /// <summary>
    /// Animates content appearing with staggered entrance effects
    /// </summary>
    public async Task AnimateContentIn()
    {
        // Hide all content initially
        ContentArea.Opacity = 0;
        ContentArea.TranslationY = 20;
        
        // Animate content appearing
        var fadeTask = ContentArea.FadeTo(1, 400, Easing.CubicOut);
        var slideTask = ContentArea.TranslateTo(0, 0, 400, Easing.CubicOut);
        
        await Task.WhenAll(fadeTask, slideTask);
        
        // Animate individual elements if needed
        if (ShowConversion)
        {
            await AnimateConversionStepsIn();
        }
        
        if (ShowWordFormation)
        {
            await AnimateWordFormationIn();
        }
    }
    
    /// <summary>
    /// Sets up the card for a specific Major System example
    /// </summary>
    public void SetupForExample(MajorSystemExample example, int stepNumber)
    {
        if (example == null) return;
        
        StepNumber = stepNumber.ToString();
        
        switch (stepNumber)
        {
            case 1:
                SetupStep1(example);
                break;
            case 2:
                SetupStep2(example);
                break;
            case 3:
                SetupStep3(example);
                break;
            case 4:
                SetupStep4(example);
                break;
        }
    }
    
    #endregion
    
    #region Private Methods
    
    /// <summary>
    /// Updates card appearance based on current properties
    /// </summary>
    private void UpdateCardAppearance()
    {
        // Apply appropriate card style
        if (IsHighlighted)
        {
            MainFrame.Style = (Style)Resources["HighlightedCard"];
            TitleLabel.Style = (Style)Resources["HighlightedTitle"];
            DescriptionLabel.Style = (Style)Resources["HighlightedDescription"];
        }
        else if (IsEmoji)
        {
            MainFrame.Style = (Style)Resources["EmojiCard"];
            TitleLabel.Style = (Style)Resources["HighlightedTitle"];
            DescriptionLabel.Style = (Style)Resources["HighlightedDescription"];
        }
        else
        {
            MainFrame.Style = (Style)Resources["DefaultCard"];
            TitleLabel.Style = (Style)Resources["StepTitle"];
            DescriptionLabel.Style = (Style)Resources["StepDescription"];
        }
    }
    
    /// <summary>
    /// Sets up step 1 (show number)
    /// </summary>
    private void SetupStep1(MajorSystemExample example)
    {
        ShowNumber = true;
        NumberText = example.Number.ToString();
        ShowConversion = false;
        ShowWordFormation = false;
    }
    
    /// <summary>
    /// Sets up step 2 (show conversion)
    /// </summary>
    private void SetupStep2(MajorSystemExample example)
    {
        ShowNumber = false;
        ShowConversion = true;
        ShowWordFormation = false;
        ConversionSteps = new ObservableCollection<ConversionStep>(example.ConversionSteps);
    }
    
    /// <summary>
    /// Sets up step 3 (show word formation)
    /// </summary>
    private void SetupStep3(MajorSystemExample example)
    {
        ShowNumber = false;
        ShowConversion = false;
        ShowWordFormation = true;
        FinalWord = example.Word;
        PopulateWordFormation(example);
    }
    
    /// <summary>
    /// Sets up step 4 (show emoji)
    /// </summary>
    private void SetupStep4(MajorSystemExample example)
    {
        ShowNumber = false;
        ShowConversion = false;
        ShowWordFormation = false;
        EmojiText = example.Emoji;
        MemoryAssociation = $"{example.Number} = {example.Word} {example.Emoji}";
    }
    
    /// <summary>
    /// Populates conversion steps in the UI
    /// </summary>
    private void PopulateConversionSteps()
    {
        ConversionContainer.Clear();
        
        if (ConversionSteps == null) return;
        
        foreach (var step in ConversionSteps)
        {
            var stepFrame = new Frame
            {
                BackgroundColor = Colors.Transparent,
                BorderColor = Application.Current?.RequestedTheme == AppTheme.Light 
                    ? Color.FromArgb("#E5E7EB") 
                    : Color.FromArgb("#374151"),
                CornerRadius = 8,
                Padding = new Thickness(12, 8),
                HasShadow = false
            };
            
            var stepLabel = new Label
            {
                Text = $"{step.Digit} = {step.Letter}",
                FontSize = 18,
                FontAttributes = FontAttributes.Bold,
                TextColor = Application.Current?.RequestedTheme == AppTheme.Light 
                    ? Color.FromArgb("#1F2937") 
                    : Colors.White,
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center
            };
            
            stepFrame.Content = stepLabel;
            ConversionContainer.Add(stepFrame);
        }
    }
    
    /// <summary>
    /// Populates word formation letters
    /// </summary>
    private void PopulateWordFormation(MajorSystemExample example)
    {
        LetterCombination.Clear();
        
        if (example.ConversionSteps == null) return;
        
        foreach (var step in example.ConversionSteps)
        {
            var letterLabel = new Label
            {
                Text = step.Letter,
                FontSize = 28,
                FontAttributes = FontAttributes.Bold,
                TextColor = Colors.White,
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center
            };
            
            LetterCombination.Add(letterLabel);
            
            // Add plus sign between letters (except for the last one)
            if (step != example.ConversionSteps.Last())
            {
                var plusLabel = new Label
                {
                    Text = "+",
                    FontSize = 24,
                    FontAttributes = FontAttributes.Bold,
                    TextColor = Colors.White,
                    HorizontalOptions = LayoutOptions.Center,
                    VerticalOptions = LayoutOptions.Center
                };
                
                LetterCombination.Add(plusLabel);
            }
        }
    }
    
    /// <summary>
    /// Animates conversion steps appearing one by one
    /// </summary>
    private async Task AnimateConversionStepsIn()
    {
        foreach (var step in ConversionContainer.Children)
        {
            if (step is VisualElement element)
            {
                element.Opacity = 0;
                element.Scale = 0.8;
                
                var fadeTask = element.FadeTo(1, 300, Easing.CubicOut);
                var scaleTask = element.ScaleTo(1, 300, Easing.CubicOut);
                
                await Task.WhenAll(fadeTask, scaleTask);
                await Task.Delay(200);
            }
        }
    }
    
    /// <summary>
    /// Animates word formation sequence
    /// </summary>
    private async Task AnimateWordFormationIn()
    {
        // Animate letters first
        foreach (var letter in LetterCombination.Children)
        {
            if (letter is VisualElement element)
            {
                element.Opacity = 0;
                await element.FadeTo(1, 200, Easing.CubicOut);
                await Task.Delay(150);
            }
        }
        
        await Task.Delay(300);
        
        // Then animate the final word
        FinalWordLabel.Opacity = 0;
        FinalWordLabel.Scale = 0.8;
        
        var fadeTask = FinalWordLabel.FadeTo(1, 400, Easing.CubicOut);
        var scaleTask = FinalWordLabel.ScaleTo(1, 400, Easing.CubicOut);
        
        await Task.WhenAll(fadeTask, scaleTask);
    }
    
    #endregion
    
    #region Property Changed Handlers
    
    private static void OnHighlightChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is MajorSystemStepCard card)
        {
            card.UpdateCardAppearance();
        }
    }
    
    private static void OnConversionStepsChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is MajorSystemStepCard card && card.ShowConversion)
        {
            card.PopulateConversionSteps();
        }
    }
    
    private static void OnCardStyleChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is MajorSystemStepCard card && newValue is Style style)
        {
            card.MainFrame.Style = style;
        }
    }
    
    private static void OnTitleStyleChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is MajorSystemStepCard card && newValue is Style style)
        {
            card.TitleLabel.Style = style;
        }
    }
    
    #endregion
}