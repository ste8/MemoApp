using MemoApp.UI.MauiApp.Views;

namespace MemoApp.UI.MauiApp;

public partial class AppShell : Shell
{
	public AppShell()
	{
		InitializeComponent();
		
		// Register routes for navigation
		// These routes enable programmatic navigation using Shell.Current.GoToAsync()
		Routing.RegisterRoute("training", typeof(TrainingSessionPage));
		Routing.RegisterRoute("statistics", typeof(StatisticsPage));
	}
}
