using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Controls.Shapes;
using System;

namespace MemoApp.UI.MauiApp.Views
{
    /// <summary>
    /// Creates a subtle neural network pattern background
    /// </summary>
    public class NeuralPatternView : ContentView
    {
        public NeuralPatternView()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            var grid = new Grid();

            // Create a simple geometric pattern that suggests neural connections
            for (int i = 0; i < 6; i++)
            {
                for (int j = 0; j < 4; j++)
                {
                    var dot = new Border
                    {
                        WidthRequest = 2,
                        HeightRequest = 2,
                        StrokeThickness = 0,
                        BackgroundColor = Color.FromRgba(0, 217, 255, 0.1),
                        StrokeShape = new Ellipse(),
                        HorizontalOptions = LayoutOptions.Start,
                        VerticalOptions = LayoutOptions.Start,
                        Margin = new Thickness(i * 40, j * 30, 0, 0)
                    };

                    grid.Children.Add(dot);
                }
            }

            // Add some connecting lines
            for (int i = 0; i < 3; i++)
            {
                var line = new Border
                {
                    HeightRequest = 1,
                    WidthRequest = 120,
                    StrokeThickness = 0,
                    BackgroundColor = Color.FromRgba(0, 217, 255, 0.05),
                    HorizontalOptions = LayoutOptions.Start,
                    VerticalOptions = LayoutOptions.Start,
                    Margin = new Thickness(20, i * 40 + 15, 0, 0)
                };

                grid.Children.Add(line);
            }

            Content = grid;
        }
    }
}