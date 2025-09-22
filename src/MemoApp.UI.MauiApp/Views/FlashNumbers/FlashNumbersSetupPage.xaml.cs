using MemoApp.UI.MauiApp.ViewModels.FlashNumbers;

namespace MemoApp.UI.MauiApp.Views.FlashNumbers;

public partial class FlashNumbersSetupPage : ContentPage
{
	public FlashNumbersSetupPage(FlashNumbersSetupViewModel viewModel)
	{
		InitializeComponent();
		BindingContext = viewModel;
	}
}