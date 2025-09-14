using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MemoApp.Core.Models;
using MemoApp.Core.Services.Analytics;
using MemoApp.Core.Services.Sharing;
using MemoApp.Localization.Services;

namespace MemoApp.UI.MauiApp.ViewModels.Components;

public partial class ShareButtonViewModel : ObservableObject
{
    private readonly IShareService _shareService;
    private readonly ILocalizationService _localizationService;
    private readonly IAnalyticsService _analyticsService;

    [ObservableProperty]
    private ShareableResult? _shareData;

    [ObservableProperty]
    private string _displayStyle = "Normal";

    [ObservableProperty]
    private string _context = "default";

    [ObservableProperty]
    private bool _isShareEnabled = true;

    [ObservableProperty]
    private bool _isVisible = true;

    [ObservableProperty]
    private bool _isSharing = false;

    [ObservableProperty]
    private string _buttonText = "📤 Share";

    [ObservableProperty]
    private Color _backgroundColor = Colors.Green;

    [ObservableProperty]
    private Color _textColor = Colors.White;

    [ObservableProperty]
    private Color _shadowColor = Colors.Gray;

    [ObservableProperty]
    private double _fontSize = 16;

    [ObservableProperty]
    private Thickness _padding = new(12, 8);

    [ObservableProperty]
    private double _cornerRadius = 8;

    [ObservableProperty]
    private double _widthRequest = -1;

    [ObservableProperty]
    private double _heightRequest = -1;

    [ObservableProperty]
    private double _buttonOpacity = 1.0;

    [ObservableProperty]
    private string _accessibilityDescription = "";

    public ShareButtonViewModel(
        IShareService shareService,
        ILocalizationService localizationService,
        IAnalyticsService analyticsService)
    {
        _shareService = shareService ?? throw new ArgumentNullException(nameof(shareService));
        _localizationService = localizationService ?? throw new ArgumentNullException(nameof(localizationService));
        _analyticsService = analyticsService ?? throw new ArgumentNullException(nameof(analyticsService));

        InitializeComponent();
    }

    private void InitializeComponent()
    {
        UpdateButtonText();
        UpdateStyling();
        UpdateAccessibility();
    }

    [RelayCommand]
    private async Task Share()
    {
        if (ShareData == null || IsSharing)
            return;

        try
        {
            IsSharing = true;
            UpdateButtonAppearanceForSharing();

            // Track analytics
            await _analyticsService.TrackEventAsync("share_button_clicked", new Dictionary<string, object>
            {
                ["context"] = Context,
                ["display_style"] = DisplayStyle,
                ["exercise_type"] = ShareData.GetType().Name
            });

            // Generate localized share content
            var shareContent = ShareData.GenerateShareContent(key => _localizationService.GetString(key));

            // Perform share
            await _shareService.ShareTextAsync(shareContent.Title, shareContent.Message);

            // Track successful share
            await _analyticsService.TrackEventAsync("share_completed", new Dictionary<string, object>
            {
                ["context"] = Context,
                ["exercise_type"] = ShareData.GetType().Name
            });
        }
        catch (Exception ex)
        {
            // Track error
            await _analyticsService.TrackEventAsync("share_failed", new Dictionary<string, object>
            {
                ["context"] = Context,
                ["error"] = ex.Message
            });

            System.Diagnostics.Debug.WriteLine($"Share failed: {ex.Message}");
        }
        finally
        {
            IsSharing = false;
            UpdateButtonAppearanceForNormalState();
        }
    }

    public void UpdateShareData(ShareableResult? shareData)
    {
        ShareData = shareData;
        IsShareEnabled = shareData != null;
        UpdateAccessibility();
    }

    public void UpdateDisplayStyle(string displayStyle)
    {
        DisplayStyle = displayStyle;
        UpdateStyling();
    }

    public void UpdateContext(string context)
    {
        Context = context;
        UpdateStyling();
        UpdateAccessibility();
    }

    public void UpdateIsEnabled(bool isEnabled)
    {
        IsShareEnabled = isEnabled;
        ButtonOpacity = isEnabled ? 1.0 : 0.5;
    }

    public void UpdateVisibility(bool isVisible)
    {
        IsVisible = isVisible;
    }

    private void UpdateButtonText()
    {
        // Platform-specific text display
        if (DeviceInfo.Idiom == DeviceIdiom.Phone)
        {
            // Icon-only on mobile for space efficiency
            ButtonText = _localizationService.GetString("Share_ButtonIcon");
        }
        else
        {
            // Icon + text on tablet/desktop
            ButtonText = $"{_localizationService.GetString("Share_ButtonIcon")} {_localizationService.GetString("Share_ButtonText")}";
        }
    }

    private void UpdateStyling()
    {
        // Base styling
        switch (DisplayStyle.ToLowerInvariant())
        {
            case "compact":
                FontSize = 14;
                Padding = new Thickness(8, 4);
                CornerRadius = 6;
                WidthRequest = DeviceInfo.Idiom == DeviceIdiom.Phone ? 44 : 80;
                HeightRequest = 32;
                break;

            case "prominent":
                FontSize = 18;
                Padding = new Thickness(16, 12);
                CornerRadius = 12;
                WidthRequest = DeviceInfo.Idiom == DeviceIdiom.Phone ? 60 : 140;
                HeightRequest = 48;
                break;

            case "normal":
            default:
                FontSize = 16;
                Padding = new Thickness(12, 8);
                CornerRadius = 8;
                WidthRequest = DeviceInfo.Idiom == DeviceIdiom.Phone ? 52 : 120;
                HeightRequest = 40;
                break;
        }

        // Context-specific colors
        switch (Context.ToLowerInvariant())
        {
            case "celebration":
                BackgroundColor = Color.FromArgb("#FFD700"); // Gold
                TextColor = Color.FromArgb("#333333");
                ShadowColor = Color.FromArgb("#FFA500");
                break;

            case "verification":
            case "manual":
                BackgroundColor = Application.Current?.Resources.TryGetValue("Success", out var success) == true
                    ? (Color)success
                    : Color.FromArgb("#28A745"); // Green
                TextColor = Colors.White;
                ShadowColor = Color.FromArgb("#1e7e34");
                break;

            default:
                BackgroundColor = Application.Current?.Resources.TryGetValue("Primary", out var primary) == true
                    ? (Color)primary
                    : Color.FromArgb("#007bff"); // Blue
                TextColor = Colors.White;
                ShadowColor = Color.FromArgb("#0056b3");
                break;
        }
    }

    private void UpdateAccessibility()
    {
        if (ShareData == null)
        {
            AccessibilityDescription = _localizationService.GetString("Share_NotAvailable");
        }
        else
        {
            var exerciseType = ShareData.GetType().Name.Replace("ShareableResult", "").Replace("Result", "");
            AccessibilityDescription = string.Format(
                _localizationService.GetString("Share_AccessibilityDescription"),
                exerciseType,
                Context
            );
        }
    }

    private void UpdateButtonAppearanceForSharing()
    {
        ButtonOpacity = 0.7;
    }

    private void UpdateButtonAppearanceForNormalState()
    {
        ButtonOpacity = IsShareEnabled ? 1.0 : 0.5;
    }
}