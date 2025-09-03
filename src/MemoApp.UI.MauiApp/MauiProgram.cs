using Microsoft.Extensions.Logging;
using MemoApp.UI.MauiApp.Configuration;
using MemoApp.UI.MauiApp.Services;
using MemoApp.Localization.Extensions;
using MemoApp.Localization.Services;
using MemoApp.Core.Services.ErrorMonitoring;
using Plugin.AdMob;
using Plugin.AdMob.Configuration;

namespace MemoApp.UI.MauiApp;

/// <summary>
/// Main program entry point for the MAUI application
/// </summary>
public static class MauiProgram
{
    private static readonly ILogger _logger = LoggerFactory.Create(builder => builder.AddDebug())
        .CreateLogger(nameof(MauiProgram));

    /// <summary>
    /// Creates and configures the MAUI application
    /// </summary>
    /// <returns>Configured MAUI application instance</returns>
    public static Microsoft.Maui.Hosting.MauiApp CreateMauiApp()
    {
        var builder = Microsoft.Maui.Hosting.MauiApp.CreateBuilder();
        
        try
        {
            // Load configuration from embedded resources
            builder.LoadEmbeddedConfiguration();
            
            // Configure core MAUI application settings
            builder.ConfigureMauiApp();
            
            // Configure Sentry for error monitoring (before service registration)
            builder.ConfigureSentry();
            
            // Configure Ads (Google AdMob)
            builder.ConfigureAdMob();
            
            // Step 4: Register all services in proper dependency order
            builder.Services
                .AddCoreServices(builder.Configuration)
                .AddLocalizationServices()
                .AddAnalyticsServices()
                .AddFreemiumServices()
                .AddPackManagementServices(builder.Configuration)
                .AddMemoryTrainingServices()
                .AddCacheAndImageServices()
                .AddAssociationServices()
                .AddViewModels()
                .AddViews();

#if DEBUG
            builder.Logging.AddDebug();
#endif

            var app = builder.Build();
            
            // Initialize localization synchronously (required for UI)
            InitializeLocalization(app.Services);
            
            // Start async app initialization in background
            _ = InitializeAppServicesAsync(app.Services);
            
            return app;
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Failed to create MAUI application");
            throw;
        }
    }

    /// <summary>
    /// Configures Sentry error monitoring integration
    /// </summary>
    /// <param name="builder">The MAUI application builder</param>
    /// <returns>The builder for method chaining</returns>
    private static MauiAppBuilder ConfigureSentry(this MauiAppBuilder builder)
    {
        var dsn = builder.Configuration.GetSentryDsn();
        var isEnabled = builder.Configuration.IsErrorMonitoringEnabled();
        
        if (!string.IsNullOrEmpty(dsn) && isEnabled)
        {
            builder.UseSentry(options =>
            {
                options.Dsn = dsn;
                options.Environment = builder.Configuration["Sentry:Environment"] ?? "development";
                options.TracesSampleRate = double.TryParse(builder.Configuration["Sentry:TracesSampleRate"], out var traces) ? traces : 0.1;
                options.ProfilesSampleRate = double.TryParse(builder.Configuration["Sentry:ProfilesSampleRate"], out var profiles) ? profiles : 0.1;
                options.SendDefaultPii = bool.TryParse(builder.Configuration["Sentry:SendDefaultPii"], out var pii) && pii;
                options.AttachStacktrace = bool.TryParse(builder.Configuration["Sentry:AttachStacktrace"], out var stack) ? stack : true;
                options.AttachScreenshot = bool.TryParse(builder.Configuration["Sentry:AttachScreenshot"], out var screenshot) && screenshot;
                options.Debug = bool.TryParse(builder.Configuration["Sentry:Debug"], out var debug) && debug;
                
                if (Enum.TryParse<Sentry.SentryLevel>(builder.Configuration["Sentry:DiagnosticLevel"], out var level))
                {
                    options.DiagnosticLevel = level;
                }
            });
            
            _logger.LogInformation("Sentry error monitoring configured with DSN: {DsnMasked}", MaskSentryDsn(dsn));
        }
        else
        {
            _logger.LogWarning("Sentry error monitoring disabled - DSN: {HasDsn}, Enabled: {IsEnabled}", !string.IsNullOrEmpty(dsn), isEnabled);
        }
        
        return builder;
    }

