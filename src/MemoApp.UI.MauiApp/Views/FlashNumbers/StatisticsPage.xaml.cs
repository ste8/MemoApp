using MemoApp.UI.MauiApp.ViewModels.FlashNumbers;
using MemoApp.UI.MauiApp.ViewModels.Components;

namespace MemoApp.UI.MauiApp.Views.FlashNumbers;

public partial class StatisticsPage : ContentPage
{
    private readonly StatisticsViewModel _viewModel;
    private ShareButtonViewModel? _shareButtonVM;

    public StatisticsPage(StatisticsViewModel viewModel, IServiceProvider serviceProvider)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;

        // Initialize ShareButton components with their ViewModels
        InitializeShareButtonComponents(serviceProvider, viewModel);
    }

    /// <summary>
    /// Initializes the ShareButton components with their ViewModels
    /// </summary>
    /// <param name="serviceProvider">The service provider for dependency injection</param>
    /// <param name="statisticsViewModel">The main Statistics ViewModel for data binding</param>
    private void InitializeShareButtonComponents(IServiceProvider serviceProvider, StatisticsViewModel statisticsViewModel)
    {
        try
        {
            // Get ShareButtonViewModel instance from the service provider
            _shareButtonVM = serviceProvider.GetService<ShareButtonViewModel>();

            if (_shareButtonVM != null)
            {
                // Set the ViewModel for both ShareButton components (mobile and desktop)
                if (StatisticsShareButton != null)
                {
                    StatisticsShareButton.BindingContext = _shareButtonVM;
                }

                if (StatisticsShareButtonDesktop != null)
                {
                    StatisticsShareButtonDesktop.BindingContext = _shareButtonVM;
                }

                // Subscribe to StatisticsViewModel changes to update ShareButtonViewModel
                statisticsViewModel.PropertyChanged += OnStatisticsViewModelPropertyChanged;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to initialize ShareButton components: {ex.Message}");
        }
    }

    /// <summary>
    /// Handles property changes from StatisticsViewModel to update share data
    /// </summary>
    private void OnStatisticsViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (_shareButtonVM == null || sender is not StatisticsViewModel viewModel)
            return;

        // Update share data when SessionShareData is available
        if (e.PropertyName == nameof(StatisticsViewModel.SessionShareData) && viewModel.SessionShareData != null)
        {
            _shareButtonVM.UpdateShareData(viewModel.SessionShareData);
            _shareButtonVM.UpdateDisplayStyle("IconOnly");
            _shareButtonVM.UpdateContext("statistics");
        }
    }
}