using MemoApp.UI.MauiApp.ViewModels;

namespace MemoApp.UI.MauiApp.Views;

public partial class FlashNumbersSetupPage : ContentPage
{
	public FlashNumbersSetupPage(FlashNumbersSetupViewModel viewModel)
	{
		InitializeComponent();
		BindingContext = viewModel;
	}
}