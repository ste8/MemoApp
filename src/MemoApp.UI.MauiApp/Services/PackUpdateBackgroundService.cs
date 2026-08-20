using System.Timers;
using MemoApp.Core.Services.Packs;
using Microsoft.Extensions.Logging;

namespace MemoApp.UI.MauiApp.Services;

public interface IPackUpdateBackgroundService
{
    int AvailableUpdatesCount { get; }
    bool HasAvailableUpdates { get; }
    DateTime? LastCheckTime { get; }
    bool IsCheckingForUpdates { get; }
    bool IsUserInActivity { get; }
    string LastErrorMessage { get; }
    bool HasRecentError { get; }
    
    event EventHandler<PackUpdateEventArgs>? UpdatesAvailableChanged;
    event EventHandler<string>? ErrorOccurred;
    
    Task StartAsync();
    Task StopAsync();
    Task<int> CheckForUpdatesAsync(bool silent = true);
    void SetUserActivityState(bool inActivity);
    Task<bool> ShouldCheckForUpdatesAsync();
    
    // Settings methods
    bool GetBackgroundChecksEnabled();
    void SetBackgroundChecksEnabled(bool enabled);
    int GetCheckIntervalHours();
    void SetCheckIntervalHours(int hours);
}

public class PackUpdateEventArgs : EventArgs
{
    public int UpdateCount { get; set; }
    public List<string> UpdatedPackIds { get; set; } = new();
    public bool IsSilentCheck { get; set; }
}

public class PackUpdateErrorEventArgs : EventArgs
{
    public string ErrorMessage { get; set; } = string.Empty;
    public bool IsUserFacing { get; set; }
}

public class PackUpdateBackgroundService : IPackUpdateBackgroundService, IDisposable
{
    private readonly IPackDownloadService _downloadService;
    private readonly IPackManagerService _packManager;
    private readonly ILogger<PackUpdateBackgroundService> _logger;
    private System.Timers.Timer? _updateCheckTimer;
    private bool _isUserInActivity;
    private bool _isCheckingForUpdates;
    private int _availableUpdatesCount;
    private DateTime? _lastCheckTime;
    private string _lastErrorMessage = string.Empty;
    private DateTime? _lastErrorTime;
    private readonly SemaphoreSlim _checkLock = new(1, 1);
    
    private const string LastCheckKey = "PackUpdateLastCheck";
    private const string UpdateCheckIntervalKey = "PackUpdateCheckInterval";
    private const string EnableBackgroundChecksKey = "PackUpdateEnableBackground";
    
    // Default check interval in hours
    private const int DefaultCheckIntervalHours = 24;
    
    public int AvailableUpdatesCount => _availableUpdatesCount;
    public bool HasAvailableUpdates => _availableUpdatesCount > 0;
    public DateTime? LastCheckTime => _lastCheckTime;
    public bool IsCheckingForUpdates => _isCheckingForUpdates;
    public bool IsUserInActivity => _isUserInActivity;
    public string LastErrorMessage => _lastErrorMessage;
    public bool HasRecentError => _lastErrorTime.HasValue && (DateTime.UtcNow - _lastErrorTime.Value).TotalHours < 1;
    
    public event EventHandler<PackUpdateEventArgs>? UpdatesAvailableChanged;
    public event EventHandler<string>? ErrorOccurred;
    
    public PackUpdateBackgroundService(
        IPackDownloadService downloadService,
        IPackManagerService packManager,
        ILogger<PackUpdateBackgroundService> logger)
    {
        _downloadService = downloadService;
        _packManager = packManager;
        _logger = logger;
    }
    
