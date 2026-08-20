using Android.App;
using Android.Runtime;
using Firebase;

namespace MemoApp.UI.MauiApp;

[Application]
public class MainApplication : MauiApplication
{
	public MainApplication(IntPtr handle, JniHandleOwnership ownership)
		: base(handle, ownership)
	{
	}

	public override void OnCreate()
	{
		base.OnCreate();
		
		// Initialize Firebase for Android
		try
		{
			FirebaseApp.InitializeApp(this);
		}
		catch (System.Exception ex)
		{
			// Debug.WriteLine is compiled out of Release, so record the failure for
			// AppInitializationService to report once error monitoring is available.
			MemoApp.UI.MauiApp.Services.Analytics.FirebaseInitializationStatus.RecordFailure(ex);
			System.Diagnostics.Debug.WriteLine($"Failed to initialize Firebase: {ex.Message}");
			// The app continues; analytics falls back to the local implementation.
		}
	}

	protected override Microsoft.Maui.Hosting.MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
}
