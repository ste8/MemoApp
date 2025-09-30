using System.Windows.Input;
using Microsoft.Maui.Controls;
using MemoApp.UI.MauiApp.ViewModels;

namespace MemoApp.UI.MauiApp.Views.Components;

public partial class GlobalDownloadProgressView : ContentView
{
    private PackCardViewModel? _currentPackCard;

    public static readonly BindableProperty CancelCommandProperty =
        BindableProperty.Create(
            nameof(CancelCommand),
            typeof(ICommand),
            typeof(GlobalDownloadProgressView));

    public static readonly BindableProperty ScrollToPackCommandProperty =
        BindableProperty.Create(
            nameof(ScrollToPackCommand),
            typeof(ICommand),
            typeof(GlobalDownloadProgressView));

    public ICommand? CancelCommand
    {
        get => (ICommand?)GetValue(CancelCommandProperty);
        set => SetValue(CancelCommandProperty, value);
    }

    public ICommand? ScrollToPackCommand
    {
        get => (ICommand?)GetValue(ScrollToPackCommandProperty);
        set => SetValue(ScrollToPackCommandProperty, value);
    }

    public GlobalDownloadProgressView()
    {
        InitializeComponent();

        // Handle BindingContext changes to control visibility
        BindingContextChanged += OnBindingContextChanged;

        // Start hidden
        IsVisible = false;
    }

    private void OnBindingContextChanged(object? sender, EventArgs e)
    {
        // Unsubscribe from old pack card
        if (_currentPackCard != null)
        {
            _currentPackCard.PropertyChanged -= PackCard_PropertyChanged;
            _currentPackCard = null;
        }

        if (BindingContext is PackCardViewModel packCard)
        {
            // Subscribe to IsDownloading changes
            _currentPackCard = packCard;
            packCard.PropertyChanged += PackCard_PropertyChanged;
            // Set initial visibility
            IsVisible = packCard.IsDownloading;
        }
        else
        {
            // Hide when BindingContext is null
            IsVisible = false;
        }
    }

    private void PackCard_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(PackCardViewModel.IsDownloading) && sender is PackCardViewModel packCard)
        {
            IsVisible = packCard.IsDownloading;
        }
    }

    protected override void OnHandlerChanged()
    {
        base.OnHandlerChanged();

        // Clean up subscriptions when handler is removed
        if (Handler == null && _currentPackCard != null)
        {
            _currentPackCard.PropertyChanged -= PackCard_PropertyChanged;
            _currentPackCard = null;
        }
    }
}