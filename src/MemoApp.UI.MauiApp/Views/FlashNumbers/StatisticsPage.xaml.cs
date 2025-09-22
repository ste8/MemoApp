using MemoApp.UI.MauiApp.ViewModels.FlashNumbers;

namespace MemoApp.UI.MauiApp.Views.FlashNumbers;

public partial class StatisticsPage : ContentPage
{
    public StatisticsPage(StatisticsViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}