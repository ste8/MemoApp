using System.Collections;
using MemoApp.UI.MauiApp.ViewModels;

namespace MemoApp.UI.MauiApp.Views;

public partial class BigNumberPage : ContentPage
{
    private bool _isScrollSynchronizing = false;
    private DateTime _lastScrollSync = DateTime.MinValue;
    private const int ScrollSyncThrottleMs = 16; // ~60fps throttling
    
    public BigNumberPage(BigNumberViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
        
        // Subscribe to auto-scroll events from ViewModel
        viewModel.ScrollToVerificationResults += OnScrollToVerificationResults;
        
        // Subscribe to focus request events from ViewModel
        viewModel.FocusUserInputRequested += OnFocusUserInputRequested;
        
        // Subscribe to navigation scroll requests from ViewModel
        viewModel.ScrollToPositionRequested += OnScrollToPositionRequested;

        // Subscribe to legend scroll requests from ViewModel
        viewModel.ScrollToLegendRequested += OnScrollToLegendRequested;
        
        // Subscribe to property changes to reset animation state when results are hidden
        viewModel.PropertyChanged += OnViewModelPropertyChanged;
    }
    
    private async void OnScrollToVerificationResults(object? sender, EventArgs e)
    {
        try
        {
            // Start entrance animation
            await AnimateResultsEntrance();
            
            // Auto-scroll to bring verification results into view (after animation starts)
            await MainScrollView.ScrollToAsync(VerificationResultsSection, ScrollToPosition.Start, true);
            
            // Add celebration animation for perfect scores (100%)
            if (sender is BigNumberViewModel viewModel && viewModel.AccuracyPercentage == 100.0)
            {
                await AnimatePerfectScoreCelebration();
            }
            
            // Add haptic feedback on mobile platforms
#if ANDROID || IOS
            if (DeviceInfo.Platform == DevicePlatform.Android || DeviceInfo.Platform == DevicePlatform.iOS)
            {
                try 
                {
                    HapticFeedback.Default.Perform(HapticFeedbackType.Click);
                }
                catch 
                {
                    // Haptic feedback not available, continue without it
                }
            }
#endif
        }
        catch (Exception ex)
        {
            // Log error but don't crash the app
            System.Diagnostics.Debug.WriteLine($"Auto-scroll error: {ex.Message}");
        }
    }
    
    private async Task AnimateResultsEntrance()
    {
        // Reset to initial animation state
        VerificationResultsSection.Opacity = 0;
        VerificationResultsSection.Scale = 0.9;
        VerificationResultsSection.TranslationY = -20;
        
        // Entrance animation sequence
        var fadeIn = VerificationResultsSection.FadeTo(1, 400, Easing.CubicOut);
        var scaleUp = VerificationResultsSection.ScaleTo(1.0, 400, Easing.CubicOut);
        var slideUp = VerificationResultsSection.TranslateTo(0, 0, 400, Easing.CubicOut);
        
        // Run animations simultaneously
        await Task.WhenAll(fadeIn, scaleUp, slideUp);
        
        // Add subtle pulse highlight for attention
        await AnimatePulseHighlight();
    }
    
    private async Task AnimatePulseHighlight()
    {
        // Brief scale pulse to draw attention
        await VerificationResultsSection.ScaleTo(1.02, 200, Easing.CubicOut);
        await VerificationResultsSection.ScaleTo(1.0, 200, Easing.CubicIn);
    }
    
    private async Task AnimatePerfectScoreCelebration()
    {
        // Enhanced celebration for 100% accuracy
        try
        {
            // Double bounce celebration
            await VerificationResultsSection.ScaleTo(1.08, 150, Easing.CubicOut);
            await VerificationResultsSection.ScaleTo(0.98, 150, Easing.CubicIn);
            await VerificationResultsSection.ScaleTo(1.05, 150, Easing.CubicOut);
            await VerificationResultsSection.ScaleTo(1.0, 150, Easing.CubicIn);
            
            // Enhanced haptic feedback for perfect score
#if ANDROID || IOS
            if (DeviceInfo.Platform == DevicePlatform.Android || DeviceInfo.Platform == DevicePlatform.iOS)
            {
                try 
                {
                    // Double haptic pulse for celebration
                    HapticFeedback.Default.Perform(HapticFeedbackType.Click);
                    await Task.Delay(100);
                    HapticFeedback.Default.Perform(HapticFeedbackType.Click);
                }
                catch 
                {
                    // Haptic feedback not available, continue without it
                }
            }
#endif
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Celebration animation error: {ex.Message}");
        }
    }
    
