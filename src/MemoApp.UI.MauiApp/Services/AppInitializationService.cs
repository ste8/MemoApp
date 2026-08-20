using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MemoApp.Core.Services.Analytics;
using MemoApp.Core.Services.ErrorMonitoring;
using MemoApp.Localization.Services;
using MemoApp.UI.MauiApp.Services.ErrorMonitoring;
using Microsoft.Extensions.Logging;

namespace MemoApp.UI.MauiApp.Services;

/// <summary>
/// Coordinates app initialization in proper dependency order with error handling and timeout protection
/// </summary>
public class AppInitializationService : IAppInitializationService
{
    private readonly ILocalizationService _localizationService;
    private readonly IErrorMonitoringService? _errorService;
    private readonly IAnalyticsService _analyticsService;
    private readonly GlobalExceptionHandler? _globalHandler;
    private readonly IUserNotificationService? _userNotificationService;
    private readonly ILogger<AppInitializationService> _logger;
    
    private volatile bool _isInitialized = false;
    private volatile bool _isCriticalInitialized = false;
    private double _initializationProgress = 0.0;
    private volatile string _currentStatus = "Not started";
    
    private readonly object _progressLock = new object();

    public AppInitializationService(
        ILocalizationService localizationService,
        IAnalyticsService analyticsService,
        ILogger<AppInitializationService> logger,
        IErrorMonitoringService? errorService = null,
        GlobalExceptionHandler? globalHandler = null,
        IUserNotificationService? userNotificationService = null)
    {
        _localizationService = localizationService;
        _errorService = errorService;
        _analyticsService = analyticsService;
        _globalHandler = globalHandler;
        _userNotificationService = userNotificationService;
        _logger = logger;
    }

    public bool IsInitialized => _isInitialized;
    public bool IsCriticalInitialized => _isCriticalInitialized;
    public double InitializationProgress 
    { 
        get 
        { 
            lock (_progressLock) 
            { 
                return _initializationProgress; 
            } 
        } 
    }
    public string CurrentStatus => _currentStatus;

    public async Task<bool> InitializeAsync(CancellationToken cancellationToken = default)
    {
        if (_isInitialized)
        {
            _logger.LogDebug("App already initialized, skipping");
            return true;
        }

        _logger.LogInformation("Starting app initialization");
        _currentStatus = "Starting initialization";
        lock (_progressLock) 
        { 
            _initializationProgress = 0.0; 
        }

        try
        {
            // Use a timeout to prevent hanging during initialization
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(TimeSpan.FromSeconds(30)); // 30 second timeout

            // Step 1: Initialize localization service (required by many other services)
            await InitializeLocalizationAsync(timeoutCts.Token);
            lock (_progressLock) { _initializationProgress = 0.25; }

            // Step 2: Initialize error monitoring service (critical for app stability)
            await InitializeErrorMonitoringAsync(timeoutCts.Token);
            lock (_progressLock) { _initializationProgress = 0.5; }
            _isCriticalInitialized = true; // App can function with just localization + error monitoring

            // Step 3: Initialize global exception handler (depends on error monitoring)
            await InitializeGlobalExceptionHandlerAsync(timeoutCts.Token);
            await ReportFirebaseInitializationFailureAsync();
            lock (_progressLock) { _initializationProgress = 0.75; }

            // Step 4: Initialize analytics service (optional, can fail without breaking app)
            await InitializeAnalyticsAsync(timeoutCts.Token);
            lock (_progressLock) { _initializationProgress = 1.0; }

            _isInitialized = true;
            _currentStatus = "Initialization complete";
            _logger.LogInformation("App initialization completed successfully");
            
            return true;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning("App initialization was cancelled by user");
            _currentStatus = "Initialization cancelled";
            return false;
        }
        catch (OperationCanceledException)
        {
            _logger.LogError("App initialization timed out after 30 seconds");
            _currentStatus = "Initialization timed out";
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Critical error during app initialization");
            _currentStatus = $"Initialization failed: {ex.Message}";
            
            // Try to log the error if error monitoring was initialized
            if (_errorService != null && _isCriticalInitialized)
            {
                try
                {
                    await _errorService.LogCriticalErrorAsync(
                        CriticalErrorTypes.AppCrash,
                        ex,
                        new Dictionary<string, object>
                        {
                            ["crash_location"] = "app_initialization",
                            ["initialization_step"] = _currentStatus,
                            ["progress"] = InitializationProgress
                        });
                }
                catch
                {
                    // Ignore errors when trying to log initialization failures
                }
            }

            return false;
        }
    }

