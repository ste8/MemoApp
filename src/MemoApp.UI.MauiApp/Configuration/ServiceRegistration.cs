using MemoApp.Core.Services.Analytics;
using MemoApp.Core.Services.ErrorMonitoring;
using MemoApp.Core.Services.Sharing;
using MemoApp.Core.MajorSystem.Services;
using MemoApp.Core.MajorSystem.Models;
using MemoApp.Core.BigNumber;
using MemoApp.Core.Services.Packs;
using MemoApp.Localization.Services;
using MemoApp.UI.MauiApp.Services;
using MemoApp.UI.MauiApp.Services.Analytics;
using MemoApp.UI.MauiApp.Services.ErrorMonitoring;
using MemoApp.UI.MauiApp.Services.Freemium;
using MemoApp.UI.MauiApp.Services.Onboarding;
using MemoApp.UI.MauiApp.Services.Sharing;
using MemoApp.UI.MauiApp.Services.ErrorHandling;
using MemoApp.UI.MauiApp.ViewModels;
using MemoApp.UI.MauiApp.ViewModels.Associations;
using MemoApp.UI.MauiApp.ViewModels.BigNumber;
using MemoApp.UI.MauiApp.ViewModels.Components;
using MemoApp.UI.MauiApp.ViewModels.FlashNumbers;
using MemoApp.UI.MauiApp.Views;
using MemoApp.UI.MauiApp.Views.Associations;
using MemoApp.UI.MauiApp.Views.BigNumber;
using MemoApp.UI.MauiApp.Views.FlashNumbers;
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

        // Centralized Exception Handler
        services.AddSingleton<IExceptionHandler, ExceptionHandler>();
        
        // App Initialization Service
        services.AddSingleton<IAppInitializationService, AppInitializationService>();
        
        // App Info Service for version information
        services.AddSingleton<IAppInfoService, AppInfoService>();
        
        // User notification service for consistent error feedback
        services.AddSingleton<IUserNotificationService, UserNotificationService>();
        
        // Messaging services
        services.AddSingleton<IMessenger>(WeakReferenceMessenger.Default);
        
        // Share service for sharing exercise results
        services.AddSingleton<IShareService, MauiShareService>();
        
        // First-time tooltip service
        services.AddSingleton<IFirstTimeTooltipService, FirstTimeTooltipService>();

        // Feedback service for user feedback collection
        services.AddTransient<IFeedbackService, FeedbackService>();

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
        
        // Exercise count service (for ad frequency tracking)
        services.AddSingleton<IExerciseCountService, ExerciseCountService>();
        
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
        services.AddSingleton<IDigitLetterMappingService, DigitLetterMappingService>();
        services.AddSingleton<ITokenDefaultsService>(serviceProvider =>
        {
            var logger = serviceProvider.GetRequiredService<ILogger<TokenDefaultsService>>();
            var packManager = serviceProvider.GetRequiredService<IPackManagerService>();
            return new TokenDefaultsService(logger, packManager);
        });
        
        // Big Number services
        services.AddSingleton<IBigNumberService, BigNumberService>();
        services.AddSingleton<IDigitComparisonService, DigitComparisonService>();
        
        // Onboarding service
        services.AddSingleton<IMajorSystemOnboardingService, MajorSystemOnboardingService>();
        
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
        services.AddSingleton<IImageResizingService, ImageResizingService>();

        // UI measurement services
        services.AddSingleton<ITextMeasurementService, TextMeasurementService>();
        services.AddSingleton<IContainerSizeService, ContainerSizeService>();
        services.AddSingleton<IBigNumberSettingsService, BigNumberSettingsService>();

        // Flash Numbers performance service
        services.AddSingleton<Services.FlashNumbers.IFlashNumbersPerformanceService, Services.FlashNumbers.FlashNumbersPerformanceService>();

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

        // Association composer service (depends on pack associations, custom associations, and token defaults services)
        services.AddSingleton<IAssociationComposerService>(serviceProvider =>
        {
            var packAssociationsService = serviceProvider.GetRequiredService<IPackAssociationsService>();
            var customAssociationsService = serviceProvider.GetRequiredService<ICustomAssociationsService>();
            var tokenDefaultsService = serviceProvider.GetRequiredService<ITokenDefaultsService>();
            var logger = serviceProvider.GetRequiredService<ILogger<AssociationComposerService>>();
            return new AssociationComposerService(packAssociationsService, customAssociationsService, tokenDefaultsService, logger);
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
        
        services.AddSingleton<FlashNumbersSetupViewModel>(serviceProvider =>
        {
            var localizationService = serviceProvider.GetRequiredService<ILocalizationService>();
            var userNotificationService = serviceProvider.GetRequiredService<IUserNotificationService>();
            var logger = serviceProvider.GetRequiredService<ILogger<FlashNumbersSetupViewModel>>();
            return new FlashNumbersSetupViewModel(localizationService, userNotificationService, logger);
        });
        
        // Transient ViewModels (new instance per navigation)
        services.AddTransient<FlashNumbersViewModel>(serviceProvider =>
        {
            var localizationService = serviceProvider.GetRequiredService<ILocalizationService>();
            var analyticsService = serviceProvider.GetRequiredService<IAnalyticsService>();
            var shareService = serviceProvider.GetRequiredService<IShareService>();
            var exerciseCountService = serviceProvider.GetRequiredService<IExerciseCountService>();
            var associationComposerService = serviceProvider.GetRequiredService<IAssociationComposerService>();
            var exceptionHandler = serviceProvider.GetRequiredService<IExceptionHandler>();
            var interstitialAdManager = serviceProvider.GetService<IInterstitialAdManager>(); // Optional
            return new FlashNumbersViewModel(localizationService, analyticsService, shareService, exerciseCountService, associationComposerService, exceptionHandler, interstitialAdManager);
        });
        
        services.AddTransient<StatisticsViewModel>();
        services.AddTransient<HelpViewModel>(serviceProvider =>
        {
            var mappingService = serviceProvider.GetRequiredService<IDigitLetterMappingService>();
            var localizationService = serviceProvider.GetRequiredService<ILocalizationService>();
            return new HelpViewModel(mappingService, localizationService);
        });
        
        services.AddTransient<AssociationsViewModel>(serviceProvider =>
        {
            var categoryService = serviceProvider.GetRequiredService<IMajorSystemCategoryService>();
            var packAssociationsService = serviceProvider.GetRequiredService<IPackAssociationsService>();
            var tokenDefaultsService = serviceProvider.GetRequiredService<ITokenDefaultsService>();
            var customAssociationsService = serviceProvider.GetRequiredService<ICustomAssociationsService>();
            var localizationService = serviceProvider.GetRequiredService<ILocalizationService>();
            var messenger = serviceProvider.GetRequiredService<IMessenger>();
            var imageCacheService = serviceProvider.GetRequiredService<IImageCacheService>();
            var analyticsService = serviceProvider.GetRequiredService<IAnalyticsService>();
            var userNotificationService = serviceProvider.GetRequiredService<IUserNotificationService>();
            var associationComposerService = serviceProvider.GetRequiredService<IAssociationComposerService>();
            var logger = serviceProvider.GetRequiredService<ILogger<AssociationsViewModel>>();
            var configuration = serviceProvider.GetRequiredService<IConfiguration>();
            var exceptionHandler = serviceProvider.GetRequiredService<IExceptionHandler>();
            return new AssociationsViewModel(categoryService, packAssociationsService, tokenDefaultsService, customAssociationsService, localizationService, messenger, imageCacheService, analyticsService, userNotificationService, associationComposerService, logger, configuration, exceptionHandler);
        });
        
        services.AddTransient<PackDownloadViewModel>(serviceProvider =>
        {
            var downloadService = serviceProvider.GetRequiredService<IPackDownloadService>();
            var packManager = serviceProvider.GetRequiredService<IPackManagerService>();
            var mauiPackService = serviceProvider.GetRequiredService<MauiPackService>();
            var localizationService = serviceProvider.GetRequiredService<ILocalizationService>();
            var analyticsService = serviceProvider.GetRequiredService<IAnalyticsService>();
            var messenger = serviceProvider.GetRequiredService<IMessenger>();
            var logger = serviceProvider.GetRequiredService<ILogger<PackDownloadViewModel>>();
            var exceptionHandler = serviceProvider.GetRequiredService<IExceptionHandler>();
            return new PackDownloadViewModel(downloadService, packManager, mauiPackService, localizationService, analyticsService, messenger, logger, exceptionHandler);
        });
        
        services.AddTransient<BigNumberViewModel>(serviceProvider =>
        {
            var gameService = serviceProvider.GetRequiredService<IBigNumberService>();
            var settingsService = serviceProvider.GetRequiredService<IBigNumberSettingsService>();
            var localizationService = serviceProvider.GetRequiredService<ILocalizationService>();
            var shareService = serviceProvider.GetRequiredService<IShareService>();
            var textMeasurementService = serviceProvider.GetRequiredService<ITextMeasurementService>();
            var containerSizeService = serviceProvider.GetRequiredService<IContainerSizeService>();
            var analyticsService = serviceProvider.GetRequiredService<IAnalyticsService>();
            var exerciseCountService = serviceProvider.GetRequiredService<IExerciseCountService>();
            var userNotificationService = serviceProvider.GetRequiredService<IUserNotificationService>();
            var digitComparisonService = serviceProvider.GetRequiredService<IDigitComparisonService>();
            var associationComposerService = serviceProvider.GetRequiredService<IAssociationComposerService>();
            var exceptionHandler = serviceProvider.GetRequiredService<IExceptionHandler>();
            var interstitialAdManager = serviceProvider.GetService<IInterstitialAdManager>(); // Optional
            return new BigNumberViewModel(gameService, settingsService, localizationService, shareService, textMeasurementService, containerSizeService, analyticsService, exerciseCountService, userNotificationService, digitComparisonService, associationComposerService, exceptionHandler, interstitialAdManager);
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

        services.AddTransient<FeedbackViewModel>(serviceProvider =>
        {
            var feedbackService = serviceProvider.GetRequiredService<IFeedbackService>();
            var localizationService = serviceProvider.GetRequiredService<ILocalizationService>();
            var logger = serviceProvider.GetRequiredService<ILogger<FeedbackViewModel>>();
            return new FeedbackViewModel(feedbackService, localizationService, logger);
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
        
        // Onboarding ViewModels
        services.AddTransient<ViewModels.Onboarding.OnboardingNavigationViewModel>();
        services.AddTransient<ViewModels.Onboarding.PersonalizationViewModel>();
        services.AddTransient<ViewModels.Onboarding.WelcomeChallengeViewModel>(serviceProvider =>
        {
            var localizationService = serviceProvider.GetRequiredService<ILocalizationService>();
            var logger = serviceProvider.GetRequiredService<ILogger<ViewModels.Onboarding.WelcomeChallengeViewModel>>();
            return new ViewModels.Onboarding.WelcomeChallengeViewModel(localizationService, logger);
        });
        services.AddTransient<ViewModels.Onboarding.ExampleViewModel>(serviceProvider =>
        {
            var onboardingService = serviceProvider.GetRequiredService<IMajorSystemOnboardingService>();
            var localizationService = serviceProvider.GetRequiredService<ILocalizationService>();
            var logger = serviceProvider.GetRequiredService<ILogger<ViewModels.Onboarding.ExampleViewModel>>();
            return new ViewModels.Onboarding.ExampleViewModel(onboardingService, localizationService, logger);
        });
        services.AddTransient<ViewModels.Onboarding.MajorSystemExampleViewModel>(serviceProvider =>
        {
            var majorSystemService = serviceProvider.GetRequiredService<IMajorSystemOnboardingService>();
            var localizationService = serviceProvider.GetRequiredService<ILocalizationService>();
            var logger = serviceProvider.GetRequiredService<ILogger<ViewModels.Onboarding.MajorSystemExampleViewModel>>();
            return new ViewModels.Onboarding.MajorSystemExampleViewModel(majorSystemService, localizationService, logger);
        });
        services.AddTransient<ViewModels.Onboarding.InteractivePracticeViewModel>(serviceProvider =>
        {
            var onboardingService = serviceProvider.GetRequiredService<IMajorSystemOnboardingService>();
            var localizationService = serviceProvider.GetRequiredService<ILocalizationService>();
            var logger = serviceProvider.GetRequiredService<ILogger<ViewModels.Onboarding.InteractivePracticeViewModel>>();
            return new ViewModels.Onboarding.InteractivePracticeViewModel(onboardingService, localizationService, logger);
        });
        services.AddTransient<ViewModels.Onboarding.PackDownloadOnboardingViewModel>(serviceProvider =>
        {
            var downloadService = serviceProvider.GetRequiredService<IPackDownloadService>();
            var packManager = serviceProvider.GetRequiredService<IPackManagerService>();
            var mauiPackService = serviceProvider.GetRequiredService<MauiPackService>();
            var localizationService = serviceProvider.GetRequiredService<ILocalizationService>();
            var analyticsService = serviceProvider.GetRequiredService<IAnalyticsService>();
            var logger = serviceProvider.GetRequiredService<ILogger<ViewModels.Onboarding.PackDownloadOnboardingViewModel>>();
            return new ViewModels.Onboarding.PackDownloadOnboardingViewModel(downloadService, packManager, mauiPackService, localizationService, analyticsService, logger);
        });

        // Component ViewModels
        services.AddTransient<ShareButtonViewModel>();

        return services;
    }

    /// <summary>
    /// Registers Views/Pages for dependency injection
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <returns>The service collection for method chaining</returns>
    public static IServiceCollection AddViews(this IServiceCollection services)
    {
        // Register Shell (main navigation container)
        services.AddSingleton<AppShell>();
        
        // Register pages
        services.AddSingleton<Views.TestPage>();
        services.AddSingleton<Views.MainPage>();
        services.AddSingleton<FlashNumbersSetupPage>();
        services.AddTransient<FlashNumbersPage>();
        services.AddTransient<StatisticsPage>();
        services.AddTransient<Views.HelpPage>();
        services.AddTransient<Views.SettingsPage>();
        services.AddTransient<AssociationsPage>();
        services.AddTransient<Views.PackDownloadPage>();
        services.AddTransient<Views.BigNumber.BigNumberPage>(serviceProvider =>
        {
            var viewModel = serviceProvider.GetRequiredService<BigNumberViewModel>();
            return new Views.BigNumber.BigNumberPage(viewModel, serviceProvider);
        });
        services.AddTransient<TokenDetailPage>();
        services.AddTransient<AddCustomAssociationPage>();
        services.AddTransient<Views.ImageViewerPage>();
        services.AddTransient<Views.FeedbackPage>();
        
        // Onboarding Views
        services.AddTransient<Views.Onboarding.OnboardingNavigationPage>();
        services.AddTransient<Views.Onboarding.WelcomeView>();
        services.AddTransient<Views.Onboarding.ChallengeView>();
        services.AddTransient<Views.Onboarding.WelcomeChallengeView>();
        services.AddTransient<Views.Onboarding.MajorSystemIntroView>();
        services.AddTransient<Views.Onboarding.ExampleView>();
        services.AddTransient<Views.Onboarding.MajorSystemExampleView>();
        services.AddTransient<Views.Onboarding.InteractivePracticeView>();
        services.AddTransient<Views.Onboarding.PackDownloadOnboardingView>();
        services.AddTransient<Views.Onboarding.PersonalizationView>();
        
        return services;
    }
}