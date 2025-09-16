using Microsoft.Maui.Controls;
using System;
using System.Threading.Tasks;

namespace MemoApp.UI.MauiApp.Views
{
    /// <summary>
    /// Creates a floating animation effect that makes elements gently bob up and down
    /// </summary>
    public class FloatingAnimationBehavior : Behavior<View>
    {
        public static readonly BindableProperty FloatHeightProperty = BindableProperty.Create(
            nameof(FloatHeight), typeof(double), typeof(FloatingAnimationBehavior), 10.0);

        public static readonly BindableProperty DurationProperty = BindableProperty.Create(
            nameof(Duration), typeof(uint), typeof(FloatingAnimationBehavior), (uint)3000);

        public static readonly BindableProperty DelayProperty = BindableProperty.Create(
            nameof(Delay), typeof(uint), typeof(FloatingAnimationBehavior), (uint)0);

        public double FloatHeight
        {
            get => (double)GetValue(FloatHeightProperty);
            set => SetValue(FloatHeightProperty, value);
        }

        public uint Duration
        {
            get => (uint)GetValue(DurationProperty);
            set => SetValue(DurationProperty, value);
        }

        public uint Delay
        {
            get => (uint)GetValue(DelayProperty);
            set => SetValue(DelayProperty, value);
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
            if (Delay > 0)
                await Task.Delay((int)Delay);

            await StartFloatingAnimation();
        }

        private async Task StartFloatingAnimation()
        {
            _isAnimating = true;

            while (_isAnimating && _attachedView != null)
            {
                // Float up
                await _attachedView.TranslateTo(
                    _attachedView.TranslationX,
                    -FloatHeight,
                    Duration / 2,
                    Easing.SinInOut);

                if (!_isAnimating) break;

                // Float down
                await _attachedView.TranslateTo(
                    _attachedView.TranslationX,
                    0,
                    Duration / 2,
                    Easing.SinInOut);
            }
        }
    }
}