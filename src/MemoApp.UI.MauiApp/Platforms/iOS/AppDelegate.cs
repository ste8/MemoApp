using Foundation;

namespace MemoApp.UI.MauiApp;

[Register("AppDelegate")]
public class AppDelegate : MauiUIApplicationDelegate
{
	public override bool FinishedLaunching(UIKit.UIApplication application, NSDictionary launchOptions)
	{
		var result = base.FinishedLaunching(application, launchOptions);
		InitFirebaseAnalytics();
		return result;
	}

	private static void InitFirebaseAnalytics()
	{
		// Then initialize Firebase for iOS
		try
		{
			// Debug: Check multiple possible locations for the plist file
			var rootPath = Foundation.NSBundle.MainBundle.PathForResource("GoogleService-Info", "plist");
			System.Diagnostics.Debug.WriteLine($"GoogleService-Info.plist at root: {rootPath ?? "NOT FOUND"}");
			
			// Try to configure Firebase - it should find the file automatically if properly bundled
			Firebase.Core.App.Configure();
			System.Diagnostics.Debug.WriteLine("Firebase configured successfully");
		}
		catch (System.Exception ex)
		{
			// Firebase initialization failed, but app should continue.
			// Analytics will use fallback behavior
			System.Diagnostics.Debug.WriteLine($"Failed to initialize Firebase: {ex.Message}");
			System.Diagnostics.Debug.WriteLine($"Exception type: {ex.GetType().Name}");
		}
	}

	protected override Microsoft.Maui.Hosting.MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
}
