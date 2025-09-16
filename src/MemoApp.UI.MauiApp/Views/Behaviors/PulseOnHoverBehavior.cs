using Microsoft.Maui.Controls;
using System;
using System.Threading.Tasks;

namespace MemoApp.UI.MauiApp.Views
{
    /// <summary>
    /// Creates a pulsing effect when the user hovers or touches an element
    /// </summary>
    public class PulseOnHoverBehavior : Behavior<View>
    {
        private View _attachedView;
        private bool _isPressed;

        protected override void OnAttachedTo(View bindable)
        {
            _attachedView = bindable;

            // Add gesture recognizers
            var hoverGestureRecognizer = new PointerGestureRecognizer();
            hoverGestureRecognizer.PointerEntered += OnPointerEntered;
            hoverGestureRecognizer.PointerExited += OnPointerExited;
            bindable.GestureRecognizers.Add(hoverGestureRecognizer);

            var pressGestureRecognizer = new TapGestureRecognizer();
            pressGestureRecognizer.Tapped += OnTapped;
            bindable.GestureRecognizers.Add(pressGestureRecognizer);

            base.OnAttachedTo(bindable);
        }

        protected override void OnDetachingFrom(View bindable)
        {
            _attachedView = null;
            base.OnDetachingFrom(bindable);
        }

        private async void OnPointerEntered(object sender, PointerEventArgs e)
        {
            if (_attachedView != null && !_isPressed)
            {
                await _attachedView.ScaleTo(1.05, 150, Easing.CubicOut);
                await StartPulseAnimation();
            }
        }

        private async void OnPointerExited(object sender, PointerEventArgs e)
        {
            if (_attachedView != null && !_isPressed)
            {
                _attachedView.CancelAnimations();
                await _attachedView.ScaleTo(1.0, 150, Easing.CubicOut);
            }
        }

        private async void OnTapped(object sender, EventArgs e)
        {
            if (_attachedView != null)
            {
                _isPressed = true;
                _attachedView.CancelAnimations();

                // Quick press animation
                await _attachedView.ScaleTo(0.95, 50, Easing.CubicIn);
                await _attachedView.ScaleTo(1.0, 100, Easing.CubicOut);

                _isPressed = false;
            }
        }

        private async Task StartPulseAnimation()
        {
            while (_attachedView != null && _attachedView.IsVisible && !_isPressed)
            {
                await _attachedView.FadeTo(0.8, 800, Easing.SinInOut);
                await _attachedView.FadeTo(1.0, 800, Easing.SinInOut);
            }
        }
    }
}