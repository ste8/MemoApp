using Microsoft.Maui.Controls;

namespace MemoApp.UI.MauiApp.Helpers;

/// <summary>
/// Helper class for applying subtle icon animations to enhance user experience
/// </summary>
public static class IconAnimationHelper
{
    /// <summary>
    /// Animation duration constants for consistent timing across the app
    /// </summary>
    public static class Durations
    {
        public static uint Fast => DeviceInfo.Platform == DevicePlatform.iOS || DeviceInfo.Platform == DevicePlatform.Android ? 200u : 150u;
        public static uint Normal => DeviceInfo.Platform == DevicePlatform.iOS || DeviceInfo.Platform == DevicePlatform.Android ? 300u : 200u;
        public static uint Slow => DeviceInfo.Platform == DevicePlatform.iOS || DeviceInfo.Platform == DevicePlatform.Android ? 500u : 350u;
    }

    /// <summary>
    /// Starts a continuous rotation animation for loading spinners
    /// </summary>
    /// <param name="element">The visual element to rotate</param>
    /// <returns>Cancellation token source to stop the animation</returns>
    public static CancellationTokenSource StartLoadingSpinner(VisualElement element)
    {
        var cancellationTokenSource = new CancellationTokenSource();

        Task.Run(async () =>
        {
            while (!cancellationTokenSource.Token.IsCancellationRequested)
            {
                try
                {
                    await element.RotateTo(360, Durations.Slow, Easing.Linear);
                    element.Rotation = 0; // Reset for continuous rotation
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }, cancellationTokenSource.Token);

        return cancellationTokenSource;
    }

    /// <summary>
    /// Animates a success icon with a bounce-in effect
    /// </summary>
    /// <param name="element">The success icon element</param>
    public static async Task AnimateSuccessIcon(VisualElement element)
    {
        element.Scale = 0;
        element.IsVisible = true;

        // Bounce in animation: scale to 1.2, then settle to 1.0
        await element.ScaleTo(1.2, Durations.Fast, Easing.BounceOut);
        await element.ScaleTo(1.0, Durations.Fast, Easing.CubicOut);
    }

    /// <summary>
    /// Animates an error icon with a shake effect
    /// </summary>
    /// <param name="element">The error icon element</param>
    public static async Task AnimateErrorIcon(VisualElement element)
    {
        element.IsVisible = true;

        // Quick shake animation
        const int shakeDistance = 5;
        await element.TranslateTo(shakeDistance, 0, 50, Easing.Linear);
        await element.TranslateTo(-shakeDistance, 0, 100, Easing.Linear);
        await element.TranslateTo(shakeDistance, 0, 100, Easing.Linear);
        await element.TranslateTo(0, 0, 50, Easing.Linear);
    }

    /// <summary>
    /// Provides tap feedback animation for buttons
    /// </summary>
    /// <param name="button">The button to animate</param>
    public static async Task AnimateButtonTap(Button button)
    {
        // Scale down slightly when pressed, then bounce back
        await button.ScaleTo(0.95, Durations.Fast / 2, Easing.CubicOut);
        await button.ScaleTo(1.0, Durations.Fast, Easing.SpringOut);
    }

    /// <summary>
    /// Starts a continuous pulse animation for attention-grabbing
    /// </summary>
    /// <param name="element">The element to pulse</param>
    /// <returns>Cancellation token source to stop the animation</returns>
    public static CancellationTokenSource StartPulseAnimation(VisualElement element)
    {
        var cancellationTokenSource = new CancellationTokenSource();

        Task.Run(async () =>
        {
            while (!cancellationTokenSource.Token.IsCancellationRequested)
            {
                try
                {
                    await element.FadeTo(0.3, Durations.Normal, Easing.CubicInOut);
                    await element.FadeTo(1.0, Durations.Normal, Easing.CubicInOut);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }, cancellationTokenSource.Token);

        return cancellationTokenSource;
    }

    /// <summary>
    /// Animates an element with a smooth fade-in effect
    /// </summary>
    /// <param name="element">The element to fade in</param>
    public static async Task AnimateFadeIn(VisualElement element)
    {
        element.Opacity = 0;
        element.IsVisible = true;
        await element.FadeTo(1.0, Durations.Normal, Easing.CubicOut);
    }

    /// <summary>
    /// Animates a download progress with icon state changes
    /// </summary>
    /// <param name="downloadIcon">The download icon label</param>
    /// <param name="progressValue">Progress from 0 to 1</param>
    public static async Task AnimateDownloadProgress(Label downloadIcon, double progressValue)
    {
        if (progressValue <= 0)
        {
            // Reset to download icon
            downloadIcon.Text = "\uf019"; // FontAwesome Download icon
            downloadIcon.Rotation = 0;
        }
        else if (progressValue < 1.0)
        {
            // Show spinner and rotate during download
            downloadIcon.Text = "\uf110"; // FontAwesome Spinner icon
            await downloadIcon.RotateTo(downloadIcon.Rotation + 360, Durations.Slow, Easing.Linear);
        }
        else
        {
            // Success - show check icon with animation
            downloadIcon.Text = "\uf058"; // FontAwesome CircleCheck icon
            downloadIcon.Rotation = 0;
            await AnimateSuccessIcon(downloadIcon);
        }
    }

    /// <summary>
    /// Stops all animations on an element
    /// </summary>
    /// <param name="element">The element to stop animations on</param>
    public static void StopAllAnimations(VisualElement element)
    {
        element.CancelAnimations();

        // Reset transforms
        element.Rotation = 0;
        element.Scale = 1.0;
        element.TranslationX = 0;
        element.TranslationY = 0;
        element.Opacity = 1.0;
    }
}