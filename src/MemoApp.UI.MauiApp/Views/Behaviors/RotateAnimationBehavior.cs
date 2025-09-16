using Microsoft.Maui.Controls;
using System;
using System.Threading.Tasks;

namespace MemoApp.UI.MauiApp.Views
{
    public enum RotationDirection
    {
        Clockwise,
        CounterClockwise
    }

    /// <summary>
    /// Creates a continuous rotation animation
    /// </summary>
    public class RotateAnimationBehavior : Behavior<View>
    {
        public static readonly BindableProperty DurationProperty = BindableProperty.Create(
            nameof(Duration), typeof(uint), typeof(RotateAnimationBehavior), (uint)5000);

        public static readonly BindableProperty DirectionProperty = BindableProperty.Create(
            nameof(Direction), typeof(RotationDirection), typeof(RotateAnimationBehavior), RotationDirection.Clockwise);

        public uint Duration
        {
            get => (uint)GetValue(DurationProperty);
            set => SetValue(DurationProperty, value);
        }

        public RotationDirection Direction
        {
            get => (RotationDirection)GetValue(DirectionProperty);
            set => SetValue(DirectionProperty, value);
        }

        private View _attachedView;
        private bool _isAnimating;

        protected override void OnAttachedTo(View bindable)
        {
            _attachedView = bindable;
            bindable.Loaded += OnViewLoaded;
            base.OnAttachedTo(bindable);
        }

        protected override void OnDetachingFrom(View bindable)
        {
            _isAnimating = false;
            bindable.Loaded -= OnViewLoaded;
            _attachedView = null;
            base.OnDetachingFrom(bindable);
        }

        private async void OnViewLoaded(object sender, EventArgs e)
        {
            await StartRotationAnimation();
        }

        private async Task StartRotationAnimation()
        {
            _isAnimating = true;
            var targetRotation = Direction == RotationDirection.Clockwise ? 360 : -360;

            while (_isAnimating && _attachedView != null)
            {
                _attachedView.Rotation = 0;
                await _attachedView.RotateTo(targetRotation, Duration, Easing.Linear);
            }
        }
    }
}