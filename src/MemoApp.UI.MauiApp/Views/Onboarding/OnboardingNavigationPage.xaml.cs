using MemoApp.UI.MauiApp.ViewModels.Onboarding;

namespace MemoApp.UI.MauiApp.Views.Onboarding;

public partial class OnboardingNavigationPage : ContentPage
{
    private readonly OnboardingNavigationViewModel _viewModel;
    private readonly List<View> _onboardingViews = new();
    private readonly List<Frame> _indicators = new();

    public OnboardingNavigationPage(OnboardingNavigationViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
        _viewModel = viewModel;
        
        // Subscribe to position changes
        _viewModel.PropertyChanged += OnViewModelPropertyChanged;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        try
        {
            await InitializeViews();
            ShowCurrentView();
        }
        catch (Exception ex)
        {
            // Fallback - show a simple error message
            ContentArea.Children.Add(new Label 
            { 
                Text = $"Error loading onboarding: {ex.Message}",
                FontSize = 16,
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center
            });
        }
    }

    private async Task InitializeViews()
    {
        await _viewModel.InitializeAsync();
        
        // Get all indicators
        _indicators.Clear();
        _indicators.Add(Indicator0);
        _indicators.Add(Indicator1);
        _indicators.Add(Indicator2);
        _indicators.Add(Indicator3);
        _indicators.Add(Indicator4);
        _indicators.Add(Indicator5);
        _indicators.Add(Indicator6);
        _indicators.Add(Indicator7);
        
        // Store views from ViewModel
        _onboardingViews.Clear();
        _onboardingViews.AddRange(_viewModel.OnboardingPages);
    }

    private void OnViewModelPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(OnboardingNavigationViewModel.CurrentPosition))
        {
            ShowCurrentView();
            UpdateIndicators();
        }
    }

    private void ShowCurrentView()
    {
        ContentArea.Children.Clear();
        
        if (_viewModel.CurrentPosition >= 0 && _viewModel.CurrentPosition < _onboardingViews.Count)
        {
            var currentView = _onboardingViews[_viewModel.CurrentPosition];
            ContentArea.Children.Add(currentView);
        }
        else
        {
            // Fallback content if views aren't loaded
            ContentArea.Children.Add(new Label 
            { 
                Text = $"Loading view {_viewModel.CurrentPosition + 1} of {_onboardingViews.Count}...",
                FontSize = 16,
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center
            });
        }
    }

    private void UpdateIndicators()
    {
        // Ensure indicators are initialized before updating
        if (_indicators == null || _indicators.Count == 0)
            return;
            
        var primaryColor = (Color)Application.Current.Resources["Primary"];
        var grayColor = (Color)(Application.Current.Resources.TryGetValue("Gray400", out var gray) ? gray : Colors.Gray);
        
        for (int i = 0; i < _indicators.Count; i++)
        {
            if (_indicators[i] != null)
            {
                _indicators[i].BackgroundColor = i == _viewModel.CurrentPosition ? primaryColor : grayColor;
            }
        }
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
    }
}