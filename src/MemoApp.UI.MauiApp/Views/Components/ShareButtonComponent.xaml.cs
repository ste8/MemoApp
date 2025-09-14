using MemoApp.Core.Models;
using MemoApp.UI.MauiApp.ViewModels.Components;

namespace MemoApp.UI.MauiApp.Views.Components;

public partial class ShareButtonComponent : ContentView
{
    public ShareButtonComponent()
    {
        InitializeComponent();
    }

    #region Bindable Properties

    public static readonly BindableProperty ShareDataProperty =
        BindableProperty.Create(nameof(ShareData), typeof(ShareableResult), typeof(ShareButtonComponent), null,
            propertyChanged: OnShareDataChanged);

    public static readonly BindableProperty DisplayStyleProperty =
        BindableProperty.Create(nameof(DisplayStyle), typeof(string), typeof(ShareButtonComponent), "Normal",
            propertyChanged: OnDisplayStyleChanged);

    public static readonly BindableProperty ContextProperty =
        BindableProperty.Create(nameof(Context), typeof(string), typeof(ShareButtonComponent), "default",
            propertyChanged: OnContextChanged);

    public static readonly BindableProperty IsEnabledProperty =
        BindableProperty.Create(nameof(IsEnabled), typeof(bool), typeof(ShareButtonComponent), true,
            propertyChanged: OnIsEnabledChanged);

    public ShareableResult? ShareData
    {
        get => (ShareableResult?)GetValue(ShareDataProperty);
        set => SetValue(ShareDataProperty, value);
    }

    /// <summary>
    /// Display style: "Compact", "Normal", or "Prominent"
    /// </summary>
    public string DisplayStyle
    {
        get => (string)GetValue(DisplayStyleProperty);
        set => SetValue(DisplayStyleProperty, value);
    }

    /// <summary>
    /// Context for styling and behavior: "verification", "celebration", "manual", etc.
    /// </summary>
    public string Context
    {
        get => (string)GetValue(ContextProperty);
        set => SetValue(ContextProperty, value);
    }

    public new bool IsEnabled
    {
        get => (bool)GetValue(IsEnabledProperty);
        set => SetValue(IsEnabledProperty, value);
    }

    #endregion

    #region Property Changed Handlers

    private static void OnShareDataChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is ShareButtonComponent component && component.BindingContext is ShareButtonViewModel viewModel)
        {
            viewModel.UpdateShareData(newValue as ShareableResult);
        }
    }

    private static void OnDisplayStyleChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is ShareButtonComponent component && component.BindingContext is ShareButtonViewModel viewModel)
        {
            viewModel.UpdateDisplayStyle(newValue as string ?? "Normal");
        }
    }

    private static void OnContextChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is ShareButtonComponent component && component.BindingContext is ShareButtonViewModel viewModel)
        {
            viewModel.UpdateContext(newValue as string ?? "default");
        }
    }

    private static void OnIsEnabledChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is ShareButtonComponent component && component.BindingContext is ShareButtonViewModel viewModel)
        {
            viewModel.UpdateIsEnabled((bool)newValue);
        }
    }

    #endregion
}