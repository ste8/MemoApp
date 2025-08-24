using MemoApp.UI.MauiApp.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace MemoApp.UI.MauiApp.Views;

public partial class SettingsPage : ContentPage
{
    private readonly SettingsViewModel _viewModel;

    public SettingsPage(SettingsViewModel viewModel)
    {
        try
        {
            _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
            InitializeComponent();
            BindingContext = _viewModel;
            System.Diagnostics.Debug.WriteLine($"SettingsPage: Created with ViewModel, language: {_viewModel.CurrentLanguageName}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"SettingsPage constructor error: {ex}");
            throw;
        }
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        
        try
        {
            System.Diagnostics.Debug.WriteLine("SettingsPage: OnAppearing started");
            
            // Initialize the ViewModel asynchronously to load the saved language
            await _viewModel.InitializeAsync();
            
            System.Diagnostics.Debug.WriteLine($"SettingsPage: ViewModel initialized with language: {_viewModel.CurrentLanguageName}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"SettingsPage OnAppearing error: {ex}");
        }
    }
}