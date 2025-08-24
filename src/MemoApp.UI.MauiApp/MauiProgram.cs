using Microsoft.Extensions.Logging;
using MemoApp.UI.MauiApp.ViewModels;
using MemoApp.UI.MauiApp.Views;
using MemoApp.Localization.Extensions;
using MemoApp.UI.MauiApp.Services;
using MemoApp.UI.MauiApp.Services.Analytics;
using MemoApp.Localization.Services;
using MemoApp.Core.NumberMemorization;
using MemoApp.Core.Services.Packs;
using MemoApp.Core.Services.Analytics;
using MemoApp.Core.MajorSystem.Services;
using MemoApp.UI.MauiApp.Converters;
using CommunityToolkit.Mvvm.Messaging;

namespace MemoApp.UI.MauiApp;

public static class MauiProgram
{
	public static Microsoft.Maui.Hosting.MauiApp CreateMauiApp()
	{
		var builder = Microsoft.Maui.Hosting.MauiApp.CreateBuilder();
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

		// Load configuration
		var configuration = builder.Configuration;
		var packsIndexUrl = configuration["PackManagement:PacksIndexUrl"] ?? "https://github.com/ste8/MemoApp-Packs/releases/download/packs-manifest/packs.json";
		var cacheMinutes = int.Parse(configuration["PackManagement:CacheDurationMinutes"] ?? "30");

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

		// Load saved preferences before the app starts
		var localizationService = app.Services.GetRequiredService<ILocalizationService>();
		Task.Run(async () =>
		{
			try
			{
				await localizationService.LoadSavedLanguageAsync();
				await localizationService.LoadSavedNumberFormatAsync();
			}
			catch (Exception ex)
			{
				System.Diagnostics.Debug.WriteLine($"Failed to load saved preferences at startup: {ex.Message}");
			}
		}).Wait();
		
		// Initialize analytics tracking
		Task.Run(async () =>
		{
			try
			{
				var analyticsService = app.Services.GetRequiredService<IAnalyticsService>();
				await analyticsService.TrackAppStartAsync();
			}
			catch (Exception ex)
			{
				System.Diagnostics.Debug.WriteLine($"Failed to track app start: {ex.Message}");
			}
		});

		return app;
	}
}