    /// <summary>
    /// Initializes localization service synchronously (required for UI startup)
    /// </summary>
    /// <param name="services">The service provider</param>
    private static void InitializeLocalization(IServiceProvider services)
    {
        try
        {
            var localizationService = services.GetRequiredService<ILocalizationService>();
            // Initialize with default culture synchronously for UI startup
            // Full localization loading will happen in background initialization
            _logger.LogInformation("Localization service registered successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize localization - app may have display issues");
        }
    }

    /// <summary>
    /// Initializes application services asynchronously in background
    /// </summary>
    /// <param name="services">The service provider</param>
    private static async Task InitializeAppServicesAsync(IServiceProvider services)
    {
        try
        {
            var initializationService = services.GetRequiredService<IAppInitializationService>();
            var success = await initializationService.InitializeAsync();
            
            if (success)
            {
                _logger.LogInformation("App initialization completed successfully");
            }
            else
            {
                _logger.LogError("App initialization failed: {Status}", initializationService.CurrentStatus);
            }
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Critical error during app initialization");
            
            // Try to report to error monitoring if available
            try
            {
                var errorService = services.GetService<IErrorMonitoringService>();
                if (errorService != null)
                {
                    await errorService.LogCriticalErrorAsync(
                        "app_startup_failure",
                        ex,
                        new Dictionary<string, object>
                        {
                            ["startup_phase"] = "background_initialization"
                        });
                }
            }
            catch
            {
                // Ignore errors when reporting initialization failures
            }
        }
    }

    /// <summary>
    /// Masks sensitive parts of Sentry DSN for logging
    /// </summary>
    /// <param name="dsn">The full Sentry DSN</param>
    /// <returns>Masked DSN safe for logging</returns>
    private static string MaskSentryDsn(string dsn)
    {
        if (string.IsNullOrEmpty(dsn)) return "[empty]";
        
        try
        {
            var uri = new Uri(dsn);
            return $"{uri.Scheme}://***@{uri.Host}";
        }
        catch
        {
            return "[invalid]";
        }
    }

    private static MauiAppBuilder ConfigureAdMob(this MauiAppBuilder builder)
    {
        builder.UseAdMob()
            .UseConsentDebugSettings(new ConsentDebugSettings {
                Geography = ConsentDebugGeography.Eea,
                TestDeviceHashedIds = ["33BE2250B43518CCDA7DE426D04EE231"],
                //Reset = true
            });
        
#if DEBUG
        AdConfig.UseTestAdUnitIds = true;
#else
        // Only configure ad unit IDs for platforms that support AdMob (iOS and Android)
        if (DeviceInfo.Platform == DevicePlatform.iOS || DeviceInfo.Platform == DevicePlatform.Android)
        {
            var platform = DeviceInfo.Platform == DevicePlatform.iOS ? "iOS" : "Android";
            var bannerAdUnitId = builder.Configuration[$"Freemium:AdMob:{platform}:BannerAdUnitId"];
            var interstitialAdUnitId = builder.Configuration[$"Freemium:AdMob:{platform}:InterstitialAdUnitId"];
            
            if (!string.IsNullOrEmpty(bannerAdUnitId))
                AdConfig.DefaultBannerAdUnitId = bannerAdUnitId;
                
            if (!string.IsNullOrEmpty(interstitialAdUnitId))
                AdConfig.DefaultInterstitialAdUnitId = interstitialAdUnitId;
        }
#endif
        
        return builder;
    }
}