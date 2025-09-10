using Microsoft.Maui.Controls;

namespace MemoApp.UI.MauiApp.Controls;

public partial class UnifiedPackSelector : ContentView
{
    public static readonly BindableProperty ModeProperty = BindableProperty.Create(
        nameof(Mode),
        typeof(PackSelectorMode),
        typeof(UnifiedPackSelector),
        PackSelectorMode.Management,
        propertyChanged: OnModeChanged);

    public static readonly BindableProperty ShowFiltersProperty = BindableProperty.Create(
        nameof(ShowFilters),
        typeof(bool),
        typeof(UnifiedPackSelector),
        false);

    public static readonly BindableProperty AllowMultipleSelectionProperty = BindableProperty.Create(
        nameof(AllowMultipleSelection),
        typeof(bool),
        typeof(UnifiedPackSelector),
        false);

    public PackSelectorMode Mode
    {
        get => (PackSelectorMode)GetValue(ModeProperty);
        set => SetValue(ModeProperty, value);
    }

    public bool ShowFilters
    {
        get => (bool)GetValue(ShowFiltersProperty);
        set => SetValue(ShowFiltersProperty, value);
    }

    public bool AllowMultipleSelection
    {
        get => (bool)GetValue(AllowMultipleSelectionProperty);
        set => SetValue(AllowMultipleSelectionProperty, value);
    }

    public UnifiedPackSelector()
    {
        InitializeComponent();
    }

    private static void OnModeChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is UnifiedPackSelector selector && newValue is PackSelectorMode mode)
        {
            // Update UI based on mode
            selector.UpdateForMode(mode);
        }
    }

    private void UpdateForMode(PackSelectorMode mode)
    {
        // This will be handled by the ViewModel through binding
        // The mode determines:
        // - Selection behavior (single vs none)
        // - Action button visibility
        // - Header text
        // - Skip option visibility
    }
}

public enum PackSelectorMode
{
    Onboarding,  // Used during initial setup
    Management   // Used in the main app for pack management
}