    public async Task StartAsync()
    {
        try
        {
            _logger.LogInformation("Starting pack update background service");
            System.Diagnostics.Debug.WriteLine("PackUpdateBackgroundService: StartAsync called");
            
            // Load last check time
            var lastCheckStr = Preferences.Get(LastCheckKey, string.Empty);
            // RoundtripKind is required: the value is written with ToString("O") and ends in Z,
            // and a bare TryParse converts it to local time. It is later subtracted from
            // DateTime.UtcNow, so without this the update interval is wrong by the device's
            // UTC offset - and changes when the user travels.
            if (!string.IsNullOrEmpty(lastCheckStr) && DateTime.TryParse(
                    lastCheckStr,
                    System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.RoundtripKind,
                    out var lastCheck))
            {
                _lastCheckTime = lastCheck;
                _logger.LogInformation("Last update check was at: {LastCheck}", lastCheck);
                System.Diagnostics.Debug.WriteLine($"PackUpdateBackgroundService: Last check was at {lastCheck}");
            }
            else
            {
                _logger.LogInformation("No previous update check found");
                System.Diagnostics.Debug.WriteLine("PackUpdateBackgroundService: No previous update check found");
            }
            
            // Check if background checks are enabled
            var enabled = Preferences.Get(EnableBackgroundChecksKey, true); // Default to true
            if (!enabled)
            {
                _logger.LogInformation("Background update checks are disabled");
                System.Diagnostics.Debug.WriteLine("PackUpdateBackgroundService: Background checks disabled");
                return;
            }
            
            // Get check interval
            var intervalHours = Preferences.Get(UpdateCheckIntervalKey, DefaultCheckIntervalHours);
            System.Diagnostics.Debug.WriteLine($"PackUpdateBackgroundService: Check interval: {intervalHours} hours");
            
            // Perform initial check if needed
            var shouldCheck = await ShouldCheckForUpdatesAsync();
            _logger.LogInformation("Should check for updates: {ShouldCheck}", shouldCheck);
            System.Diagnostics.Debug.WriteLine($"PackUpdateBackgroundService: Should check for updates: {shouldCheck}");
            
            if (shouldCheck)
            {
                _logger.LogInformation("Performing initial update check");
                System.Diagnostics.Debug.WriteLine("PackUpdateBackgroundService: Starting initial update check task");
                _ = Task.Run(async () => 
                {
                    await Task.Delay(2000); // Give app time to fully initialize
                    System.Diagnostics.Debug.WriteLine("PackUpdateBackgroundService: About to call CheckForUpdatesAsync");
                    await CheckForUpdatesAsync(silent: false); // Not silent for initial check
                });
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("PackUpdateBackgroundService: Skipping initial update check");
            }
            
            // Set up timer for periodic checks
            _updateCheckTimer?.Dispose();
            _updateCheckTimer = new System.Timers.Timer(TimeSpan.FromHours(intervalHours).TotalMilliseconds);
            _updateCheckTimer.Elapsed += async (sender, e) => await OnTimerElapsed();
            _updateCheckTimer.Start();
            
            _logger.LogInformation("Pack update service started with {Interval} hour interval", intervalHours);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start pack update background service");
        }
    }
    
    public async Task StopAsync()
    {
        try
        {
            _logger.LogInformation("Stopping pack update background service");
            _updateCheckTimer?.Stop();
            _updateCheckTimer?.Dispose();
            _updateCheckTimer = null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping pack update background service");
        }
        await Task.CompletedTask;
    }
    
    public async Task<int> CheckForUpdatesAsync(bool silent = true)
    {
        System.Diagnostics.Debug.WriteLine($"PackUpdateBackgroundService: CheckForUpdatesAsync called (silent: {silent})");
        
        await _checkLock.WaitAsync();
        try
        {
            if (_isCheckingForUpdates)
            {
                _logger.LogDebug("Update check already in progress");
                System.Diagnostics.Debug.WriteLine("PackUpdateBackgroundService: Update check already in progress");
                return _availableUpdatesCount;
            }
            
            _isCheckingForUpdates = true;
            _logger.LogInformation("Checking for pack updates (silent: {Silent})", silent);
            System.Diagnostics.Debug.WriteLine($"PackUpdateBackgroundService: Checking for pack updates (silent: {silent})");
            
            // Check network connectivity
            var connectivity = Connectivity.Current;
            if (connectivity.NetworkAccess != NetworkAccess.Internet)
            {
                _logger.LogWarning("No internet connection available for update check");
                return _availableUpdatesCount;
            }
            
            // Log connection type (WiFi, Cellular, etc.)
            var connectionProfiles = connectivity.ConnectionProfiles;
            var connectionTypes = string.Join(", ", connectionProfiles);
            _logger.LogDebug("Checking updates over connection types: {ConnectionTypes}", connectionTypes);
            
            // Get available packs with updates
            _logger.LogInformation("Fetching pack index for update check...");
            
            // Clear cache to ensure we get fresh data
            _downloadService.ClearCache();
            var index = await _downloadService.GetAvailablePacksAsync(forceRefresh: true);
            
            if (index == null)
            {
                _logger.LogWarning("Failed to fetch pack index");
                return _availableUpdatesCount;
            }
            
            _logger.LogInformation("Retrieved pack index with {PackCount} packs", index.Packs.Count);
            
            // Log pack details for debugging
            foreach (var pack in index.Packs)
            {
                _logger.LogDebug("Pack {PackId}: IsDownloaded={IsDownloaded}, HasUpdate={HasUpdate}, Version={Version}", 
                    pack.Id, pack.IsDownloaded, pack.HasUpdate, pack.Version);
            }
            
            var updatedPacks = index.Packs.Where(p => p.HasUpdate).ToList();
            var newUpdateCount = updatedPacks.Count;
            
            _logger.LogInformation("Found {UpdateCount} packs with updates: {UpdatedPacks}", 
                newUpdateCount, string.Join(", ", updatedPacks.Select(p => $"{p.Id}(v{p.Version})")));
            
            // Store last check time
            _lastCheckTime = DateTime.UtcNow;
            Preferences.Set(LastCheckKey, _lastCheckTime.Value.ToString("O"));
            
            // Check if update count changed
            if (newUpdateCount != _availableUpdatesCount || newUpdateCount > 0)
            {
                _availableUpdatesCount = newUpdateCount;
                
                if (newUpdateCount > 0)
                {
                    _logger.LogInformation("Found {Count} pack updates available for packs: {PackIds}", 
                        newUpdateCount, string.Join(", ", updatedPacks.Select(p => p.Id)));
                }
                else
                {
                    _logger.LogInformation("No pack updates available");
                }
                
                // Notify listeners
                var eventArgs = new PackUpdateEventArgs
                {
                    UpdateCount = newUpdateCount,
                    UpdatedPackIds = updatedPacks.Select(p => p.Id).ToList(),
                    IsSilentCheck = silent
                };
                
                _logger.LogInformation("Notifying listeners of update status change");
                UpdatesAvailableChanged?.Invoke(this, eventArgs);
            }
            
            return _availableUpdatesCount;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking for pack updates");
            
            // Store error information
            _lastErrorMessage = "Unable to check for pack updates. Check your internet connection.";
            _lastErrorTime = DateTime.UtcNow;
            
            // Notify about error if not silent
            if (!silent)
            {
                ErrorOccurred?.Invoke(this, _lastErrorMessage);
            }
            
            return _availableUpdatesCount;
        }
        finally
        {
            _isCheckingForUpdates = false;
            _checkLock.Release();
        }
    }
    
