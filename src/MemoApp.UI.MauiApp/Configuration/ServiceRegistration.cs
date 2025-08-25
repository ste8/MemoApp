using MemoApp.Core.Services.Analytics;
using MemoApp.Core.Services.ErrorMonitoring;
using MemoApp.Core.MajorSystem.Services;
using MemoApp.Core.NumberMemorization;
using MemoApp.Core.Services.Packs;
using MemoApp.Localization.Services;
using MemoApp.UI.MauiApp.Services;
using MemoApp.UI.MauiApp.Services.Analytics;
using MemoApp.UI.MauiApp.Services.ErrorMonitoring;
using MemoApp.UI.MauiApp.ViewModels;
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
        
        // Messaging services
        services.AddSingleton<IMessenger>(WeakReferenceMessenger.Default);
        
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
        
        // Number memorization service
        services.AddSingleton<INumberMemorizationService, NumberMemorizationService>();
        
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
        services.AddSingleton<MainViewModel>();
        
        // Transient ViewModels (new instance per navigation)
        services.AddTransient<FlashNumbersViewModel>(serviceProvider =>
        {
            var localizationService = serviceProvider.GetRequiredService<ILocalizationService>();
            var analyticsService = serviceProvider.GetRequiredService<IAnalyticsService>();
            return new FlashNumbersViewModel(localizationService, analyticsService);
        });
        
        services.AddTransient<StatisticsViewModel>();
        services.AddTransient<HelpViewModel>();
        
        services.AddTransient<SettingsViewModel>(serviceProvider => 
        {
            var localizationService = serviceProvider.GetRequiredService<ILocalizationService>();
            var packManager = serviceProvider.GetRequiredService<IPackManagerService>();
            var downloadService = serviceProvider.GetRequiredService<IPackDownloadService>();
            var updateService = serviceProvider.GetService<IPackUpdateBackgroundService>(); // Optional
            var analyticsService = serviceProvider.GetRequiredService<IAnalyticsService>();
            var logger = serviceProvider.GetRequiredService<ILogger<SettingsViewModel>>();
            return new SettingsViewModel(localizationService, packManager, downloadService, updateService, analyticsService, logger);
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
        
        return services;
    }
}