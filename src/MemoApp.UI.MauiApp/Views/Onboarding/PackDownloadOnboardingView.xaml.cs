using MemoApp.UI.MauiApp.ViewModels.Onboarding;

namespace MemoApp.UI.MauiApp.Views.Onboarding;

public partial class PackDownloadOnboardingView : ContentView
{
    private readonly PackDownloadOnboardingViewModel _viewModel;
    private bool _isInitialized = false;

    public PackDownloadOnboardingView(PackDownloadOnboardingViewModel viewModel)
    {
        System.Diagnostics.Debug.WriteLine("PackDownloadOnboardingView constructor called");
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
        System.Diagnostics.Debug.WriteLine("PackDownloadOnboardingView constructor completed");
    }

    /// <summary>
    /// Initialize the ViewModel when the view becomes visible for the first time
    /// </summary>
    protected override async void OnParentSet()
    {
        base.OnParentSet();
        
        // Initialize the ViewModel only once when the view is first added to parent
        // This prevents multiple initializations and ensures proper loading
        if (Parent != null && _viewModel != null && !_isInitialized)
        {
            _isInitialized = true;
            await _viewModel.InitializeAsync();
        }
    }
}