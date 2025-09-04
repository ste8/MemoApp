using MemoApp.Core.Services.Analytics;
using MemoApp.Core.Services.ErrorMonitoring;
using MemoApp.Core.MajorSystem.Services;
using MemoApp.Core.BigNumber;
using MemoApp.Core.Services.Packs;
using MemoApp.Localization.Services;
using MemoApp.UI.MauiApp.Services;
using MemoApp.UI.MauiApp.Services.Analytics;
using MemoApp.UI.MauiApp.Services.ErrorMonitoring;
using MemoApp.UI.MauiApp.Services.Freemium;
using MemoApp.UI.MauiApp.ViewModels;
using MemoApp.UI.MauiApp.Views;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace MemoApp.UI.MauiApp.Configuration;

/// <summary>
/// Handles service registration for dependency injection container
/// </summary>
public static class ServiceRegistration
{
    /// <summary>
    /// Registers core infrastructure services (error monitoring, initialization, messaging)
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configuration">Application configuration</param>
    /// <returns>The service collection for method chaining</returns>
    public static IServiceCollection AddCoreServices(this IServiceCollection services, IConfiguration configuration)
    {
        // HTTP Client for network operations
        services.AddHttpClient();
        
        // Error Monitoring Services (early in pipeline)
        services.AddSingleton<IErrorMonitoringService, SentryErrorMonitoringService>();
        services.AddSingleton<GlobalExceptionHandler>();
        
        // App Initialization Service
        services.AddSingleton<IAppInitializationService, AppInitializationService>();
        
        // App Info Service for version information
        services.AddSingleton<IAppInfoService, AppInfoService>();
        
        // User notification service for consistent error feedback
        services.AddSingleton<IUserNotificationService, UserNotificationService>();
        
        // Messaging services
        services.AddSingleton<IMessenger>(WeakReferenceMessenger.Default);
        
        // AdMob registered as singleton since it uses default AdConfig settings
        services.AddSingleton<IAdMobService, AdMobService>();
        
        return services;
    }

    /// <summary>
    /// Registers localization services
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <returns>The service collection for method chaining</returns>
    public static IServiceCollection AddLocalizationServices(this IServiceCollection services)
    {
        services.AddSingleton<ILocalizationService, MauiLocalizationService>();
        return services;
    }

    /// <summary>
    /// Registers analytics services with platform-specific implementations
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <returns>The service collection for method chaining</returns>
    public static IServiceCollection AddAnalyticsServices(this IServiceCollection services)
    {
        // Platform-specific analytics implementation
#if ANDROID || IOS
        services.AddSingleton<IAnalyticsService, FirebaseAnalyticsService>();
#else
        services.AddSingleton<IAnalyticsService, LocalAnalyticsService>();
#endif
        return services;
    }

    /// <summary>
    /// Registers freemium services (subscriptions, promotions, ads)
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <returns>The service collection for method chaining</returns>
    public static IServiceCollection AddFreemiumServices(this IServiceCollection services)
    {
        // Subscription service
        services.AddSingleton<ISubscriptionService, SubscriptionService>();
        
        // Promotion service
        services.AddSingleton<IPromotionService, PromotionService>();
        
        // Interstitial ad manager
        services.AddTransient<IInterstitialAdManager, InterstitialAdManager>();
        
        return services;
    }

