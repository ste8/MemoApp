using MemoApp.UI.MauiApp.ViewModels;

namespace MemoApp.UI.MauiApp.Views;

public partial class MainPage : ContentPage
{
	private bool _firstAppearance = true;

	public MainPage(MainViewModel viewModel)
	{
		InitializeComponent();
		BindingContext = viewModel;
	}

	protected override async void OnAppearing()
	{
		base.OnAppearing();
		
		if (_firstAppearance)
		{
			_firstAppearance = false;
		}
	}
}
