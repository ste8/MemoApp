using Microsoft.Extensions.Logging;
using MemoApp.UI.MauiApp.ViewModels;
using MemoApp.UI.MauiApp.Views;
using MemoApp.Localization.Extensions;
using MemoApp.UI.MauiApp.Services;
using MemoApp.Localization.Services;

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

		// Register localization services with MAUI-specific implementation
		builder.Services.AddLocalization<MauiLocalizationService>();

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

#if DEBUG
		builder.Logging.AddDebug();
#endif

		var app = builder.Build();

		// Initialize the LocalizationResourceManager with the DI service
		app.Services.InitializeLocalization();

		return app;
	}
}
