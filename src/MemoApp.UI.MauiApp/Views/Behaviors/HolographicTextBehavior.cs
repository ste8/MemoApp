using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using System;
using System.Threading.Tasks;

namespace MemoApp.UI.MauiApp.Views
{
    /// <summary>
    /// Creates a holographic shimmering effect on text elements
    /// </summary>
    public class HolographicTextBehavior : Behavior<Label>
    {
        private Label _attachedLabel;
        private bool _isAnimating;

        protected override void OnAttachedTo(Label bindable)
        {
            _attachedLabel = bindable;
            bindable.Loaded += OnLabelLoaded;
            base.OnAttachedTo(bindable);
        }

        protected override void OnDetachingFrom(Label bindable)
        {
            _isAnimating = false;
            bindable.Loaded -= OnLabelLoaded;
            _attachedLabel = null;
            base.OnDetachingFrom(bindable);
        }

        private async void OnLabelLoaded(object sender, EventArgs e)
        {
            await StartHolographicAnimation();
        }

        private async Task StartHolographicAnimation()
        {
            _isAnimating = true;

            while (_isAnimating && _attachedLabel != null)
            {
                // Subtle color shift to simulate holographic effect
                await AnimateColorShift();

                if (!_isAnimating) break;

                // Small scale pulse
                await _attachedLabel.ScaleTo(1.02, 2000, Easing.SinInOut);

                if (!_isAnimating) break;

                await _attachedLabel.ScaleTo(1.0, 2000, Easing.SinInOut);
            }
        }

        private async Task AnimateColorShift()
        {
            if (_attachedLabel == null) return;

            var originalColor = _attachedLabel.TextColor;
            var colors = new[]
            {
                Color.FromRgb(0, 217, 255),    // Cyan
                Color.FromRgb(100, 255, 218),  // Mint
                Color.FromRgb(184, 224, 255),  // Light blue
                Color.FromRgb(255, 255, 255)   // White
            };

            foreach (var color in colors)
            {
                if (!_isAnimating) break;

                // Animate to new color
                var animation = new Animation(v =>
                {
                    if (_attachedLabel != null)
                    {
                        _attachedLabel.TextColor = Color.FromRgba(
                            color.Red + (originalColor.Red - color.Red) * (1 - v),
                            color.Green + (originalColor.Green - color.Green) * (1 - v),
                            color.Blue + (originalColor.Blue - color.Blue) * (1 - v),
                            color.Alpha + (originalColor.Alpha - color.Alpha) * (1 - v)
                        );
                    }
                }, 0, 1);

                var tcs = new TaskCompletionSource<bool>();
                animation.Commit(_attachedLabel, "ColorShift", 16, 1500, Easing.SinInOut,
                    (v, c) => tcs.SetResult(c));

                await tcs.Task;
                await Task.Delay(500);
            }

            // Return to original color
            if (_attachedLabel != null)
            {
                _attachedLabel.TextColor = originalColor;
            }
        }
    }
}