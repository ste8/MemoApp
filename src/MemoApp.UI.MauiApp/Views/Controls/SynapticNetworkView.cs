using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Controls.Shapes;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MemoApp.UI.MauiApp.Views
{
    /// <summary>
    /// Creates an animated synaptic network background effect
    /// </summary>
    public class SynapticNetworkView : ContentView
    {
        private Grid _networkGrid;
        private Random _random = new Random();
        private bool _isAnimating;
        private List<View> _nodes = new List<View>();

        public SynapticNetworkView()
        {
            InitializeComponent();
            this.Loaded += OnLoaded;
        }

        private void InitializeComponent()
        {
            _networkGrid = new Grid
            {
                HorizontalOptions = LayoutOptions.FillAndExpand,
                VerticalOptions = LayoutOptions.FillAndExpand
            };

            // Create network nodes
            CreateNetworkNodes();

            Content = _networkGrid;
        }

        private void CreateNetworkNodes()
        {
            const int nodeCount = 15;

            for (int i = 0; i < nodeCount; i++)
            {
                var node = new Border
                {
                    WidthRequest = 4,
                    HeightRequest = 4,
                    StrokeThickness = 0,
                    BackgroundColor = Color.FromRgba(0, 217, 255, 0.4),
                    StrokeShape = new Ellipse(),
                    HorizontalOptions = LayoutOptions.Start,
                    VerticalOptions = LayoutOptions.Start
                };

                // Position nodes randomly
                var x = _random.NextDouble();
                var y = _random.NextDouble();

                AbsoluteLayout.SetLayoutBounds(node, new Rect(x, y, -1, -1));
                AbsoluteLayout.SetLayoutFlags(node, Microsoft.Maui.Layouts.AbsoluteLayoutFlags.PositionProportional);

                _nodes.Add(node);
            }

            // Use AbsoluteLayout for precise positioning
            var absoluteLayout = new AbsoluteLayout();
            foreach (var node in _nodes)
            {
                absoluteLayout.Children.Add(node);
            }

            _networkGrid.Children.Add(absoluteLayout);

            // Add connection lines (simplified as borders)
            CreateConnectionLines();
        }

        private void CreateConnectionLines()
        {
            const int lineCount = 8;

            for (int i = 0; i < lineCount; i++)
            {
                var line = new Border
                {
                    HeightRequest = 1,
                    StrokeThickness = 0,
                    BackgroundColor = Color.FromRgba(0, 217, 255, 0.15),
                    HorizontalOptions = LayoutOptions.FillAndExpand,
                    VerticalOptions = LayoutOptions.Center,
                    Rotation = _random.Next(-45, 45),
                    Opacity = 0.3
                };

                _networkGrid.Children.Add(line);
            }
        }

        private async void OnLoaded(object sender, EventArgs e)
        {
            await StartNetworkAnimation();
        }

        private async Task StartNetworkAnimation()
        {
            _isAnimating = true;

            while (_isAnimating && IsVisible)
            {
                // Animate nodes
                var tasks = new List<Task>();

                foreach (var node in _nodes)
                {
                    if (node != null && _random.NextDouble() < 0.3) // 30% chance to pulse
                    {
                        tasks.Add(AnimateNodePulse(node));
                    }
                }

                await Task.WhenAll(tasks);

                if (!_isAnimating) break;

                await Task.Delay(_random.Next(2000, 4000)); // Random delay between pulses
            }
        }

        private async Task AnimateNodePulse(View node)
        {
            if (!_isAnimating || node == null) return;

            // Pulse effect
            await node.ScaleTo(2.0, 800, Easing.CubicOut);
            await node.ScaleTo(1.0, 1200, Easing.CubicIn);
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