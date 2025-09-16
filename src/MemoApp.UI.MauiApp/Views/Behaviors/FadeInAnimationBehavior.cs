using Microsoft.Maui.Controls;
using System;
using System.Threading.Tasks;

namespace MemoApp.UI.MauiApp.Views
{
    /// <summary>
    /// Creates a fade-in animation effect for elements
    /// </summary>
    public class FadeInAnimationBehavior : Behavior<View>
    {
        public static readonly BindableProperty DelayProperty = BindableProperty.Create(
            nameof(Delay), typeof(uint), typeof(FadeInAnimationBehavior), (uint)0);

        public static readonly BindableProperty DurationProperty = BindableProperty.Create(
            nameof(Duration), typeof(uint), typeof(FadeInAnimationBehavior), (uint)1000);

        public uint Delay
        {
            get => (uint)GetValue(DelayProperty);
            set => SetValue(DelayProperty, value);
        }

        public uint Duration
        {
            get => (uint)GetValue(DurationProperty);
            set => SetValue(DurationProperty, value);
        }

        private View _attachedView;

        protected override void OnAttachedTo(View bindable)
        {
            _attachedView = bindable;
            bindable.Opacity = 0; // Start invisible
            bindable.Loaded += OnViewLoaded;
            base.OnAttachedTo(bindable);
        }

        protected override void OnDetachingFrom(View bindable)
        {
            bindable.Loaded -= OnViewLoaded;
            _attachedView = null;
            base.OnDetachingFrom(bindable);
        }

        private async void OnViewLoaded(object sender, EventArgs e)
        {
            if (Delay > 0)
                await Task.Delay((int)Delay);

            await _attachedView.FadeTo(1.0, Duration, Easing.CubicOut);
        }
    }
}