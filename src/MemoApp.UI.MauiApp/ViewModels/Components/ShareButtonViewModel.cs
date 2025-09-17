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
        UpdateButtonText();
        UpdateStyling();
        UpdateAccessibility();
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
        if (DisplayStyle.ToLowerInvariant() == "icononly")
        {
            // Icon only - use share-variant Material Design icon
            ButtonText = "\uf72d"; // Material Design Icons share-variant
        }
        else
        {
            // Icon + text on all platforms for better clarity and recognition
            ButtonText = $"{_localizationService.GetString("Share_ButtonIcon")} {_localizationService.GetString("Share_ButtonText")}";
        }
    }

    private void UpdateStyling()
    {
        // Base styling
        switch (DisplayStyle.ToLowerInvariant())
        {
            case "icononly":
                // Duolingo-inspired compact design
                FontSize = DeviceInfo.Idiom == DeviceIdiom.Phone ? 20 :
                          DeviceInfo.Idiom == DeviceIdiom.Tablet ? 22 : 24;
                Padding = DeviceInfo.Idiom == DeviceIdiom.Phone ? new Thickness(12) :
                         DeviceInfo.Idiom == DeviceIdiom.Tablet ? new Thickness(14) :
                         new Thickness(10);
                CornerRadius = DeviceInfo.Idiom == DeviceIdiom.Phone ? 12 : 10;
                WidthRequest = DeviceInfo.Idiom == DeviceIdiom.Phone ? 48 :
                              DeviceInfo.Idiom == DeviceIdiom.Tablet ? 52 : 44;
                HeightRequest = WidthRequest; // Square aspect ratio
                break;

            case "compact":
                FontSize = 14;
                Padding = new Thickness(8, 4);
                CornerRadius = 6;
                WidthRequest = DeviceInfo.Idiom == DeviceIdiom.Phone ? 110 : 120; // Wider for icon+text
                HeightRequest = 32;
                break;

            case "prominent":
                FontSize = DeviceInfo.Idiom == DeviceIdiom.Phone ? 16 : 18; // Match CelebrationNewChallengeButton
                Padding = new Thickness(20, 12); // Match CelebrationNewChallengeButton
                CornerRadius = 16; // Match CelebrationNewChallengeButton
                WidthRequest = DeviceInfo.Idiom == DeviceIdiom.Phone ? 140 : 160; // Wider for icon+text
                HeightRequest = DeviceInfo.Idiom == DeviceIdiom.Phone ? 56 : DeviceInfo.Idiom == DeviceIdiom.Tablet ? 64 : 56; // Match CelebrationNewChallengeButton
                break;

            case "normal":
            default:
                FontSize = DeviceInfo.Idiom == DeviceIdiom.Phone ? 16 : 18; // Better match with verification buttons
                Padding = new Thickness(12, 8);
                CornerRadius = 14; // Match verification buttons style
                WidthRequest = DeviceInfo.Idiom == DeviceIdiom.Phone ? 120 : 140; // Wider for icon+text
                HeightRequest = DeviceInfo.Idiom == DeviceIdiom.Phone ? 48 : DeviceInfo.Idiom == DeviceIdiom.Tablet ? 56 : 64; // Better match with verification buttons
                break;
        }

        // Enhanced color scheme for better visual hierarchy
        if (DisplayStyle.ToLowerInvariant() == "icononly")
        {
            // Slightly more subtle for secondary action
            BackgroundColor = Color.FromArgb("#1991DB"); // Slightly darker Twitter blue
            TextColor = Colors.White;
            ShadowColor = Color.FromArgb("#1478B8");
        }
        else
        {
            // Consistent Twitter-blue color for all contexts (better brand recognition and UX)
            BackgroundColor = Color.FromArgb("#1DA1F2"); // Twitter Blue
            TextColor = Colors.White;
            ShadowColor = Color.FromArgb("#0d8bd9"); // Darker blue for shadow depth
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
            if (DisplayStyle.ToLowerInvariant() == "icononly")
            {
                // More descriptive for icon-only buttons
                AccessibilityDescription = _localizationService.GetString("Share_IconOnlyDescription");
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