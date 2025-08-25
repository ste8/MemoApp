using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using MemoApp.UI.MauiApp.ViewModels;
using MemoApp.Localization.Extensions;
using MemoApp.UI.MauiApp.Services;
using MemoApp.UI.MauiApp.Services.Analytics;
using MemoApp.UI.MauiApp.Services.ErrorMonitoring;
using MemoApp.Localization.Services;
using MemoApp.Core.NumberMemorization;
using MemoApp.Core.Services.Packs;
using MemoApp.Core.Services.Analytics;
using MemoApp.Core.Services.ErrorMonitoring;
using MemoApp.Core.MajorSystem.Services;
using CommunityToolkit.Mvvm.Messaging;
using System.Linq;

namespace MemoApp.UI.MauiApp;

public static class MauiProgram
{
	public static Microsoft.Maui.Hosting.MauiApp CreateMauiApp()
	{
		var builder = Microsoft.Maui.Hosting.MauiApp.CreateBuilder();
		
		// Load appsettings.json configuration from embedded resource
		var assembly = System.Reflection.Assembly.GetExecutingAssembly();
		using var stream = assembly.GetManifestResourceStream("MemoApp.UI.MauiApp.appsettings.json");
		if (stream != null)
		{
			var configBuilder = new ConfigurationBuilder();
			configBuilder.AddJsonStream(stream);
			var embeddedConfig = configBuilder.Build();
			
			// Add the embedded config to the builder
			foreach (var kvp in embeddedConfig.AsEnumerable())
			{
				if (kvp.Value != null)
				{
					builder.Configuration[kvp.Key] = kvp.Value;
				}
			}
		}
		else
		{
			System.Diagnostics.Debug.WriteLine("Warning: appsettings.json not found as embedded resource");
		}
		
#if DEBUG
		// Also try to load appsettings.Development.json if it exists
		using var devStream = assembly.GetManifestResourceStream("MemoApp.UI.MauiApp.appsettings.Development.json");
		if (devStream != null)
		{
			var devConfigBuilder = new ConfigurationBuilder();
			devConfigBuilder.AddJsonStream(devStream);
			var embeddedDevConfig = devConfigBuilder.Build();
			
			// Add the embedded dev config to the builder (overwrites main config values)
			foreach (var kvp in embeddedDevConfig.AsEnumerable())
			{
				if (kvp.Value != null)
				{
					builder.Configuration[kvp.Key] = kvp.Value;
				}
			}
		}
#endif
		
		builder
			.UseMauiApp<App>()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
				
				// FontAwesome v6 Pro fonts
				fonts.AddFont("fa-solid-900.ttf", "FontAwesomeSolid");
				fonts.AddFont("fa-regular-400.ttf", "FontAwesomeRegular");
				fonts.AddFont("fa-light-300.ttf", "FontAwesomeLight");
				fonts.AddFont("fa-thin-100.ttf", "FontAwesomeThin");
				fonts.AddFont("fa-duotone-900.ttf", "FontAwesomeDuotone");
				fonts.AddFont("fa-brands-400.ttf", "FontAwesomeBrands");
			});

		// Load configuration (now loaded from appsettings.json)
		var configuration = builder.Configuration;
		var packsIndexUrl = configuration["PackManagement:PacksIndexUrl"] ?? "https://github.com/ste8/MemoApp-Packs/releases/download/packs-manifest/packs.json";
		var cacheMinutes = int.Parse(configuration["PackManagement:CacheDurationMinutes"] ?? "30");

		// Register Error Monitoring Services (early in pipeline)
		builder.Services.AddSingleton<IErrorMonitoringService, SentryErrorMonitoringService>();
		builder.Services.AddSingleton<GlobalExceptionHandler>();
		
		// Register App Initialization Service
		builder.Services.AddSingleton<IAppInitializationService, AppInitializationService>();
		
		// Configure Sentry MAUI integration with proper options
		builder.UseSentry(options =>
		{
			var sentryDsn = configuration["Sentry:Dsn"];
			System.Diagnostics.Debug.WriteLine($"Loading Sentry DSN: '{sentryDsn}' (Length: {sentryDsn?.Length ?? 0})");
			
			if (!string.IsNullOrEmpty(sentryDsn))
			{
				options.Dsn = sentryDsn;
				options.Environment = configuration["Sentry:Environment"] ?? "production";
				options.Debug = bool.TryParse(configuration["Sentry:Debug"], out var debug) && debug;
				options.TracesSampleRate = double.TryParse(configuration["Sentry:TracesSampleRate"], out var traceRate) ? traceRate : 0.1;
				
				System.Diagnostics.Debug.WriteLine($"Sentry configured: Environment={options.Environment}, Debug={options.Debug}, TracesSampleRate={options.TracesSampleRate}");
			}
			else
			{
				System.Diagnostics.Debug.WriteLine("Warning: Sentry DSN is null or empty - Sentry will not be initialized");
				// List all configuration keys for debugging
				var allKeys = configuration.AsEnumerable().Select(kvp => $"{kvp.Key}={kvp.Value}").ToArray();
				System.Diagnostics.Debug.WriteLine($"All configuration keys ({allKeys.Length}): {string.Join(", ", allKeys)}");
			}
		});

		// Register HTTP client for pack downloads
		builder.Services.AddHttpClient();
		
		// Register localization services with MAUI-specific implementation
		builder.Services.AddLocalization<MauiLocalizationService>();
		
		// Register text measurement and container size services
		builder.Services.AddSingleton<ITextMeasurementService, TextMeasurementService>();
		builder.Services.AddSingleton<IContainerSizeService, ContainerSizeService>();
		
		// Register Messaging services
		builder.Services.AddSingleton<IMessenger>(WeakReferenceMessenger.Default);
		
		// Register pack management services
		builder.Services.AddSingleton<IThumbnailService, ThumbnailService>();
		builder.Services.AddSingleton<IPackManagerService>(serviceProvider =>
		{
			var logger = serviceProvider.GetRequiredService<ILogger<PackManagerService>>();
			return new PackManagerService(logger, FileSystem.Current.AppDataDirectory);
		});
		builder.Services.AddSingleton<MauiPackService>();
		builder.Services.AddSingleton<IPackDownloadService>(serviceProvider =>
		{
			var httpClient = serviceProvider.GetRequiredService<HttpClient>();
			var packManager = serviceProvider.GetRequiredService<IPackManagerService>();
			var logger = serviceProvider.GetRequiredService<ILogger<PackDownloadService>>();
			return new PackDownloadService(httpClient, packManager, logger, packsIndexUrl, FileSystem.Current.AppDataDirectory, cacheMinutes);
		});
		
		// Register background update service
		builder.Services.AddSingleton<IPackUpdateBackgroundService, PackUpdateBackgroundService>();
		
		// Register Major System services
		builder.Services.AddSingleton<IMajorSystemCategoryService, MajorSystemCategoryService>();
		builder.Services.AddSingleton<ITokenDefaultsService>(serviceProvider =>
		{
			var logger = serviceProvider.GetRequiredService<ILogger<TokenDefaultsService>>();
			var packManager = serviceProvider.GetRequiredService<IPackManagerService>();
			return new TokenDefaultsService(logger, packManager);
		});
		
		// Register Cache services
		builder.Services.AddSingleton<IPackAssociationsCacheService, PackAssociationsCacheService>();
		builder.Services.AddSingleton<IImageCacheService, ImageCacheService>();
		
		// Register Analytics services - platform-specific implementation
