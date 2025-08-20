using System.Collections.ObjectModel;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MemoApp.Core.Packs;
using MemoApp.Core.Services.Packs;
using MemoApp.Localization.Services;
using MemoApp.UI.MauiApp.Services;
using Microsoft.Extensions.Logging;

namespace MemoApp.UI.MauiApp.ViewModels;

public partial class PackDownloadViewModel : BaseViewModel
{
    private readonly IPackDownloadService _downloadService;
    private readonly IPackManagerService _packManager;
    private readonly MauiPackService _mauiPackService;
    private readonly ILocalizationService _localizationService;
    private readonly ILogger<PackDownloadViewModel> _logger;
    private CancellationTokenSource? _downloadCancellation;

    [ObservableProperty]
    private ObservableCollection<PackItemViewModel> packs = new();

    [ObservableProperty]
    private bool isRefreshing;

    [ObservableProperty]
    private bool isDownloading;

    [ObservableProperty]
    private string? statusMessage;

    [ObservableProperty]
    private bool hasNoPacks;

    public ICommand RefreshCommand { get; }
    public ICommand DownloadPackCommand { get; }
    public ICommand CancelDownloadCommand { get; }
    public ICommand DeletePackCommand { get; }

    public PackDownloadViewModel(
        IPackDownloadService downloadService,
        IPackManagerService packManager,
        MauiPackService mauiPackService,
        ILocalizationService localizationService,
        ILogger<PackDownloadViewModel> logger)
    {
        _downloadService = downloadService;
        _packManager = packManager;
        _mauiPackService = mauiPackService;
        _localizationService = localizationService;
        _logger = logger;

        Title = _localizationService.GetString("PackDownload_Title");

        RefreshCommand = new AsyncRelayCommand(RefreshPacksAsync);
        DownloadPackCommand = new AsyncRelayCommand<PackItemViewModel>(DownloadPackAsync);
        CancelDownloadCommand = new RelayCommand(CancelDownload);
        DeletePackCommand = new AsyncRelayCommand<PackItemViewModel>(DeletePackAsync);
    }

    public async Task InitializeAsync()
    {
        await RefreshPacksAsync();
        
        // Log application data directory for debugging
        _logger.LogInformation("Application data directory: {DataDir}", FileSystem.Current.AppDataDirectory);
    }

    private async Task RefreshPacksAsync()
    {
        try
        {
            IsRefreshing = true;
            StatusMessage = null;
            HasNoPacks = false;

            // Network call should be properly async - no need for Task.Run
            var index = await _downloadService.GetAvailablePacksAsync(forceRefresh: true);
            
            if (index == null)
            {
                StatusMessage = _localizationService.GetString("PackDownload_NetworkErrorMessage");
                HasNoPacks = true;
                return;
            }

            Packs.Clear();

            foreach (var pack in index.Packs)
            {
                var packItem = new PackItemViewModel(pack, _localizationService);
                Packs.Add(packItem);
            }

            HasNoPacks = Packs.Count == 0;
            
            if (HasNoPacks)
            {
                StatusMessage = _localizationService.GetString("PackDownload_NoPacksMessage");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to refresh packs");
            StatusMessage = _localizationService.GetString("PackDownload_ErrorMessage");
            HasNoPacks = true;
        }
        finally
        {
            IsRefreshing = false;
        }
    }

    private async Task DownloadPackAsync(PackItemViewModel? packItem)
    {
        if (packItem == null || IsDownloading)
            return;

        try
        {
            IsDownloading = true;
            packItem.IsDownloading = true;
            _downloadCancellation = new CancellationTokenSource();

            var progress = new Progress<DownloadProgress>(p =>
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    packItem.DownloadProgress = p.ProgressPercentage;
                    packItem.ProgressText = GetProgressText(p);
                    
                    if (p.State == DownloadState.Failed)
                    {
                        packItem.IsDownloading = false;
                        StatusMessage = p.ErrorMessage ?? _localizationService.GetString("PackDownload_ErrorMessage");
                    }
                });
            });

