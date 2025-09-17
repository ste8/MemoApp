using MemoApp.Localization.Services;
using MemoApp.UI.MauiApp.Services;
using Microsoft.Extensions.DependencyInjection;

namespace MemoApp.UI.MauiApp.Views;

public partial class UpdateNotificationBanner : ContentView
{
    private IPackUpdateBackgroundService? _updateService;
    private ILocalizationService? _localizationService;
    private bool _isAnimating;
    private bool _isInitialized;
    
    public UpdateNotificationBanner()
    {
        InitializeComponent();
    }
    
    protected override void OnHandlerChanged()
    {
        base.OnHandlerChanged();
        
        if (Handler != null && !_isInitialized)
        {
            InitializeServices();
        }
        else if (Handler == null && _updateService != null)
        {
            // Cleanup when view is removed
            _updateService.UpdatesAvailableChanged -= OnUpdatesAvailableChanged;
        }
    }
    
    private void InitializeServices()
    {
        if (_isInitialized) return;
        
        try
        {
            // Get services from DI when handler is available
            var services = Handler?.MauiContext?.Services;
            if (services == null) return;
            
            _updateService = services.GetService<IPackUpdateBackgroundService>();
            _localizationService = services.GetService<ILocalizationService>();
            
            if (_updateService == null || _localizationService == null)
            {
                System.Diagnostics.Debug.WriteLine("UpdateNotificationBanner: Required services not found");
                return;
            }
            
            // Subscribe to update events
            _updateService.UpdatesAvailableChanged += OnUpdatesAvailableChanged;
            
            // Set up action button
            ActionButton.Clicked += OnActionButtonClicked;
            
            _isInitialized = true;
            
            // Check initial state after a short delay to ensure service has initialized
            Dispatcher.DispatchDelayed(TimeSpan.FromMilliseconds(500), () =>
            {
                System.Diagnostics.Debug.WriteLine($"UpdateNotificationBanner: Initial UpdateVisibility check");
                UpdateVisibility();
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"UpdateNotificationBanner initialization error: {ex}");
        }
    }
    
    private void OnUpdatesAvailableChanged(object? sender, PackUpdateEventArgs e)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            UpdateVisibility();
            
            if (e.UpdateCount > 0 && !e.IsSilentCheck && !_updateService.IsUserInActivity)
            {
                // Animate the banner to draw attention
                _ = AnimateBanner();
            }
        });
    }
    
    private void UpdateVisibility()
    {
        System.Diagnostics.Debug.WriteLine($"UpdateNotificationBanner.UpdateVisibility called");
        
        if (_updateService == null || _localizationService == null)
        {
            System.Diagnostics.Debug.WriteLine($"UpdateNotificationBanner: Services not available - UpdateService: {_updateService != null}, LocalizationService: {_localizationService != null}");
            IsVisible = false;
            return;
        }
        
        System.Diagnostics.Debug.WriteLine($"UpdateNotificationBanner: HasAvailableUpdates: {_updateService.HasAvailableUpdates}, Count: {_updateService.AvailableUpdatesCount}");
        
        if (_updateService.HasAvailableUpdates)
        {
            var count = _updateService.AvailableUpdatesCount;
            
            // Set message based on count
            if (count == 1)
            {
                MessageLabel.Text = _localizationService.GetString("Update_SinglePackAvailable");
            }
            else
            {
                MessageLabel.Text = _localizationService.GetString("Update_MultiplePacksAvailable")
                    .Replace("{0}", count.ToString());
            }
            
            // Show last check time if available
            if (_updateService.LastCheckTime.HasValue)
            {
                var timeAgo = GetTimeAgoString(_updateService.LastCheckTime.Value);
                DetailsLabel.Text = _localizationService.GetString("Update_LastChecked")
                    .Replace("{0}", timeAgo);
                DetailsLabel.IsVisible = true;
            }
            
            System.Diagnostics.Debug.WriteLine($"UpdateNotificationBanner: Showing banner with message: {MessageLabel.Text}");
            IsVisible = true;
        }
        else
        {
            System.Diagnostics.Debug.WriteLine($"UpdateNotificationBanner: Hiding banner - no updates available");
            IsVisible = false;
        }
    }
    
    private async Task AnimateBanner()
    {
        if (_isAnimating || !IsVisible) return;

        try
        {
            _isAnimating = true;

            // Subtle slide in animation
            this.TranslationY = -50;
            this.Opacity = 0;

            await Task.WhenAll(
                this.TranslateTo(0, 0, 500, Easing.CubicOut),
                this.FadeTo(1, 500)
            );

            // Gentle pulse effect
            await this.ScaleTo(1.02, 200);
            await this.ScaleTo(1.0, 200);
        }
        finally
        {
            _isAnimating = false;
        }
    }
    
    private async void OnActionButtonClicked(object? sender, EventArgs e)
    {
        try
        {
            // Navigate to pack download page
            await Shell.Current.GoToAsync(nameof(PackDownloadPage));
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to navigate to pack download page: {ex}");
        }
    }
    
    private string GetTimeAgoString(DateTime dateTime)
    {
        var timeSpan = DateTime.UtcNow - dateTime;
        
        if (timeSpan.TotalMinutes < 1)
            return _localizationService.GetString("Time_JustNow");
        if (timeSpan.TotalMinutes < 60)
            return _localizationService.GetString("Time_MinutesAgo").Replace("{0}", ((int)timeSpan.TotalMinutes).ToString());
        if (timeSpan.TotalHours < 24)
            return _localizationService.GetString("Time_HoursAgo").Replace("{0}", ((int)timeSpan.TotalHours).ToString());
        if (timeSpan.TotalDays < 7)
            return _localizationService.GetString("Time_DaysAgo").Replace("{0}", ((int)timeSpan.TotalDays).ToString());
        
        return dateTime.ToLocalTime().ToString("d");
    }
}