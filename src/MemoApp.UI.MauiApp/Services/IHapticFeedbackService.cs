using System;

namespace MemoApp.UI.MauiApp.Services;

/// <summary>
/// Interface for providing haptic feedback functionality across platforms
/// </summary>
public interface IHapticFeedbackService
{
    /// <summary>
    /// Provides a light haptic feedback for success or positive actions
    /// </summary>
    void PerformSuccessFeedback();
    
    /// <summary>
    /// Provides a medium haptic feedback for warnings or neutral actions
    /// </summary>
    void PerformWarningFeedback();
    
    /// <summary>
    /// Provides a strong haptic feedback for errors or negative actions
    /// </summary>
    void PerformErrorFeedback();
    
    /// <summary>
    /// Provides a light haptic feedback for UI selections
    /// </summary>
    void PerformSelectionFeedback();
    
    /// <summary>
    /// Provides a strong haptic feedback for impactful actions
    /// </summary>
    void PerformImpactFeedback();
    
    /// <summary>
    /// Checks if haptic feedback is supported on the current device
    /// </summary>
    bool IsSupported { get; }
}