            DownloadProgress result;
            
            if (packItem.Pack.HasUpdate)
            {
                result = await Task.Run(() => 
                    _downloadService.UpdatePackAsync(packItem.Pack, progress, _downloadCancellation.Token)
                        .ContinueWith(t => t.Result ? 
                            new DownloadProgress { State = DownloadState.Completed } : 
                            new DownloadProgress { State = DownloadState.Failed }));
            }
            else
            {
                result = await _downloadService.DownloadPackAsync(
                    packItem.Pack, progress, _downloadCancellation.Token);
            }

            if (result.State == DownloadState.Completed)
            {
                // Log pack location for debugging
                var packPath = await _packManager.GetPackDirectoryAsync(packItem.Pack.Id);
                _logger.LogInformation("Pack {PackId} downloaded successfully to: {PackPath}", packItem.Pack.Id, packPath);
                
                // Generate thumbnails after successful download
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    packItem.ProgressText = _localizationService.GetString("PackDownload_GeneratingThumbnailsLabel");
                });

                await _mauiPackService.GenerateThumbnailsForPackAsync(packItem.Pack.Id, null);

                MainThread.BeginInvokeOnMainThread(() =>
                {
                    packItem.Pack.IsDownloaded = true;
                    packItem.Pack.HasUpdate = false;
                    packItem.IsDownloading = false;
                    StatusMessage = _localizationService.GetString("PackDownload_SuccessMessage");
                });

                // Refresh to update UI state
                await RefreshPacksAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to download pack {PackId}", packItem.Pack.Id);
            StatusMessage = _localizationService.GetString("PackDownload_ErrorMessage");
        }
        finally
        {
            IsDownloading = false;
            if (packItem != null)
            {
                packItem.IsDownloading = false;
            }
            _downloadCancellation?.Dispose();
            _downloadCancellation = null;
        }
    }

    private void CancelDownload()
    {
        _downloadCancellation?.Cancel();
    }

    private async Task DeletePackAsync(PackItemViewModel? packItem)
    {
        if (packItem == null || !packItem.Pack.IsDownloaded)
            return;

        var confirm = await Application.Current!.MainPage!.DisplayAlert(
            _localizationService.GetString("Settings_DeletePack"),
            _localizationService.GetString("Settings_DeletePackConfirmation"),
            _localizationService.GetString("Common_Delete"),
            _localizationService.GetString("Common_Cancel"));

        if (!confirm)
            return;

        try
        {
            var success = await _downloadService.DeletePackAsync(packItem.Pack.Id);
            
            if (success)
            {
                await RefreshPacksAsync();
            }
            else
            {
                StatusMessage = _localizationService.GetString("PackDownload_ErrorMessage");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete pack {PackId}", packItem.Pack.Id);
            StatusMessage = _localizationService.GetString("PackDownload_ErrorMessage");
        }
    }

    private string GetProgressText(DownloadProgress progress)
    {
        return progress.State switch
        {
            DownloadState.Downloading => 
                $"{_localizationService.GetString("PackDownload_ProgressLabel")} {progress.ProgressPercentage:F0}%",
            DownloadState.Extracting => 
                _localizationService.GetString("PackDownload_ExtractingLabel"),
            DownloadState.GeneratingThumbnails => 
                _localizationService.GetString("PackDownload_GeneratingThumbnailsLabel"),
            _ => string.Empty
        };
    }
}

public partial class PackItemViewModel : ObservableObject
{
    private readonly ILocalizationService _localizationService;

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

    public bool CanDownload => !Pack.IsDownloaded || Pack.HasUpdate;
    public bool ShowUpdateBadge => Pack.IsDownloaded && Pack.HasUpdate;

    public PackItemViewModel(Pack pack, ILocalizationService localizationService)
    {
        Pack = pack;
        _localizationService = localizationService;
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
}