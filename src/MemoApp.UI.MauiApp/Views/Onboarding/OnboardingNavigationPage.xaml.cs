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
        
        // Get all step indicators
        _indicators.Clear();
        _indicators.Add(PhaseIndicator0);
        _indicators.Add(PhaseIndicator1);
        _indicators.Add(PhaseIndicator2);
        
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
        if (_indicators == null || _indicators.Count == 0)
            return;
            
        var primaryColor = (Color)Application.Current.Resources["Primary"];
        var grayColor = (Color)(Application.Current.Resources.TryGetValue("Gray300", out var gray) ? gray : Colors.Gray);
        var warningColor = (Color)(Application.Current.Resources.TryGetValue("Orange400", out var orange) ? orange : Colors.Orange);
        
        // Map 4 steps to 4 visual indicators:
        // Step 1 (Learn): Steps 0-1 (Welcome+Challenge, Major System)
        // Step 2 (Practice): Step 2 (Interactive Practice)
        // Step 3 (Content): Step 3 (Pack Download) - FINAL STEP
        var currentStep = _viewModel.CurrentPosition switch
        {
            0 or 1 => 0, // Learn step
            2 => 1,      // Practice step
            3 => 2,      // Content step (FINAL)
            _ => 0
        };
        
        // Update step indicators with special handling for content step
        for (int i = 0; i < _indicators.Count; i++)
        {
            if (_indicators[i] != null)
            {
                if (i == 2 && currentStep >= 2) // Content step - use warning color when active
                {
                    _indicators[i].BackgroundColor = currentStep == 2 ? warningColor : primaryColor;
                }
                else
                {
                    _indicators[i].BackgroundColor = i <= currentStep ? primaryColor : grayColor;
                }
            }
        }
        
        // Update step labels colors with special handling for content step
        var labelElements = new[] { PhaseLabel0, PhaseLabel1, PhaseLabel2 };
        
        for (int i = 0; i < labelElements.Length; i++)
        {
            if (labelElements[i] != null)
            {
                if (i == 2 && currentStep >= 2) // Content step - use warning color when active
                {
                    labelElements[i].TextColor = currentStep == 2 ? warningColor : primaryColor;
                }
                else
                {
                    labelElements[i].TextColor = i <= currentStep ? primaryColor : grayColor;
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