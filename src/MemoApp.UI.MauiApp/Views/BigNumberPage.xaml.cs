using System.Collections;
using MemoApp.UI.MauiApp.ViewModels;

namespace MemoApp.UI.MauiApp.Views;

public partial class BigNumberPage : ContentPage
{
    private bool _isScrollSynchronizing = false;
    
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
    
    private async void OnScrollToPositionRequested(object? sender, int position)
    {
        try
        {
            // Temporarily disable synchronization to prevent interference with programmatic scrolling
            _isScrollSynchronizing = true;
            
            // Scroll both CollectionViews to the specified position
            // CollectionView uses ScrollTo method with item index, not position
            if (OriginalNumberCollectionView?.ItemsSource is IList originalList && position < originalList.Count)
            {
                var originalItem = originalList[position];
                OriginalNumberCollectionView.ScrollTo(originalItem, position: ScrollToPosition.Start, animate: true);
            }
            
            if (UserAnswerCollectionView?.ItemsSource is IList userList && position < userList.Count)
            {
                var userItem = userList[position];
                UserAnswerCollectionView.ScrollTo(userItem, position: ScrollToPosition.Start, animate: true);
            }
            
            // Add small delay to ensure smooth scrolling and re-enable synchronization
            await Task.Delay(200); // Slightly longer delay to allow animation to complete
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
            viewModel.PropertyChanged -= OnViewModelPropertyChanged;
        }
    }
    
    #region Scroll Synchronization
    
    /// <summary>
    /// Handles scroll events from the original number CollectionView to synchronize with user answer CollectionView
    /// </summary>
    private void OnOriginalNumberScrolled(object? sender, ItemsViewScrolledEventArgs e)
    {
        System.Diagnostics.Debug.WriteLine($"OnOriginalNumberScrolled called: HorizontalOffset={e.HorizontalOffset}, Synchronizing={_isScrollSynchronizing}");
        
        if (_isScrollSynchronizing)
            return;
            
        try
        {
            _isScrollSynchronizing = true;
            
            // Synchronize the user answer CollectionView to match the original number scroll position
            SynchronizeCollectionViewScroll(UserAnswerCollectionView, e.HorizontalOffset);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Original number scroll synchronization error: {ex.Message}");
        }
        finally
        {
            _isScrollSynchronizing = false;
        }
    }
    
    /// <summary>
    /// Handles scroll events from the user answer CollectionView to synchronize with original number CollectionView
    /// </summary>
    private void OnUserAnswerScrolled(object? sender, ItemsViewScrolledEventArgs e)
    {
        System.Diagnostics.Debug.WriteLine($"OnUserAnswerScrolled called: HorizontalOffset={e.HorizontalOffset}, Synchronizing={_isScrollSynchronizing}");
        
        if (_isScrollSynchronizing)
            return;
            
        try
        {
            _isScrollSynchronizing = true;
            
            // Synchronize the original number CollectionView to match the user answer scroll position
            SynchronizeCollectionViewScroll(OriginalNumberCollectionView, e.HorizontalOffset);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"User answer scroll synchronization error: {ex.Message}");
        }
        finally
        {
            _isScrollSynchronizing = false;
        }
    }
    
    /// <summary>
    /// Synchronizes the target CollectionView to the specified horizontal offset
    /// </summary>
    /// <param name="targetCollectionView">The CollectionView to synchronize</param>
    /// <param name="horizontalOffset">The target horizontal scroll offset</param>
    private void SynchronizeCollectionViewScroll(CollectionView? targetCollectionView, double horizontalOffset)
    {
        System.Diagnostics.Debug.WriteLine($"SynchronizeCollectionViewScroll called: target={targetCollectionView?.GetHashCode()}, offset={horizontalOffset}");
        
        if (targetCollectionView == null || !targetCollectionView.IsVisible)
        {
            System.Diagnostics.Debug.WriteLine($"Target CollectionView is null or not visible");
            return;
        }
            
        try
        {
            if (targetCollectionView.ItemsSource is IList items && items.Count > 0)
            {
                // Calculate item width based on device idiom (matching XAML values)
                double itemWidth;
                if (DeviceInfo.Idiom == DeviceIdiom.Phone)
                    itemWidth = 40.0;
                else if (DeviceInfo.Idiom == DeviceIdiom.Tablet)
                    itemWidth = 50.0;
                else if (DeviceInfo.Idiom == DeviceIdiom.Desktop)
                    itemWidth = 60.0;
                else
                    itemWidth = 44.0; // Default fallback
                
                // Add ItemSpacing (4 pixels as defined in XAML)
                const double itemSpacing = 4.0;
                double totalItemWidth = itemWidth + itemSpacing;
                
                // Calculate target item index based on horizontal offset
                int targetIndex = Math.Max(0, Math.Min((int)(horizontalOffset / totalItemWidth), items.Count - 1));
                
                System.Diagnostics.Debug.WriteLine($"Calculated targetIndex={targetIndex} from offset={horizontalOffset}, totalItemWidth={totalItemWidth}");
                
                if (targetIndex < items.Count)
                {
                    var targetItem = items[targetIndex];
                    
                    System.Diagnostics.Debug.WriteLine($"Scrolling to item at index {targetIndex}");
                    // Use non-animated scrolling to avoid visual lag during synchronization
                    targetCollectionView.ScrollTo(targetItem, position: ScrollToPosition.Start, animate: false);
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"ItemsSource is null or empty: {targetCollectionView.ItemsSource?.GetType()}");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"CollectionView synchronization error: {ex.Message}");
        }
    }
    
    #endregion
}