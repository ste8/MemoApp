using System.Collections.ObjectModel;
using MemoApp.Core.MajorSystem.Models;

namespace MemoApp.UI.MauiApp.Views.Components;

/// <summary>
/// Compact view component for displaying Major System digit-letter mappings.
/// Optimized for space-constrained contexts like onboarding flows.
/// Uses localized mapping service for consistency across the application.
/// </summary>
public partial class CompactMajorSystemMappingView : ContentView
{
    /// <summary>
    /// Bindable property for the mapping service
    /// </summary>
    public static readonly BindableProperty MappingServiceProperty =
        BindableProperty.Create(
            nameof(MappingService),
            typeof(IDigitLetterMappingService),
            typeof(CompactMajorSystemMappingView),
            propertyChanged: OnMappingServiceChanged);

    /// <summary>
    /// The mapping service used to retrieve localized digit-letter mappings
    /// </summary>
    public IDigitLetterMappingService? MappingService
    {
        get => (IDigitLetterMappingService?)GetValue(MappingServiceProperty);
        set => SetValue(MappingServiceProperty, value);
    }

    public CompactMajorSystemMappingView()
    {
        InitializeComponent();
        BindingContext = this;
    }

    /// <summary>
    /// Called when the mapping service property changes
    /// </summary>
    private static void OnMappingServiceChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is CompactMajorSystemMappingView view && newValue is IDigitLetterMappingService service)
        {
            view.LoadMappings(service);
        }
    }

    /// <summary>
    /// Loads mappings from the service and creates grid layout
    /// </summary>
    private void LoadMappings(IDigitLetterMappingService service)
    {
        MappingsGrid.Children.Clear();

        var mappings = service.GetMappings();

        // Create grid layout: Column 0 (0,1,2,3,4), Column 1 (5,6,7,8,9)
        for (int i = 0; i < mappings.Count; i++)
        {
            var mapping = mappings[i];
            var frame = CreateMappingFrame(mapping);

            // Position in grid: first 5 in column 0, rest in column 1
            int column = i < 5 ? 0 : 1;
            int row = i < 5 ? i : i - 5;

            Grid.SetColumn(frame, column);
            Grid.SetRow(frame, row);

            MappingsGrid.Children.Add(frame);
        }
    }

    /// <summary>
    /// Creates a frame for displaying a single mapping
    /// </summary>
    private Frame CreateMappingFrame(DigitLetterMapping mapping)
    {
        var frame = new Frame
        {
            Padding = new Thickness(12),
            BackgroundColor = Application.Current?.RequestedTheme == AppTheme.Light
                ? Color.FromRgb(245, 245, 245) // Light gray
                : Color.FromRgb(64, 64, 64), // Dark gray
            BorderColor = Application.Current?.RequestedTheme == AppTheme.Light
                ? Color.FromRgb(229, 229, 229) // Light border
                : Color.FromRgb(102, 102, 102), // Dark border
            CornerRadius = 8,
            HasShadow = false
        };

        var label = new Label
        {
            HorizontalOptions = LayoutOptions.Center,
            HorizontalTextAlignment = TextAlignment.Center,
            LineBreakMode = LineBreakMode.WordWrap
        };

        var formattedString = new FormattedString();

        // Digit number in bold with primary color
        formattedString.Spans.Add(new Span
        {
            Text = mapping.Digit.ToString(),
            FontAttributes = FontAttributes.Bold,
            FontSize = DeviceInfo.Idiom == DeviceIdiom.Phone ? 16 : DeviceInfo.Idiom == DeviceIdiom.Tablet ? 18 : 20,
            TextColor = GetPrimaryColor()
        });

        // Equals sign
        formattedString.Spans.Add(new Span
        {
            Text = " = ",
            FontAttributes = FontAttributes.Bold,
            FontSize = DeviceInfo.Idiom == DeviceIdiom.Phone ? 16 : DeviceInfo.Idiom == DeviceIdiom.Tablet ? 18 : 20
        });

        // Letters in normal weight
        formattedString.Spans.Add(new Span
        {
            Text = mapping.Letters,
            FontSize = DeviceInfo.Idiom == DeviceIdiom.Phone ? 14 : DeviceInfo.Idiom == DeviceIdiom.Tablet ? 16 : 18
        });

        label.FormattedText = formattedString;
        frame.Content = label;

        return frame;
    }

    /// <summary>
    /// Gets the primary color from app resources
    /// </summary>
    private Color GetPrimaryColor()
    {
        if (Application.Current?.Resources.TryGetValue("Primary", out var primaryResource) == true
            && primaryResource is Color primary)
        {
            return primary;
        }
        return Colors.Blue; // Fallback
    }

    /// <summary>
    /// Refresh mappings when the view appears (useful for culture changes)
    /// </summary>
    protected override void OnParentSet()
    {
        base.OnParentSet();

        if (Parent != null && MappingService != null)
        {
            LoadMappings(MappingService);
        }
    }
}