    public void SetUserActivityState(bool inActivity)
    {
        _isUserInActivity = inActivity;
        _logger.LogDebug("User activity state changed to: {InActivity}", inActivity);
        
        // If user just finished an activity and we have pending updates, we could notify
        // This will be handled by the UI layer based on the UpdatesAvailableChanged event
    }
    
    public async Task<bool> ShouldCheckForUpdatesAsync()
    {
        try
        {
            if (!_lastCheckTime.HasValue)
                return true;
            
            // Get check interval
            var intervalHours = Preferences.Get(UpdateCheckIntervalKey, DefaultCheckIntervalHours);
            
            var timeSinceLastCheck = DateTime.UtcNow - _lastCheckTime.Value;
            return timeSinceLastCheck.TotalHours >= intervalHours;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error determining if update check is needed");
            return false;
        }
    }
    
    private async Task OnTimerElapsed()
    {
        try
        {
            // Don't check if user is in an activity
            if (_isUserInActivity)
            {
                _logger.LogDebug("Skipping scheduled update check - user is in activity");
                return;
            }
            
            await CheckForUpdatesAsync(silent: true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during scheduled update check");
            
            // Store error but don't notify user for background checks
            _lastErrorMessage = "Background update check failed";
            _lastErrorTime = DateTime.UtcNow;
        }
    }
    
    // Settings methods
    public bool GetBackgroundChecksEnabled()
    {
        return Preferences.Get(EnableBackgroundChecksKey, true);
    }
    
    public void SetBackgroundChecksEnabled(bool enabled)
    {
        Preferences.Set(EnableBackgroundChecksKey, enabled);
        _logger.LogInformation("Background checks enabled set to: {Enabled}", enabled);
    }
    
    public int GetCheckIntervalHours()
    {
        return Preferences.Get(UpdateCheckIntervalKey, DefaultCheckIntervalHours);
    }
    
    public void SetCheckIntervalHours(int hours)
    {
        if (hours < 1) hours = 1; // Minimum 1 hour
        if (hours > 168) hours = 168; // Maximum 1 week
        
        Preferences.Set(UpdateCheckIntervalKey, hours);
        _logger.LogInformation("Check interval set to: {Hours} hours", hours);
        
        // Restart timer with new interval if running
        if (_updateCheckTimer != null)
        {
            _updateCheckTimer.Interval = TimeSpan.FromHours(hours).TotalMilliseconds;
        }
    }
    
    public void Dispose()
    {
        _updateCheckTimer?.Dispose();
        _checkLock?.Dispose();
    }
}

public class UpdateCheckSettings
{
    public bool EnableBackgroundChecks { get; set; } = true;
    public int CheckIntervalHours { get; set; } = 24;
    public UpdateNotificationLevel NotificationLevel { get; set; } = UpdateNotificationLevel.Subtle;
}

public enum UpdateNotificationLevel
{
    None,        // Only show in Settings
    Subtle,      // Badge only  
    Normal,      // Badge + banner on main page
    Prominent    // Badge + banner + toast (after activity)
}