    private async Task InitializeLocalizationAsync(CancellationToken cancellationToken)
    {
        _currentStatus = "Initializing localization";
        _logger.LogDebug("Initializing localization service");

        try
        {
            // Load saved language and number format
            await _localizationService.LoadSavedLanguageAsync().WaitAsync(cancellationToken);
            await _localizationService.LoadSavedNumberFormatAsync().WaitAsync(cancellationToken);
            
            _logger.LogInformation("Localization service initialized successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize localization service");
            throw new InvalidOperationException("Localization service initialization failed. App cannot continue.", ex);
        }
    }

    private async Task InitializeErrorMonitoringAsync(CancellationToken cancellationToken)
    {
        _currentStatus = "Initializing error monitoring";
        
        if (_errorService == null)
        {
            _logger.LogWarning("Error monitoring service not available, skipping initialization");
            return;
        }

        try
        {
            _logger.LogDebug("Initializing error monitoring service");
            await _errorService.InitializeAsync().WaitAsync(cancellationToken);
            
            // Set initial user context with localization info
            await _errorService.SetUserContextAsync(new Dictionary<string, object>
            {
                ["session_id"] = Guid.NewGuid().ToString(),
                ["app_version"] = AppInfo.VersionString,
                ["platform"] = DeviceInfo.Platform.ToString(),
                ["device_model"] = DeviceInfo.Model,
                ["ui_language"] = _localizationService.CurrentCulture.TwoLetterISOLanguageName
            }).WaitAsync(cancellationToken);
            
            _logger.LogInformation("Error monitoring service initialized successfully");
        }
        catch (Exception ex)
        {
            // Error monitoring is critical for production apps, but we can continue without it in development
            _logger.LogError(ex, "Failed to initialize error monitoring service");
#if DEBUG
            _logger.LogWarning("Continuing without error monitoring in debug mode");
#else
            throw new InvalidOperationException("Error monitoring service initialization failed in production", ex);
#endif
        }
    }

    /// <summary>
    /// Surfaces a Firebase initialisation failure from platform startup. Without this the
    /// failure is invisible in Release and analytics is simply dead with no explanation.
    /// </summary>
    private async Task ReportFirebaseInitializationFailureAsync()
    {
        var failure = Services.Analytics.FirebaseInitializationStatus.Consume();

        if (failure is null)
        {
            return;
        }

        _logger.LogError(failure, "Firebase initialization failed during platform startup; analytics will use the local fallback");

        try
        {
            await _errorService.LogCriticalErrorAsync(
                CriticalErrorTypes.InitializationFailure,
                failure,
                new Dictionary<string, object> { ["component"] = "firebase" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to report Firebase initialization failure");
        }
    }

    private async Task InitializeGlobalExceptionHandlerAsync(CancellationToken cancellationToken)
    {
        _currentStatus = "Initializing exception handling";
        
        if (_globalHandler == null)
        {
            _logger.LogWarning("Global exception handler not available, skipping initialization");
            return;
        }

        try
        {
            _logger.LogDebug("Initializing global exception handler");
            
            // This is synchronous but we wrap it for consistency and timeout protection
            await Task.Run(() => _globalHandler.Initialize(), cancellationToken).WaitAsync(cancellationToken);
            
            _logger.LogInformation("Global exception handler initialized successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize global exception handler");
            // Global exception handler is nice to have but not critical
            _logger.LogWarning("Continuing without global exception handler");
        }
    }

    private async Task InitializeAnalyticsAsync(CancellationToken cancellationToken)
    {
        _currentStatus = "Initializing analytics";
        
        try
        {
            _logger.LogDebug("Initializing analytics service");
            
            // Track app start - this is optional and can fail without affecting app functionality
            await _analyticsService.TrackAppStartAsync().WaitAsync(cancellationToken);
            
            _logger.LogInformation("Analytics service initialized successfully");
        }
        catch (Exception ex)
        {
            // Analytics is completely optional - log the error but don't fail initialization
            _logger.LogWarning(ex, "Analytics service initialization failed - continuing without analytics");
            
            // Try to log this as a performance issue if error monitoring is available
            if (_errorService != null)
            {
                try
                {
                    await _errorService.LogPerformanceIssueAsync(
                        "analytics_init_failure",
                        new Dictionary<string, object>
                        {
                            ["error_message"] = ex.Message,
                            ["error_type"] = ex.GetType().Name
                        });
                }
                catch
                {
                    // Ignore errors when trying to log analytics failures
                }
            }
        }
    }
}