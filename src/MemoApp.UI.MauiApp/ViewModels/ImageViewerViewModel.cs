using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;

namespace MemoApp.UI.MauiApp.ViewModels;

/// <summary>
/// ViewModel for the full-screen image viewer
/// </summary>
[QueryProperty(nameof(ImagePath), "imagePath")]
[QueryProperty(nameof(Token), "token")]
[QueryProperty(nameof(Word), "word")]
public partial class ImageViewerViewModel : ObservableObject
{
    private readonly ILogger<ImageViewerViewModel> _logger;

    [ObservableProperty]
    private string _imagePath = string.Empty;

    [ObservableProperty]
    private string _token = string.Empty;

    [ObservableProperty]
    private string _word = string.Empty;

    [ObservableProperty]
    private string _title = string.Empty;

    [ObservableProperty]
    private string _infoText = string.Empty;

    [ObservableProperty]
    private bool _showInfo = true;

    public ImageViewerViewModel(ILogger<ImageViewerViewModel> logger)
    {
        _logger = logger;
    }

    partial void OnTokenChanged(string value)
    {
        UpdateTitle();
    }

    partial void OnWordChanged(string value)
    {
        UpdateTitle();
    }

    private void UpdateTitle()
    {
        if (!string.IsNullOrEmpty(Word))
        {
            Title = Word;
            InfoText = Token;
        }
        else if (!string.IsNullOrEmpty(Token))
        {
            Title = $"Token {Token}";
            InfoText = string.Empty;
        }
        else
        {
            Title = "Image Viewer";
            InfoText = string.Empty;
        }
    }

    [RelayCommand]
    private async Task CloseAsync()
    {
        try
        {
            _logger.LogInformation("Closing image viewer");
            await Shell.Current.GoToAsync("..");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error closing image viewer");
        }
    }

    [RelayCommand]
    private void ToggleInfo()
    {
        ShowInfo = !ShowInfo;
        _logger.LogDebug("Info visibility toggled to {ShowInfo}", ShowInfo);
    }
}