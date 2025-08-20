using MemoApp.Core.Services.Packs;
using MemoApp.Localization.Services;
using MemoApp.UI.MauiApp.Services;
using MemoApp.UI.MauiApp.Views;

namespace MemoApp.UI.MauiApp;

public partial class App : Application
{
	private readonly IPackManagerService _packManager;
	private readonly ILocalizationService _localizationService;
	private readonly IPackUpdateBackgroundService _updateService;
	
	public App(IPackManagerService packManager, ILocalizationService localizationService, IPackUpdateBackgroundService updateService)
	{
		InitializeComponent();
		_packManager = packManager;
		_localizationService = localizationService;
		_updateService = updateService;
	}

	protected override Window CreateWindow(IActivationState? activationState)
	{
		return new Window(new AppShell());
	}
	
	protected override async void OnStart()
	{
		base.OnStart();
		
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

	public async Task CheckFirstLaunchAsync()
	{
		// Check if any packs are downloaded
		var hasPacksDownloaded = await _packManager.HasAnyPackDownloadedAsync();
		
		if (!hasPacksDownloaded)
		{
			var mainPage = MainPage ?? Windows?.FirstOrDefault()?.Page;
			if (mainPage != null)
			{
				await mainPage.DisplayAlert(
					_localizationService.GetString("FirstLaunch_WelcomeTitle"),
					_localizationService.GetString("FirstLaunch_DownloadPackMessage"),
					_localizationService.GetString("Common_OK"));
				
				await Shell.Current.GoToAsync(nameof(PackDownloadPage));
			}
		}
	}
}