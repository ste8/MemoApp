using Microsoft.Extensions.Logging;
using MemoApp.UI.MauiApp.ViewModels;
using MemoApp.UI.MauiApp.Views;
using MemoApp.Localization.Extensions;
using MemoApp.UI.MauiApp.Services;
using MemoApp.Localization.Services;
using MemoApp.Core.NumberMemorization;
using MemoApp.Core.Services.Packs;
using MemoApp.UI.MauiApp.Converters;

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


		// Register ViewModels and Views for dependency injection
		builder.Services.AddSingleton<Views.TestPage>();
		
		builder.Services.AddSingleton<MainViewModel>();
		builder.Services.AddSingleton<Views.MainPage>();
		
		builder.Services.AddTransient<TrainingSessionViewModel>();
		builder.Services.AddTransient<Views.TrainingSessionPage>();
		
		builder.Services.AddTransient<StatisticsViewModel>();
		builder.Services.AddTransient<Views.StatisticsPage>();
		
		builder.Services.AddTransient<HelpViewModel>();
		builder.Services.AddTransient<Views.HelpPage>();
		
		builder.Services.AddTransient<SettingsViewModel>();
		builder.Services.AddTransient<Views.SettingsPage>();
		
		// Register Number Memorization services
		builder.Services.AddSingleton<INumberMemorizationService, NumberMemorizationService>();
		builder.Services.AddSingleton<INumberMemorizationSettingsService, NumberMemorizationSettingsService>();
		builder.Services.AddTransient<NumberMemorizationViewModel>();
		builder.Services.AddTransient<Views.NumberMemorizationPage>();
		
		// Register Pack Management ViewModels and Views
		builder.Services.AddTransient<PackDownloadViewModel>();
		builder.Services.AddTransient<Views.PackDownloadPage>();

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

		return app;
	}
}
