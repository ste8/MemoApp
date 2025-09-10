using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MemoApp.Core.Packs;
using MemoApp.Localization.Services;

namespace MemoApp.UI.MauiApp.ViewModels;

public enum PackCardContext
{
    Management,     // Full features (download, delete, update)
    Onboarding,     // Simplified (download only)
    Selection       // Display only
}

public partial class PackCardViewModel : ObservableObject
{
    private readonly ILocalizationService _localizationService;
    private readonly PackCardContext _context;

    [ObservableProperty]
    private Pack pack;

    [ObservableProperty]
    private bool isDownloading;

    [ObservableProperty]
    private double downloadProgress;

    [ObservableProperty]
    private string? progressText;

    public string DisplayName => GetDisplayName();
    public string DisplayDescription => GetDisplayDescription();
    public string SizeText => _localizationService.GetString("PackDownload_Size").Replace("{0}", Pack.GetFormattedFileSize());
    public string VersionText => _localizationService.GetString("PackDownload_Version").Replace("{0}", Pack.Version);
    public string AuthorText => _localizationService.GetString("PackDownload_Author").Replace("{0}", Pack.Author);
    
    public string ButtonText
    {
        get
        {
            if (Pack.IsDownloaded && Pack.HasUpdate)
                return _localizationService.GetString("PackDownload_UpdateButton");
            if (Pack.IsDownloaded)
                return _localizationService.GetString("PackDownload_DownloadedLabel");
            return _localizationService.GetString("PackDownload_DownloadButton");
        }
    }

    // Context-based visibility properties
    public bool ShowDescription => true; // Always show description - users need to understand what they're downloading
    public bool ShowPackInfo => _context != PackCardContext.Selection; // Show pack info in Management and Onboarding contexts
    public bool ShowDeleteButton => _context == PackCardContext.Management && Pack.IsDownloaded;
    public bool CanDownload => !Pack.IsDownloaded || Pack.HasUpdate;
    public bool ShowUpdateBadge => Pack.IsDownloaded && Pack.HasUpdate;
    public bool ShowDownloadButton => !IsDownloading && (!Pack.IsDownloaded || Pack.HasUpdate);
    
    // Safe command execution checks
    public bool CanExecuteDownload => DownloadCommand != null && CanDownload && !IsDownloading;
    public bool CanExecuteDelete => DeleteCommand != null && Pack.IsDownloaded && !IsDownloading;

    // Commands (to be bound by parent ViewModels)
    public IRelayCommand<PackCardViewModel>? DownloadCommand { get; set; }
    public IRelayCommand<PackCardViewModel>? DeleteCommand { get; set; }

    public PackCardViewModel(Pack pack, ILocalizationService localizationService, PackCardContext context = PackCardContext.Management)
    {
        Pack = pack;
        _localizationService = localizationService;
        _context = context;
        
        // Explicitly initialize download state
        IsDownloading = false;
        DownloadProgress = 0.0;
        ProgressText = null;
    }

    private string GetDisplayName()
    {
        var currentLanguage = _localizationService.GetCurrentLanguage();
        var useNative = currentLanguage == "it" && Pack.LanguageCode == "it";
        return Pack.GetDisplayName(useNative);
    }

    private string GetDisplayDescription()
    {
        var currentLanguage = _localizationService.GetCurrentLanguage();
        var useNative = currentLanguage == "it" && Pack.LanguageCode == "it";
        return Pack.GetDisplayDescription(useNative);
    }

    public void RefreshState()
    {
        OnPropertyChanged(nameof(DisplayName));
        OnPropertyChanged(nameof(DisplayDescription));
        OnPropertyChanged(nameof(SizeText));
        OnPropertyChanged(nameof(VersionText));
        OnPropertyChanged(nameof(AuthorText));
        OnPropertyChanged(nameof(ButtonText));
        OnPropertyChanged(nameof(ShowDeleteButton));
        OnPropertyChanged(nameof(CanDownload));
        OnPropertyChanged(nameof(ShowUpdateBadge));
        OnPropertyChanged(nameof(ShowDownloadButton));
        OnPropertyChanged(nameof(CanExecuteDownload));
        OnPropertyChanged(nameof(CanExecuteDelete));
    }
}