    /// <summary>
    /// Registers pack management and content services
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configuration">Application configuration</param>
    /// <returns>The service collection for method chaining</returns>
    public static IServiceCollection AddPackManagementServices(this IServiceCollection services, IConfiguration configuration)
    {
        var packsIndexUrl = configuration.GetPacksIndexUrl();
        var cacheMinutes = configuration.GetCacheDurationMinutes();

        // Pack management services
        services.AddSingleton<IThumbnailService, ThumbnailService>();
        services.AddSingleton<IPackManagerService>(serviceProvider =>
        {
            var logger = serviceProvider.GetRequiredService<ILogger<PackManagerService>>();
            return new PackManagerService(logger, FileSystem.Current.AppDataDirectory);
        });
        services.AddSingleton<MauiPackService>();
        services.AddSingleton<IPackDownloadService>(serviceProvider =>
        {
            var httpClient = serviceProvider.GetRequiredService<HttpClient>();
            var packManager = serviceProvider.GetRequiredService<IPackManagerService>();
            var logger = serviceProvider.GetRequiredService<ILogger<PackDownloadService>>();
            return new PackDownloadService(httpClient, packManager, logger, packsIndexUrl, FileSystem.Current.AppDataDirectory, cacheMinutes);
        });
        
        // Background update service
        services.AddSingleton<IPackUpdateBackgroundService, PackUpdateBackgroundService>();
        
        return services;
    }

    /// <summary>
    /// Registers major system and memory training services
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <returns>The service collection for method chaining</returns>
    public static IServiceCollection AddMemoryTrainingServices(this IServiceCollection services)
    {
        // Major System services
        services.AddSingleton<IMajorSystemCategoryService, MajorSystemCategoryService>();
        services.AddSingleton<ITokenDefaultsService>(serviceProvider =>
        {
            var logger = serviceProvider.GetRequiredService<ILogger<TokenDefaultsService>>();
            var packManager = serviceProvider.GetRequiredService<IPackManagerService>();
            return new TokenDefaultsService(logger, packManager);
        });
        
        // Big Number service
        services.AddSingleton<IBigNumberService, BigNumberService>();
        
        return services;
    }

    /// <summary>
    /// Registers cache and image processing services
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <returns>The service collection for method chaining</returns>
    public static IServiceCollection AddCacheAndImageServices(this IServiceCollection services)
    {
        // Cache services
        services.AddSingleton<IPackAssociationsCacheService, PackAssociationsCacheService>();
        services.AddSingleton<IImageCacheService, ImageCacheService>();
        
        // Image processing services
        services.AddSingleton<IImageProcessingService, MauiImageProcessingService>();
        services.AddSingleton<ICustomImageSourceService, MauiImageSourceService>();
        
        // UI measurement services
        services.AddSingleton<ITextMeasurementService, TextMeasurementService>();
        services.AddSingleton<IContainerSizeService, ContainerSizeService>();
        services.AddSingleton<IBigNumberSettingsService, BigNumberSettingsService>();
        
        return services;
    }

    /// <summary>
    /// Registers association services (depends on other services being registered first)
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <returns>The service collection for method chaining</returns>
    public static IServiceCollection AddAssociationServices(this IServiceCollection services)
    {
        // Custom associations service
        services.AddSingleton<ICustomAssociationsService>(serviceProvider =>
        {
            var packManager = serviceProvider.GetRequiredService<IPackManagerService>();
            var imageProcessingService = serviceProvider.GetRequiredService<IImageProcessingService>();
            var tokenDefaultsService = serviceProvider.GetRequiredService<ITokenDefaultsService>();
            var logger = serviceProvider.GetRequiredService<ILogger<CustomAssociationsService>>();
            return new CustomAssociationsService(packManager, imageProcessingService, tokenDefaultsService, logger);
        });
        
        // Pack associations service (depends on custom associations service)
        services.AddSingleton<IPackAssociationsService>(serviceProvider =>
        {
            var packManager = serviceProvider.GetRequiredService<IPackManagerService>();
            var categoryService = serviceProvider.GetRequiredService<IMajorSystemCategoryService>();
            var tokenDefaultsService = serviceProvider.GetRequiredService<ITokenDefaultsService>();
            var customAssociationsService = serviceProvider.GetRequiredService<ICustomAssociationsService>();
            var cacheService = serviceProvider.GetRequiredService<IPackAssociationsCacheService>();
            var logger = serviceProvider.GetRequiredService<ILogger<PackAssociationsService>>();
            return new PackAssociationsService(packManager, categoryService, tokenDefaultsService, customAssociationsService, cacheService, logger, FileSystem.Current.AppDataDirectory);
        });

        return services;
    }

