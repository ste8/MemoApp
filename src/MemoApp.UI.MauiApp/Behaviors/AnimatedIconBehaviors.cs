using Microsoft.Maui.Controls;
using MemoApp.UI.MauiApp.Helpers;

namespace MemoApp.UI.MauiApp.Behaviors;

/// <summary>
/// Behavior that automatically starts a loading spinner when attached
/// </summary>
public class LoadingSpinnerBehavior : Behavior<VisualElement>
{
    private CancellationTokenSource? _cancellationTokenSource;

    protected override void OnAttachedTo(VisualElement bindable)
    {
        bindable.PropertyChanged += OnElementPropertyChanged;

        if (bindable.IsVisible)
        {
            StartAnimation(bindable);
        }

        base.OnAttachedTo(bindable);
    }

    protected override void OnDetachingFrom(VisualElement bindable)
    {
        bindable.PropertyChanged -= OnElementPropertyChanged;
        StopAnimation();
        base.OnDetachingFrom(bindable);
    }

    private void OnElementPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(VisualElement.IsVisible) && sender is VisualElement element)
        {
            if (element.IsVisible)
            {
                StartAnimation(element);
            }
            else
            {
                StopAnimation();
            }
        }
    }

    private void StartAnimation(VisualElement element)
    {
        StopAnimation(); // Stop any existing animation
        _cancellationTokenSource = IconAnimationHelper.StartLoadingSpinner(element);
    }

    private void StopAnimation()
    {
        _cancellationTokenSource?.Cancel();
        _cancellationTokenSource = null;
    }
}

/// <summary>
/// Behavior that animates success icons with a bounce effect when they become visible
/// </summary>
public class SuccessAnimationBehavior : Behavior<VisualElement>
{
    private bool _hasAnimated = false;

    protected override void OnAttachedTo(VisualElement bindable)
    {
        bindable.PropertyChanged += OnElementPropertyChanged;

        // If already visible when attached, animate immediately
        if (bindable.IsVisible && !_hasAnimated)
        {
            _hasAnimated = true;
            _ = IconAnimationHelper.AnimateSuccessIcon(bindable);
        }

        base.OnAttachedTo(bindable);
    }

    protected override void OnDetachingFrom(VisualElement bindable)
    {
        bindable.PropertyChanged -= OnElementPropertyChanged;
        _hasAnimated = false;
        base.OnDetachingFrom(bindable);
    }

    private async void OnElementPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(VisualElement.IsVisible) && sender is VisualElement element && element.IsVisible && !_hasAnimated)
        {
            _hasAnimated = true;
            await IconAnimationHelper.AnimateSuccessIcon(element);
        }
        else if (e.PropertyName == nameof(VisualElement.IsVisible) && sender is VisualElement el && !el.IsVisible)
        {
            _hasAnimated = false; // Reset when hidden
        }
    }
}

/// <summary>
/// Behavior that animates error icons with a shake effect when they become visible
/// </summary>
public class ErrorAnimationBehavior : Behavior<VisualElement>
{
    protected override void OnAttachedTo(VisualElement bindable)
    {
        bindable.PropertyChanged += OnElementPropertyChanged;
        base.OnAttachedTo(bindable);
    }

    protected override void OnDetachingFrom(VisualElement bindable)
    {
        bindable.PropertyChanged -= OnElementPropertyChanged;
        base.OnDetachingFrom(bindable);
    }

    private async void OnElementPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(VisualElement.IsVisible) && sender is VisualElement element && element.IsVisible)
        {
            await IconAnimationHelper.AnimateErrorIcon(element);
        }
    }
}

/// <summary>
/// Behavior that provides tap feedback animation for buttons
/// </summary>
public class TapFeedbackBehavior : Behavior<Button>
{
    protected override void OnAttachedTo(Button bindable)
    {
        bindable.Pressed += OnButtonPressed;
        base.OnAttachedTo(bindable);
    }

    protected override void OnDetachingFrom(Button bindable)
    {
        bindable.Pressed -= OnButtonPressed;
        base.OnDetachingFrom(bindable);
    }

    private async void OnButtonPressed(object? sender, EventArgs e)
    {
        if (sender is Button button)
        {
            await IconAnimationHelper.AnimateButtonTap(button);
        }
    }
}

/// <summary>
/// Behavior that starts a pulse animation when attached
/// </summary>
public class PulseAnimationBehavior : Behavior<VisualElement>
{
    private CancellationTokenSource? _cancellationTokenSource;

    protected override void OnAttachedTo(VisualElement bindable)
    {
        bindable.PropertyChanged += OnElementPropertyChanged;

        if (bindable.IsVisible)
        {
            StartAnimation(bindable);
        }

        base.OnAttachedTo(bindable);
    }

    protected override void OnDetachingFrom(VisualElement bindable)
    {
        bindable.PropertyChanged -= OnElementPropertyChanged;
        StopAnimation();
        base.OnDetachingFrom(bindable);
    }

    private void OnElementPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(VisualElement.IsVisible) && sender is VisualElement element)
        {
            if (element.IsVisible)
            {
                StartAnimation(element);
            }
            else
            {
                StopAnimation();
            }
        }
    }

    private void StartAnimation(VisualElement element)
    {
        StopAnimation(); // Stop any existing animation
        _cancellationTokenSource = IconAnimationHelper.StartPulseAnimation(element);
    }

    private void StopAnimation()
    {
        _cancellationTokenSource?.Cancel();
        _cancellationTokenSource = null;
    }
}

/// <summary>
/// Behavior that fades in elements when they become visible
/// </summary>
public class FadeInBehavior : Behavior<VisualElement>
{
    protected override void OnAttachedTo(VisualElement bindable)
    {
        bindable.PropertyChanged += OnElementPropertyChanged;
        base.OnAttachedTo(bindable);
    }

    protected override void OnDetachingFrom(VisualElement bindable)
    {
        bindable.PropertyChanged -= OnElementPropertyChanged;
        base.OnDetachingFrom(bindable);
    }

    private async void OnElementPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(VisualElement.IsVisible) && sender is VisualElement element && element.IsVisible)
        {
            await IconAnimationHelper.AnimateFadeIn(element);
        }
    }
}