using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Controls.Shapes;
using System;
using System.Threading.Tasks;

namespace MemoApp.UI.MauiApp.Views
{
    /// <summary>
    /// Creates an animated brain wave pulse indicator
    /// </summary>
    public class BrainWavePulse : ContentView
    {
        private Grid _container;
        private bool _isAnimating;

        public BrainWavePulse()
        {
            InitializeComponent();
            this.Loaded += OnLoaded;
        }

        private void InitializeComponent()
        {
            _container = new Grid
            {
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center
            };

            // Create wave bars
            for (int i = 0; i < 5; i++)
            {
                var bar = new Border
                {
                    WidthRequest = 3,
                    HeightRequest = 8,
                    StrokeThickness = 0,
                    BackgroundColor = Color.FromRgb(0, 217, 255),
                    StrokeShape = new RoundRectangle { CornerRadius = 1.5 },
                    Margin = new Thickness(2, 0),
                    Opacity = 0.6
                };

                _container.Add(bar, i, 0);
            }

            Content = _container;
        }

        private async void OnLoaded(object sender, EventArgs e)
        {
            await StartBrainWaveAnimation();
        }

        private async Task StartBrainWaveAnimation()
        {
            _isAnimating = true;

            while (_isAnimating && IsVisible)
            {
                // Animate each bar with different timing
                var tasks = new Task[_container.Children.Count];

                for (int i = 0; i < _container.Children.Count; i++)
                {
                    var bar = _container.Children[i] as Border;
                    if (bar != null)
                    {
                        tasks[i] = AnimateBar(bar, i * 100); // Stagger by 100ms
                    }
                }

                await Task.WhenAll(tasks);

                if (!_isAnimating) break;

                await Task.Delay(500); // Pause between cycles
            }
        }

        private async Task AnimateBar(Border bar, int delay)
        {
            await Task.Delay(delay);

            if (!_isAnimating || bar == null) return;

            // Scale up
            await bar.ScaleTo(bar.ScaleY * 2.5, 300, Easing.CubicOut);

            if (!_isAnimating) return;

            // Scale down
            await bar.ScaleTo(1.0, 600, Easing.CubicIn);
        }

        protected override void OnHandlerChanged()
        {
            base.OnHandlerChanged();
            if (Handler == null)
            {
                _isAnimating = false;
            }
        }
    }
}