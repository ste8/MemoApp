using MemoApp.Core.Services.Packs;
using MemoApp.Localization.Services;
using MemoApp.UI.MauiApp.Views;

namespace MemoApp.UI.MauiApp;

public partial class App : Application
{
	private readonly IPackManagerService _packManager;
	private readonly ILocalizationService _localizationService;
	
	public App(IPackManagerService packManager, ILocalizationService localizationService)
	{
		InitializeComponent();
		_packManager = packManager;
		_localizationService = localizationService;
	}

	protected override Window CreateWindow(IActivationState? activationState)
	{
		return new Window(new AppShell());
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