    /// <summary>
    /// Registers ViewModels for dependency injection
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <returns>The service collection for method chaining</returns>
    public static IServiceCollection AddViewModels(this IServiceCollection services)
    {
        // Singleton ViewModels (shared state)
        services.AddSingleton<MainViewModel>(serviceProvider =>
        {
            var localizationService = serviceProvider.GetRequiredService<ILocalizationService>();
            var userNotificationService = serviceProvider.GetRequiredService<IUserNotificationService>();
            var logger = serviceProvider.GetRequiredService<ILogger<MainViewModel>>();
            return new MainViewModel(localizationService, userNotificationService, logger);
        });
        
        // Transient ViewModels (new instance per navigation)
        services.AddTransient<FlashNumbersViewModel>(serviceProvider =>
        {
            var localizationService = serviceProvider.GetRequiredService<ILocalizationService>();
            var analyticsService = serviceProvider.GetRequiredService<IAnalyticsService>();
            var interstitialAdManager = serviceProvider.GetService<IInterstitialAdManager>(); // Optional
            return new FlashNumbersViewModel(localizationService, analyticsService, interstitialAdManager);
        });
        
        services.AddTransient<StatisticsViewModel>();
        services.AddTransient<HelpViewModel>();
        
        services.AddTransient<AssociationsViewModel>(serviceProvider =>
        {
            var categoryService = serviceProvider.GetRequiredService<IMajorSystemCategoryService>();
            var packAssociationsService = serviceProvider.GetRequiredService<IPackAssociationsService>();
            var tokenDefaultsService = serviceProvider.GetRequiredService<ITokenDefaultsService>();
            var localizationService = serviceProvider.GetRequiredService<ILocalizationService>();
            var messenger = serviceProvider.GetRequiredService<IMessenger>();
            var imageCacheService = serviceProvider.GetRequiredService<IImageCacheService>();
            var analyticsService = serviceProvider.GetRequiredService<IAnalyticsService>();
            var userNotificationService = serviceProvider.GetRequiredService<IUserNotificationService>();
            var logger = serviceProvider.GetRequiredService<ILogger<AssociationsViewModel>>();
            var configuration = serviceProvider.GetRequiredService<IConfiguration>();
            return new AssociationsViewModel(categoryService, packAssociationsService, tokenDefaultsService, localizationService, messenger, imageCacheService, analyticsService, userNotificationService, logger, configuration);
        });
        
        services.AddTransient<PackDownloadViewModel>(serviceProvider =>
        {
            var downloadService = serviceProvider.GetRequiredService<IPackDownloadService>();
            var packManager = serviceProvider.GetRequiredService<IPackManagerService>();
            var mauiPackService = serviceProvider.GetRequiredService<MauiPackService>();
            var localizationService = serviceProvider.GetRequiredService<ILocalizationService>();
            var analyticsService = serviceProvider.GetRequiredService<IAnalyticsService>();
            var logger = serviceProvider.GetRequiredService<ILogger<PackDownloadViewModel>>();
            return new PackDownloadViewModel(downloadService, packManager, mauiPackService, localizationService, analyticsService, logger);
        });
        
        services.AddTransient<BigNumberViewModel>(serviceProvider =>
        {
            var gameService = serviceProvider.GetRequiredService<IBigNumberService>();
            var settingsService = serviceProvider.GetRequiredService<IBigNumberSettingsService>();
            var localizationService = serviceProvider.GetRequiredService<ILocalizationService>();
            var textMeasurementService = serviceProvider.GetRequiredService<ITextMeasurementService>();
            var containerSizeService = serviceProvider.GetRequiredService<IContainerSizeService>();
            var analyticsService = serviceProvider.GetRequiredService<IAnalyticsService>();
            return new BigNumberViewModel(gameService, settingsService, localizationService, textMeasurementService, containerSizeService, analyticsService);
        });
        
        services.AddTransient<TokenDetailViewModel>(serviceProvider =>
        {
            var packAssociationsService = serviceProvider.GetRequiredService<IPackAssociationsService>();
            var tokenDefaultsService = serviceProvider.GetRequiredService<ITokenDefaultsService>();
            var customAssociationsService = serviceProvider.GetRequiredService<ICustomAssociationsService>();
            var localizationService = serviceProvider.GetRequiredService<ILocalizationService>();
            var messenger = serviceProvider.GetRequiredService<IMessenger>();
            var imageCacheService = serviceProvider.GetRequiredService<IImageCacheService>();
            var logger = serviceProvider.GetRequiredService<ILogger<TokenDetailViewModel>>();
            return new TokenDetailViewModel(packAssociationsService, tokenDefaultsService, customAssociationsService, localizationService, messenger, imageCacheService, logger);
        });
        
        services.AddTransient<AddCustomAssociationViewModel>(serviceProvider =>
        {
            var customAssociationsService = serviceProvider.GetRequiredService<ICustomAssociationsService>();
            var imageSourceService = serviceProvider.GetRequiredService<ICustomImageSourceService>();
            var packAssociationsService = serviceProvider.GetRequiredService<IPackAssociationsService>();
            var tokenDefaultsService = serviceProvider.GetRequiredService<ITokenDefaultsService>();
            var localizationService = serviceProvider.GetRequiredService<ILocalizationService>();
            var userNotificationService = serviceProvider.GetRequiredService<IUserNotificationService>();
            var messenger = serviceProvider.GetRequiredService<IMessenger>();
            var logger = serviceProvider.GetRequiredService<ILogger<AddCustomAssociationViewModel>>();
            return new AddCustomAssociationViewModel(customAssociationsService, imageSourceService, packAssociationsService, tokenDefaultsService, localizationService, userNotificationService, messenger, logger);
        });
        
        services.AddTransient<ImageViewerViewModel>(serviceProvider =>
        {
            var logger = serviceProvider.GetRequiredService<ILogger<ImageViewerViewModel>>();
            return new ImageViewerViewModel(logger);
        });
        
        services.AddTransient<SettingsViewModel>(serviceProvider => 
        {
            var localizationService = serviceProvider.GetRequiredService<ILocalizationService>();
            var packManager = serviceProvider.GetRequiredService<IPackManagerService>();
            var downloadService = serviceProvider.GetRequiredService<IPackDownloadService>();
            var updateService = serviceProvider.GetService<IPackUpdateBackgroundService>(); // Optional
            var analyticsService = serviceProvider.GetRequiredService<IAnalyticsService>();
            var appInfoService = serviceProvider.GetRequiredService<IAppInfoService>();
            var userNotificationService = serviceProvider.GetRequiredService<IUserNotificationService>();
            var logger = serviceProvider.GetRequiredService<ILogger<SettingsViewModel>>();
            return new SettingsViewModel(localizationService, packManager, downloadService, updateService, analyticsService, appInfoService, userNotificationService, logger);
        });
        
        return services;
    }

    /// <summary>
    /// Registers Views/Pages for dependency injection
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <returns>The service collection for method chaining</returns>
    public static IServiceCollection AddViews(this IServiceCollection services)
    {
        // Register pages
        services.AddSingleton<Views.TestPage>();
        services.AddSingleton<Views.MainPage>();
        services.AddTransient<Views.FlashNumbersPage>();
        services.AddTransient<Views.StatisticsPage>();
        services.AddTransient<Views.HelpPage>();
        services.AddTransient<Views.SettingsPage>();
        services.AddTransient<Views.AssociationsPage>();
        services.AddTransient<Views.PackDownloadPage>();
        services.AddTransient<Views.BigNumberPage>();
        services.AddTransient<Views.TokenDetailPage>();
        services.AddTransient<Views.AddCustomAssociationPage>();
        services.AddTransient<Views.ImageViewerPage>();
        
        return services;
    }
}