#if ANDROID || IOS
		builder.Services.AddSingleton<IAnalyticsService, FirebaseAnalyticsService>();
#else
		builder.Services.AddSingleton<IAnalyticsService, LocalAnalyticsService>();
#endif
		
		// Register Custom Associations services first (required by PackAssociationsService)
		builder.Services.AddSingleton<IImageProcessingService, MauiImageProcessingService>();
		builder.Services.AddSingleton<ICustomImageSourceService, MauiImageSourceService>();
		builder.Services.AddSingleton<ICustomAssociationsService>(serviceProvider =>
		{
			var packManager = serviceProvider.GetRequiredService<IPackManagerService>();
			var imageProcessingService = serviceProvider.GetRequiredService<IImageProcessingService>();
			var tokenDefaultsService = serviceProvider.GetRequiredService<ITokenDefaultsService>();
			var logger = serviceProvider.GetRequiredService<ILogger<CustomAssociationsService>>();
			return new CustomAssociationsService(packManager, imageProcessingService, tokenDefaultsService, logger);
		});
		
		builder.Services.AddSingleton<IPackAssociationsService>(serviceProvider =>
		{
			var packManager = serviceProvider.GetRequiredService<IPackManagerService>();
			var categoryService = serviceProvider.GetRequiredService<IMajorSystemCategoryService>();
			var tokenDefaultsService = serviceProvider.GetRequiredService<ITokenDefaultsService>();
			var customAssociationsService = serviceProvider.GetRequiredService<ICustomAssociationsService>();
			var cacheService = serviceProvider.GetRequiredService<IPackAssociationsCacheService>();
			var logger = serviceProvider.GetRequiredService<ILogger<PackAssociationsService>>();
			return new PackAssociationsService(packManager, categoryService, tokenDefaultsService, customAssociationsService, cacheService, logger, FileSystem.Current.AppDataDirectory);
		});


		// Register ViewModels and Views for dependency injection
		builder.Services.AddSingleton<Views.TestPage>();
		
		builder.Services.AddSingleton<MainViewModel>();
		builder.Services.AddSingleton<Views.MainPage>();
		
		builder.Services.AddTransient<FlashNumbersViewModel>(serviceProvider =>
		{
			var localizationService = serviceProvider.GetRequiredService<ILocalizationService>();
			var analyticsService = serviceProvider.GetRequiredService<IAnalyticsService>();
			return new FlashNumbersViewModel(localizationService, analyticsService);
		});
		builder.Services.AddTransient<Views.FlashNumbersPage>();
		
		builder.Services.AddTransient<StatisticsViewModel>();
		builder.Services.AddTransient<Views.StatisticsPage>();
		
		builder.Services.AddTransient<HelpViewModel>();
		builder.Services.AddTransient<Views.HelpPage>();
		
		builder.Services.AddTransient<SettingsViewModel>(serviceProvider => 
		{
			var localizationService = serviceProvider.GetRequiredService<ILocalizationService>();
			var packManager = serviceProvider.GetRequiredService<IPackManagerService>();
			var downloadService = serviceProvider.GetRequiredService<IPackDownloadService>();
			var updateService = serviceProvider.GetService<IPackUpdateBackgroundService>(); // Optional
			var analyticsService = serviceProvider.GetRequiredService<IAnalyticsService>();
			var logger = serviceProvider.GetRequiredService<ILogger<SettingsViewModel>>();
			return new SettingsViewModel(localizationService, packManager, downloadService, updateService, analyticsService, logger);
		});
		builder.Services.AddTransient<Views.SettingsPage>();
		
		// Register Number Memorization services
		builder.Services.AddSingleton<INumberMemorizationService, NumberMemorizationService>();
		builder.Services.AddSingleton<INumberMemorizationSettingsService, NumberMemorizationSettingsService>();
		builder.Services.AddTransient<NumberMemorizationViewModel>(serviceProvider =>
		{
			var gameService = serviceProvider.GetRequiredService<INumberMemorizationService>();
			var settingsService = serviceProvider.GetRequiredService<INumberMemorizationSettingsService>();
			var localizationService = serviceProvider.GetRequiredService<ILocalizationService>();
			var textMeasurementService = serviceProvider.GetRequiredService<ITextMeasurementService>();
			var containerSizeService = serviceProvider.GetRequiredService<IContainerSizeService>();
			var analyticsService = serviceProvider.GetRequiredService<IAnalyticsService>();
			return new NumberMemorizationViewModel(gameService, settingsService, localizationService, textMeasurementService, containerSizeService, analyticsService);
		});
		builder.Services.AddTransient<Views.NumberMemorizationPage>();
		
		// Register Associations ViewModels and Views
		builder.Services.AddTransient<AssociationsViewModel>(serviceProvider =>
		{
			var categoryService = serviceProvider.GetRequiredService<IMajorSystemCategoryService>();
			var packAssociationsService = serviceProvider.GetRequiredService<IPackAssociationsService>();
			var tokenDefaultsService = serviceProvider.GetRequiredService<ITokenDefaultsService>();
			var localizationService = serviceProvider.GetRequiredService<ILocalizationService>();
			var messenger = serviceProvider.GetRequiredService<IMessenger>();
			var imageCacheService = serviceProvider.GetRequiredService<IImageCacheService>();
			var analyticsService = serviceProvider.GetRequiredService<IAnalyticsService>();
			var logger = serviceProvider.GetRequiredService<ILogger<AssociationsViewModel>>();
			return new AssociationsViewModel(categoryService, packAssociationsService, tokenDefaultsService, localizationService, messenger, imageCacheService, analyticsService, logger);
		});
		builder.Services.AddTransient<Views.AssociationsPage>();
		
		// Register Token Detail ViewModels and Views
		builder.Services.AddTransient<TokenDetailViewModel>();
		builder.Services.AddTransient<Views.TokenDetailPage>();
		
		// Register Image Viewer ViewModels and Views
		builder.Services.AddTransient<ImageViewerViewModel>();
		builder.Services.AddTransient<Views.ImageViewerPage>();
		
		// Register Pack Management ViewModels and Views
		builder.Services.AddTransient<PackDownloadViewModel>(serviceProvider =>
		{
			var downloadService = serviceProvider.GetRequiredService<IPackDownloadService>();
			var packManager = serviceProvider.GetRequiredService<IPackManagerService>();
			var mauiPackService = serviceProvider.GetRequiredService<MauiPackService>();
			var localizationService = serviceProvider.GetRequiredService<ILocalizationService>();
			var analyticsService = serviceProvider.GetRequiredService<IAnalyticsService>();
			var logger = serviceProvider.GetRequiredService<ILogger<PackDownloadViewModel>>();
			return new PackDownloadViewModel(downloadService, packManager, mauiPackService, localizationService, analyticsService, logger);
		});
		builder.Services.AddTransient<Views.PackDownloadPage>();
		
		// Register Add Custom Association ViewModels and Views
		builder.Services.AddTransient<AddCustomAssociationViewModel>();
		builder.Services.AddTransient<Views.AddCustomAssociationPage>();

#if DEBUG
		builder.Logging.AddDebug();
#endif

		var app = builder.Build();

		// Initialize the LocalizationResourceManager with the DI service
		app.Services.InitializeLocalization();

		// Initialize all services using the proper coordination service
		// This replaces the dangerous .Wait() and Task.Run patterns
		var initializationService = app.Services.GetRequiredService<IAppInitializationService>();
		
		// Start initialization asynchronously but don't block app startup
		// The app can start showing UI while services initialize in background
		Task.Run(async () =>
		{
			try
			{
				var initSuccess = await initializationService.InitializeAsync();
				if (initSuccess)
				{
					System.Diagnostics.Debug.WriteLine("All app services initialized successfully");
				}
				else
				{
					System.Diagnostics.Debug.WriteLine($"App initialization failed: {initializationService.CurrentStatus}");
				}
			}
			catch (Exception ex)
			{
				System.Diagnostics.Debug.WriteLine($"Critical error during app initialization: {ex.Message}");
			}
		});

		return app;
	}
}
