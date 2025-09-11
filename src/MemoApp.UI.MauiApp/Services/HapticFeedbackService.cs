#if ANDROID || IOS
using Microsoft.Maui.Devices;
#endif

namespace MemoApp.UI.MauiApp.Services;

/// <summary>
/// Implementation of haptic feedback service using MAUI platform APIs
/// </summary>
public class HapticFeedbackService : IHapticFeedbackService
{
    public bool IsSupported => DeviceInfo.Platform == DevicePlatform.iOS || DeviceInfo.Platform == DevicePlatform.Android;

    public void PerformSuccessFeedback()
    {
        if (!IsSupported) return;
        
        try
        {
#if ANDROID || IOS
            // Use Vibration API for haptic feedback
            var duration = TimeSpan.FromMilliseconds(100);
            Vibration.Default.Vibrate(duration);
#endif
        }
        catch (Exception)
        {
            // Silently handle any haptic feedback errors
            // to prevent disrupting the user experience
        }
    }

    public void PerformWarningFeedback()
    {
        if (!IsSupported) return;
        
        try
        {
#if ANDROID || IOS
            var duration = TimeSpan.FromMilliseconds(150);
            Vibration.Default.Vibrate(duration);
#endif
        }
        catch (Exception)
        {
            // Silently handle errors
        }
    }

    public void PerformErrorFeedback()
    {
        if (!IsSupported) return;
        
        try
        {
#if ANDROID || IOS
            var duration = TimeSpan.FromMilliseconds(200);
            Vibration.Default.Vibrate(duration);
#endif
        }
        catch (Exception)
        {
            // Silently handle errors
        }
    }

    public void PerformSelectionFeedback()
    {
        if (!IsSupported) return;
        
        try
        {
#if ANDROID || IOS
            var duration = TimeSpan.FromMilliseconds(50);
            Vibration.Default.Vibrate(duration);
#endif
        }
        catch (Exception)
        {
            // Silently handle errors
        }
    }

    public void PerformImpactFeedback()
    {
        if (!IsSupported) return;
        
        try
        {
#if ANDROID || IOS
            var duration = TimeSpan.FromMilliseconds(300);
            Vibration.Default.Vibrate(duration);
#endif
        }
        catch (Exception)
        {
            // Silently handle errors
        }
    }
}