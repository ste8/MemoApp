using MemoApp.Core.Services.ErrorMonitoring;
using MemoApp.UI.MauiApp.Services;
using MemoApp.UI.MauiApp.Views.Onboarding;

namespace MemoApp.UI.MauiApp;

public partial class App : Application
{
	private readonly IPackUpdateBackgroundService _updateService;
	private readonly IErrorMonitoringService? _errorService;
	private readonly IAppInitializationService _initializationService;
	private readonly IServiceProvider _serviceProvider;
	private readonly DateTime _appStartTime = DateTime.UtcNow;
	
	public App(IPackUpdateBackgroundService updateService, IAppInitializationService initializationService, IServiceProvider serviceProvider, IErrorMonitoringService? errorService = null)
	{
		InitializeComponent();
		_updateService = updateService;
		_initializationService = initializationService;
		_serviceProvider = serviceProvider;
		_errorService = errorService;
	}

	protected override Window CreateWindow(IActivationState? activationState)
	{
		// Check if onboarding has been completed
		var onboardingCompleted = Preferences.Get("OnboardingCompleted", "false") == "true";
		
		// Force onboarding for testing - uncomment to always show onboarding
		onboardingCompleted = false;
		
		if (onboardingCompleted)
		{
			// User has completed onboarding, show main app
			var appShell = _serviceProvider.GetRequiredService<AppShell>();
			return new Window(appShell);
		}
		else
		{
			// Show onboarding flow for new users
			var onboardingPage = _serviceProvider.GetRequiredService<OnboardingNavigationPage>();
			return new Window(onboardingPage);
		}
	}
	
	protected override async void OnStart()
	{
		base.OnStart();
		
		// Track app startup performance
		var startupTime = (DateTime.UtcNow - _appStartTime).TotalMilliseconds;
		if (_errorService != null && startupTime > 10000) // > 10 seconds
		{
			await _errorService.LogPerformanceIssueAsync(
				PerformanceIssueTypes.AppStartupSlow,
				new Dictionary<string, object>
				{
					["startup_time_ms"] = startupTime,
					["cold_start"] = true,
					["platform"] = DeviceInfo.Platform.ToString()
				});
		}
		
		// Start background update service
		await _updateService.StartAsync();
	}
	
	protected override async void OnSleep()
	{
		base.OnSleep();
		
		// Stop background update service when app goes to background
		await _updateService.StopAsync();
	}
	
	protected override async void OnResume()
	{
		base.OnResume();
		
		// Restart background update service when app resumes
		await _updateService.StartAsync();
	}


}