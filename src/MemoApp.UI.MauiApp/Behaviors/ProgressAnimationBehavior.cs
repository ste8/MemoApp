using Microsoft.Maui.Controls;

namespace MemoApp.UI.MauiApp.Behaviors;

/// <summary>
/// Provides smooth animation for progress bar width changes
/// </summary>
public class ProgressAnimationBehavior : Behavior<View>
{
    private View? _associatedObject;
    private double _lastWidth = 0;

    protected override void OnAttachedTo(View bindable)
    {
        base.OnAttachedTo(bindable);
        _associatedObject = bindable;
        bindable.PropertyChanged += OnPropertyChanged;
    }

    protected override void OnDetachingFrom(View bindable)
    {
        bindable.PropertyChanged -= OnPropertyChanged;
        _associatedObject = null;
        base.OnDetachingFrom(bindable);
    }

    private void OnPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (_associatedObject == null || e.PropertyName != nameof(View.WidthRequest))
            return;

        var newWidth = _associatedObject.WidthRequest;
        if (Math.Abs(newWidth - _lastWidth) > 0.1 && newWidth > 0)
        {
            // Animate width change smoothly
            var animation = new Animation(v => _associatedObject.WidthRequest = v, _lastWidth, newWidth);
            animation.Commit(_associatedObject, "ProgressAnimation", 16, 250, Easing.CubicOut);
            _lastWidth = newWidth;
        }
    }
}