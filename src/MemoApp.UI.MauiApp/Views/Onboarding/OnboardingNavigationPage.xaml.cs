using MemoApp.UI.MauiApp.ViewModels.Onboarding;

namespace MemoApp.UI.MauiApp.Views.Onboarding;

public partial class OnboardingNavigationPage : ContentPage
{
    private readonly OnboardingNavigationViewModel _viewModel;
    private readonly List<View> _onboardingViews = new();
    private readonly List<Border> _stepIndicators = new();

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

        // Get all 5 step indicators
        _stepIndicators.Clear();
        _stepIndicators.Add(StepIndicator0);
        _stepIndicators.Add(StepIndicator1);
        _stepIndicators.Add(StepIndicator2);
        _stepIndicators.Add(StepIndicator3);
        _stepIndicators.Add(StepIndicator4);

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
                Text = $"Loading step {_viewModel.CurrentPosition + 1} of {_onboardingViews.Count}...",
                FontSize = 16,
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center
            });
        }
    }

    private void UpdateIndicators()
    {
        // Ensure indicators are initialized before updating
        if (_stepIndicators == null || _stepIndicators.Count == 0)
            return;

        var primaryColor = (Color)Application.Current.Resources["Primary"];
        var grayColor = (Color)(Application.Current.Resources.TryGetValue("Gray300", out var gray) ? gray : Colors.Gray);
        var successColor = (Color)(Application.Current.Resources.TryGetValue("Green400", out var green) ? green : Colors.Green);

        // Linear mapping: CurrentPosition directly maps to step indicators (0-4)
        var currentStep = _viewModel.CurrentPosition;

        // Update step indicators with linear progression
        for (int i = 0; i < _stepIndicators.Count; i++)
        {
            if (_stepIndicators[i] != null)
            {
                if (i < currentStep)
                {
                    // Completed steps - use success color
                    _stepIndicators[i].BackgroundColor = successColor;
                }
                else if (i == currentStep)
                {
                    // Current step - use primary color
                    _stepIndicators[i].BackgroundColor = primaryColor;
                }
                else
                {
                    // Future steps - use gray
                    _stepIndicators[i].BackgroundColor = grayColor;
                }
            }
        }

        // Update step label colors with same logic
        var labelElements = new[] { StepLabel0, StepLabel1, StepLabel2, StepLabel3, StepLabel4 };

        for (int i = 0; i < labelElements.Length; i++)
        {
            if (labelElements[i] != null)
            {
                if (i < currentStep)
                {
                    // Completed steps
                    labelElements[i].TextColor = successColor;
                }
                else if (i == currentStep)
                {
                    // Current step
                    labelElements[i].TextColor = primaryColor;
                }
                else
                {
                    // Future steps
                    labelElements[i].TextColor = grayColor;
                }
            }
        }
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
    }
}