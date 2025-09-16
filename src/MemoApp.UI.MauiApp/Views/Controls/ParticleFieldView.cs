using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MemoApp.UI.MauiApp.Views
{
    /// <summary>
    /// Creates floating memory particles that drift across the screen
    /// </summary>
    public class ParticleFieldView : ContentView
    {
        public static readonly BindableProperty ParticleCountProperty = BindableProperty.Create(
            nameof(ParticleCount), typeof(int), typeof(ParticleFieldView), 20);

        public int ParticleCount
        {
            get => (int)GetValue(ParticleCountProperty);
            set => SetValue(ParticleCountProperty, value);
        }

        private AbsoluteLayout _particleContainer;
        private Random _random = new Random();
        private bool _isAnimating;
        private List<View> _particles = new List<View>();

        public ParticleFieldView()
        {
            InitializeComponent();
            this.Loaded += OnLoaded;
        }

        private void InitializeComponent()
        {
            _particleContainer = new AbsoluteLayout
            {
                HorizontalOptions = LayoutOptions.FillAndExpand,
                VerticalOptions = LayoutOptions.FillAndExpand
            };

            Content = _particleContainer;
        }

        private void CreateParticles()
        {
            _particles.Clear();
            _particleContainer.Children.Clear();

            var particleShapes = new[] { "●", "◆", "▲", "■", "⬢" }; // Memory symbols
            var particleColors = new[]
            {
                Color.FromRgba(0, 217, 255, 0.6),    // Cyan
                Color.FromRgba(184, 224, 255, 0.5),  // Light blue
                Color.FromRgba(100, 255, 218, 0.4),  // Mint
                Color.FromRgba(255, 255, 255, 0.3)   // White
            };

            for (int i = 0; i < ParticleCount; i++)
            {
                var particle = new Label
                {
                    Text = particleShapes[_random.Next(particleShapes.Length)],
                    FontSize = _random.Next(8, 16),
                    TextColor = particleColors[_random.Next(particleColors.Length)],
                    Opacity = _random.NextDouble() * 0.7 + 0.3, // 0.3 to 1.0
                    Rotation = _random.Next(0, 360)
                };

                // Random starting position
                var x = _random.NextDouble();
                var y = _random.NextDouble();

                AbsoluteLayout.SetLayoutBounds(particle, new Rect(x, y, -1, -1));
                AbsoluteLayout.SetLayoutFlags(particle, Microsoft.Maui.Layouts.AbsoluteLayoutFlags.PositionProportional);

                _particles.Add(particle);
                _particleContainer.Children.Add(particle);
            }
        }

        private async void OnLoaded(object sender, EventArgs e)
        {
            CreateParticles();
            await StartParticleAnimation();
        }

        private async Task StartParticleAnimation()
        {
            _isAnimating = true;

            // Start individual particle animations
            var tasks = new List<Task>();
            foreach (var particle in _particles)
            {
                tasks.Add(AnimateParticle(particle));
            }

            await Task.WhenAll(tasks);
        }

        private async Task AnimateParticle(View particle)
        {
            while (_isAnimating && particle != null && IsVisible)
            {
                try
                {
                    // Get current position
                    var currentBounds = AbsoluteLayout.GetLayoutBounds(particle);

                    // Calculate new random position
                    var newX = currentBounds.X + (_random.NextDouble() - 0.5) * 0.3;
                    var newY = currentBounds.Y + (_random.NextDouble() - 0.5) * 0.3;

                    // Keep within bounds
                    newX = Math.Max(0, Math.Min(1, newX));
                    newY = Math.Max(0, Math.Min(1, newY));

                    // Animate to new position
                    AbsoluteLayout.SetLayoutBounds(particle, new Rect(newX, newY, -1, -1));

                    // Random fade and rotation
                    var fadeTask = particle.FadeTo(_random.NextDouble() * 0.7 + 0.3,
                        (uint)_random.Next(2000, 4000), Easing.SinInOut);

                    var rotateTask = particle.RotateTo(particle.Rotation + _random.Next(-30, 30),
                        (uint)_random.Next(3000, 6000), Easing.Linear);

                    await Task.WhenAll(fadeTask, rotateTask);

                    if (!_isAnimating) break;

                    // Random pause
                    await Task.Delay(_random.Next(1000, 3000));
                }
                catch (Exception)
                {
                    // Handle any animation errors gracefully
                    break;
                }
            }
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