using Foundation;
using Firebase.Core;

namespace MemoApp.UI.MauiApp;

[Register("AppDelegate")]
public class AppDelegate : MauiUIApplicationDelegate
{
	public override bool FinishedLaunching(UIKit.UIApplication application, NSDictionary launchOptions)
	{
		// Initialize Firebase for iOS
		try
		{
			Firebase.Core.App.Configure();
		}
		catch (System.Exception ex)
		{
			System.Diagnostics.Debug.WriteLine($"Failed to initialize Firebase: {ex.Message}");
			// Firebase initialization failed, but app should continue
			// Analytics will use fallback behavior
		}

		return base.FinishedLaunching(application, launchOptions);
	}

	protected override Microsoft.Maui.Hosting.MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
}