    private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        // Reset animation state when results become hidden
        if (e.PropertyName == nameof(BigNumberViewModel.ShowVerificationResults) && 
            sender is BigNumberViewModel viewModel && 
            !viewModel.ShowVerificationResults)
        {
            ResetResultsAnimationState();
        }
    }
    
    private void ResetResultsAnimationState()
    {
        // Reset to initial animation state for next time
        VerificationResultsSection.Opacity = 0;
        VerificationResultsSection.Scale = 0.9;
        VerificationResultsSection.TranslationY = -20;
    }
    
    private async void OnFocusUserInputRequested(object? sender, EventArgs e)
    {
        try
        {
            // Small delay to ensure the UI has updated and the input field is visible
            await Task.Delay(100);
            
            // Focus on the number input field
            if (NumberInput != null && NumberInput.IsVisible)
            {
                NumberInput.Focus();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Focus input error: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Gets the total item width including margins and spacing to match XAML layout
    /// This must match the calculation in BigNumberViewModel.GetTotalItemWidth()
    /// </summary>
    private double GetTotalItemWidth()
    {
        // Base width from XAML OnIdiom values
        double baseWidth;
        if (DeviceInfo.Idiom == DeviceIdiom.Phone)
            baseWidth = 40.0;
        else if (DeviceInfo.Idiom == DeviceIdiom.Tablet)
            baseWidth = 50.0;
        else if (DeviceInfo.Idiom == DeviceIdiom.Desktop)
            baseWidth = 60.0;
        else
            baseWidth = 40.0;
        
        // XAML: Margin="2" (2px on all sides) + Spacing="4"
        const double margin = 2.0 * 2; // Left + Right margins
        const double spacing = 4.0;    // Spacing between items
        
        return baseWidth + margin + spacing;
    }

    private async void OnScrollToPositionRequested(object? sender, int position)
    {
        try
        {
            // Temporarily disable synchronization to prevent interference with programmatic scrolling
            _isScrollSynchronizing = true;
            
            // Use unified width calculation to match ViewModel position tracking
            double totalItemWidth = GetTotalItemWidth();
            
            // Calculate target pixel position
            double targetScrollX = position * totalItemWidth;
            
            System.Diagnostics.Debug.WriteLine($"Navigation scroll: position={position}, targetScrollX={targetScrollX}");
            
            // Scroll both ScrollViews to the calculated pixel position
            var scrollTasks = new List<Task>();
            
            if (OriginalNumberScrollView != null)
            {
                scrollTasks.Add(OriginalNumberScrollView.ScrollToAsync(targetScrollX, 0, true));
            }
            
            if (UserAnswerScrollView != null)
            {
                scrollTasks.Add(UserAnswerScrollView.ScrollToAsync(targetScrollX, 0, true));
            }
            
            // Execute scrolls in parallel and wait for completion
            if (scrollTasks.Count > 0)
            {
                await Task.WhenAll(scrollTasks);
            }
            
            // Small delay to ensure scrolling is complete before re-enabling synchronization
            await Task.Delay(100);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Scroll to position error: {ex.Message}");
        }
        finally
        {
            _isScrollSynchronizing = false;
        }
    }

    private async void OnScrollToLegendRequested(object? sender, EventArgs e)
    {
        System.Diagnostics.Debug.WriteLine("OnScrollToLegendRequested: Event handler called");

        try
        {
            // Verify legend section exists and is visible
            System.Diagnostics.Debug.WriteLine($"OnScrollToLegendRequested: LegendSection null: {LegendSection == null}, IsVisible: {LegendSection?.IsVisible}");
            System.Diagnostics.Debug.WriteLine($"OnScrollToLegendRequested: MainScrollView null: {MainScrollView == null}");
            System.Diagnostics.Debug.WriteLine($"OnScrollToLegendRequested: LegendSection position - X:{LegendSection.X}, Y:{LegendSection.Y}, Width:{LegendSection.Width}, Height:{LegendSection.Height}");
            System.Diagnostics.Debug.WriteLine($"OnScrollToLegendRequested: MainScrollView ContentSize - Width:{MainScrollView.ContentSize.Width}, Height:{MainScrollView.ContentSize.Height}");

            if (LegendSection?.IsVisible != true)
            {
                System.Diagnostics.Debug.WriteLine("Legend section not visible for scrolling");
                return;
            }

            // Minimal delay for UI updates (reduced from 150ms to optimize total timing)
            System.Diagnostics.Debug.WriteLine("OnScrollToLegendRequested: Starting delay");
            await Task.Delay(50);

            // Smooth scroll to the legend section with optimal positioning
            System.Diagnostics.Debug.WriteLine("OnScrollToLegendRequested: About to call ScrollToAsync");
            try
            {
                // Add a timeout to prevent hanging
                var scrollTask = MainScrollView.ScrollToAsync(LegendSection, ScrollToPosition.MakeVisible, true);
                var timeoutTask = Task.Delay(2000); // 2 second timeout
                var completedTask = await Task.WhenAny(scrollTask, timeoutTask);

                if (completedTask == timeoutTask)
                {
                    System.Diagnostics.Debug.WriteLine("OnScrollToLegendRequested: ScrollToAsync timed out, trying alternative approaches");

                    // Try different ScrollToPosition options
                    try
                    {
                        await MainScrollView.ScrollToAsync(LegendSection, ScrollToPosition.Start, true);
                        System.Diagnostics.Debug.WriteLine("OnScrollToLegendRequested: ScrollToPosition.Start worked");
                    }
                    catch
                    {
                        try
                        {
                            // Last resort: scroll to Y position
                            await MainScrollView.ScrollToAsync(0, LegendSection.Y, true);
                            System.Diagnostics.Debug.WriteLine("OnScrollToLegendRequested: Y position scroll worked");
                        }
                        catch (Exception yEx)
                        {
                            System.Diagnostics.Debug.WriteLine($"OnScrollToLegendRequested: All scroll methods failed: {yEx.Message}");
                        }
                    }
                }
                else
                {
                    await scrollTask; // Ensure we await the actual task
                    System.Diagnostics.Debug.WriteLine("OnScrollToLegendRequested: ScrollToAsync completed successfully");
                }
            }
            catch (Exception scrollEx)
            {
                System.Diagnostics.Debug.WriteLine($"OnScrollToLegendRequested: ScrollToAsync error: {scrollEx.Message}");
                // Try alternative scroll method
                try
                {
                    await MainScrollView.ScrollToAsync(0, LegendSection.Y, true);
                    System.Diagnostics.Debug.WriteLine("OnScrollToLegendRequested: Alternative scroll method worked");
                }
                catch (Exception altEx)
                {
                    System.Diagnostics.Debug.WriteLine($"OnScrollToLegendRequested: Alternative scroll also failed: {altEx.Message}");
                }
            }

            // Add subtle highlight animation to draw attention
            System.Diagnostics.Debug.WriteLine("OnScrollToLegendRequested: About to call AnimateLegendHighlight");
            await AnimateLegendHighlight();
            System.Diagnostics.Debug.WriteLine("OnScrollToLegendRequested: AnimateLegendHighlight completed");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Legend scroll error: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"Legend scroll error stack trace: {ex.StackTrace}");
        }
    }

    private async Task AnimateLegendHighlight()
    {
        try
        {
            if (LegendSection == null) return;

            // Subtle pulse to indicate the expanded section
            await LegendSection.ScaleTo(1.02, 200, Easing.CubicOut);
            await LegendSection.ScaleTo(1.0, 200, Easing.CubicIn);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Legend highlight animation error: {ex.Message}");
        }
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        
        if (BindingContext is BigNumberViewModel viewModel)
        {
            // Set the container element reference for accurate size measurement
            viewModel.SetContainerElement(NumberDisplayFrame);
            
            await viewModel.InitializeAsync();
        }
    }
    
    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        
        // Unsubscribe from events to prevent memory leaks
        if (BindingContext is BigNumberViewModel viewModel)
        {
            viewModel.ScrollToVerificationResults -= OnScrollToVerificationResults;
            viewModel.FocusUserInputRequested -= OnFocusUserInputRequested;
            viewModel.ScrollToPositionRequested -= OnScrollToPositionRequested;
            viewModel.ScrollToLegendRequested -= OnScrollToLegendRequested;
            viewModel.PropertyChanged -= OnViewModelPropertyChanged;
        }
    }
    
    #region Scroll Synchronization
    
    /// <summary>
    /// Handles scroll events from the original number ScrollView to synchronize with user answer ScrollView
    /// </summary>
    private void OnOriginalNumberScrolled(object? sender, ScrolledEventArgs e)
    {
        System.Diagnostics.Debug.WriteLine($"OnOriginalNumberScrolled called: ScrollX={e.ScrollX}, Synchronizing={_isScrollSynchronizing}");
        
        if (_isScrollSynchronizing)
            return;
            
        // Synchronize with pixel-perfect accuracy
        SynchronizeScrollViewScroll(UserAnswerScrollView, e.ScrollX);
        
        // Update dynamic position indicator
        if (BindingContext is BigNumberViewModel viewModel)
        {
            viewModel.UpdateDynamicPosition(e.ScrollX);
        }
    }
    
    /// <summary>
    /// Handles scroll events from the user answer ScrollView to synchronize with original number ScrollView
    /// </summary>
    private void OnUserAnswerScrolled(object? sender, ScrolledEventArgs e)
    {
        System.Diagnostics.Debug.WriteLine($"OnUserAnswerScrolled called: ScrollX={e.ScrollX}, Synchronizing={_isScrollSynchronizing}");
        
        if (_isScrollSynchronizing)
            return;
            
        // Synchronize with pixel-perfect accuracy
        SynchronizeScrollViewScroll(OriginalNumberScrollView, e.ScrollX);
        
        // Update dynamic position indicator
        if (BindingContext is BigNumberViewModel viewModel)
        {
            viewModel.UpdateDynamicPosition(e.ScrollX);
        }
    }
    
    /// <summary>
    /// Synchronizes the target ScrollView to the specified horizontal offset with pixel-perfect accuracy
    /// </summary>
    /// <param name="targetScrollView">The ScrollView to synchronize</param>
    /// <param name="horizontalOffset">The target horizontal scroll offset</param>
    private void SynchronizeScrollViewScroll(ScrollView? targetScrollView, double horizontalOffset)
    {
        // Performance throttling for smooth 60fps scrolling
        var now = DateTime.UtcNow;
        if ((now - _lastScrollSync).TotalMilliseconds < ScrollSyncThrottleMs)
            return;
            
        _lastScrollSync = now;
        
        System.Diagnostics.Debug.WriteLine($"SynchronizeScrollViewScroll called: target={targetScrollView?.GetHashCode()}, offset={horizontalOffset}");
        
        if (targetScrollView == null || !targetScrollView.IsVisible)
        {
            System.Diagnostics.Debug.WriteLine($"Target ScrollView is null or not visible");
            return;
        }
            
        try
        {
            _isScrollSynchronizing = true;
            
            System.Diagnostics.Debug.WriteLine($"Scrolling to pixel position {horizontalOffset}");
            
            // Execute on main thread for consistent performance
            Device.BeginInvokeOnMainThread(async () => 
            {
                try
                {
                    // Direct pixel-level synchronization with no animation for smooth experience
                    await targetScrollView.ScrollToAsync(horizontalOffset, targetScrollView.ScrollY, false);
                }
                finally
                {
                    _isScrollSynchronizing = false;
                }
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"ScrollView synchronization error: {ex.Message}");
            _isScrollSynchronizing = false;
        }
    }
    
    #endregion
}