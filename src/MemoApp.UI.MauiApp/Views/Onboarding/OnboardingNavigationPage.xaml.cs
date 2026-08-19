using MemoApp.UI.MauiApp.ViewModels.Onboarding;

namespace MemoApp.UI.MauiApp.Views.Onboarding;

public partial class OnboardingNavigationPage : ContentPage
{
    private readonly OnboardingNavigationViewModel _viewModel;
    private readonly List<View> _onboardingViews = new();
    private readonly List<Border> _tutorialStepIndicators = new();
    private readonly List<Border> _setupStepIndicators = new();

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

        // Get phase-based step indicators
        _tutorialStepIndicators.Clear();
        _tutorialStepIndicators.Add(TutorialStep0);
        _tutorialStepIndicators.Add(TutorialStep1);
        _tutorialStepIndicators.Add(TutorialStep2);
        _tutorialStepIndicators.Add(TutorialStep3);
        _tutorialStepIndicators.Add(TutorialStep4);

        _setupStepIndicators.Clear();
        _setupStepIndicators.Add(SetupStep0);
        _setupStepIndicators.Add(SetupStep1);
        _setupStepIndicators.Add(SetupStep2);

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
        if (_tutorialStepIndicators.Count == 0 || _setupStepIndicators.Count == 0)
            return;

        var primaryColor = GetColorResource("Primary", Colors.Blue);
        var grayColor = GetColorResource("Gray300", Colors.Gray);
        var successColor = GetColorResource("Success", Colors.Green);

        var currentPosition = _viewModel.CurrentPosition;
        var isInTutorialPhase = OnboardingPhaseConfig.IsInTutorialPhase(currentPosition);
        var isInSetupPhase = OnboardingPhaseConfig.IsInSetupPhase(currentPosition);

        // Update Tutorial Phase Indicators (positions 0-3)
        for (int i = 0; i < _tutorialStepIndicators.Count; i++)
        {
            if (_tutorialStepIndicators[i] != null)
            {
                if (isInTutorialPhase)
                {
                    // Currently in tutorial phase
                    if (i < currentPosition)
                    {
                        _tutorialStepIndicators[i].BackgroundColor = successColor; // Completed
                    }
                    else if (i == currentPosition)
                    {
                        _tutorialStepIndicators[i].BackgroundColor = primaryColor; // Current
                    }
                    else
                    {
                        _tutorialStepIndicators[i].BackgroundColor = grayColor; // Future
                    }
                }
                else
                {
                    // Tutorial phase completed - all green
                    _tutorialStepIndicators[i].BackgroundColor = successColor;
                }
            }
        }

        // Update Setup Phase Indicators (positions 4-6)
        for (int i = 0; i < _setupStepIndicators.Count; i++)
        {
            if (_setupStepIndicators[i] != null)
            {
                var setupPosition = i + OnboardingPhaseConfig.TUTORIAL_PHASE_STEP_COUNT; // Convert to absolute position

                if (isInSetupPhase)
                {
                    // Currently in setup phase
                    if (setupPosition < currentPosition)
                    {
                        _setupStepIndicators[i].BackgroundColor = successColor; // Completed
                    }
                    else if (setupPosition == currentPosition)
                    {
                        _setupStepIndicators[i].BackgroundColor = primaryColor; // Current
                    }
                    else
                    {
                        _setupStepIndicators[i].BackgroundColor = grayColor; // Future
                    }
                }
                else
                {
                    // Still in tutorial phase - all gray
                    _setupStepIndicators[i].BackgroundColor = grayColor;
                }
            }
        }
    }

    private Color GetColorResource(string key, Color fallback)
    {
        return Application.Current.Resources.TryGetValue(key, out var color) && color is Color c ? c : fallback;
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
    }
}