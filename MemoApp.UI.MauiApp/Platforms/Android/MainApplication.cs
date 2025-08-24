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
			System.Diagnostics.Debug.WriteLine($"Failed to initialize Firebase: {ex.Message}");
			// Firebase initialization failed, but app should continue
			// Analytics will use fallback behavior
		}
	}

	protected override Microsoft.Maui.Hosting.MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
}
