using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Controls.Shapes;
using System;
using System.Threading.Tasks;

namespace MemoApp.UI.MauiApp.Views
{
    /// <summary>
    /// Creates a ripple effect animation for touch interactions
    /// </summary>
    public class RippleEffectBehavior : Behavior<View>
    {
        private View _attachedView;

        protected override void OnAttachedTo(View bindable)
        {
            _attachedView = bindable;

            var tapGestureRecognizer = new TapGestureRecognizer();
            tapGestureRecognizer.Tapped += OnTapped;
            bindable.GestureRecognizers.Add(tapGestureRecognizer);

            base.OnAttachedTo(bindable);
        }

        protected override void OnDetachingFrom(View bindable)
        {
            _attachedView = null;
            base.OnDetachingFrom(bindable);
        }

        private async void OnTapped(object sender, EventArgs e)
        {
            if (_attachedView != null)
            {
                await CreateRippleEffect();
            }
        }

        private async Task CreateRippleEffect()
        {
            // Create ripple overlay
            var ripple = new Border
            {
                WidthRequest = 0,
                HeightRequest = 0,
                StrokeThickness = 0,
                BackgroundColor = Colors.White.WithAlpha(0.3f),
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center,
                StrokeShape = new RoundRectangle { CornerRadius = 50 }
            };

            // Add to parent if it's a layout
            if (_attachedView.Parent is Layout layout)
            {
                layout.Children.Add(ripple);
            }
            else if (_attachedView.Parent is ContentView contentView)
            {
                var grid = new Grid();
                contentView.Content = grid;
                grid.Children.Add(_attachedView);
                grid.Children.Add(ripple);
            }

            // Animate ripple expansion
            var maxSize = Math.Max(_attachedView.Width, _attachedView.Height) * 1.5;

            var expandTask = Task.WhenAll(
                ripple.WidthTo(maxSize, 300, Easing.CubicOut),
                ripple.HeightTo(maxSize, 300, Easing.CubicOut)
            );

            var fadeTask = ripple.FadeTo(0, 300, Easing.CubicOut);

            await Task.WhenAll(expandTask, fadeTask);

            // Remove ripple
            if (_attachedView?.Parent is Layout parentLayout)
            {
                parentLayout.Children.Remove(ripple);
            }
        }
    }

    // Extension methods for smoother animations
    public static class ViewExtensions
    {
        public static Task<bool> WidthTo(this View view, double width, uint duration, Easing easing)
        {
            var tcs = new TaskCompletionSource<bool>();
            var animation = new Animation(v => view.WidthRequest = v, view.WidthRequest, width);
            animation.Commit(view, "WidthAnimation", 16, duration, easing, (v, c) => tcs.SetResult(c));
            return tcs.Task;
        }

        public static Task<bool> HeightTo(this View view, double height, uint duration, Easing easing)
        {
            var tcs = new TaskCompletionSource<bool>();
            var animation = new Animation(v => view.HeightRequest = v, view.HeightRequest, height);
            animation.Commit(view, "HeightAnimation", 16, duration, easing, (v, c) => tcs.SetResult(c));
            return tcs.Task;
        }
    }
}