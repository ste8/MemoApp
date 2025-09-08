using System.Collections.ObjectModel;
using System.Net;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using MemoApp.Core.Packs;
using MemoApp.Core.Services.Analytics;
using MemoApp.Core.Services.Packs;
using MemoApp.Localization.Services;
using MemoApp.UI.MauiApp.Services;
using MemoApp.UI.MauiApp.Messages;
using Microsoft.Extensions.Logging;

namespace MemoApp.UI.MauiApp.ViewModels;

public partial class PackDownloadViewModel : BaseViewModel
{
    private readonly IPackDownloadService _downloadService;
    private readonly IPackManagerService _packManager;
    private readonly MauiPackService _mauiPackService;
    private readonly ILocalizationService _localizationService;
    private readonly IAnalyticsService _analyticsService;
    private readonly IMessenger _messenger;
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

    [ObservableProperty]
    private string updateCheckMessage = string.Empty;

    [ObservableProperty]
    private bool showUpdateMessage;

    public ICommand RefreshCommand { get; }
    public ICommand DownloadPackCommand { get; }
    public ICommand CancelDownloadCommand { get; }
    public ICommand DeletePackCommand { get; }

    public PackDownloadViewModel(
        IPackDownloadService downloadService,
        IPackManagerService packManager,
        MauiPackService mauiPackService,
        ILocalizationService localizationService,
        IAnalyticsService analyticsService,
        IMessenger messenger,
        ILogger<PackDownloadViewModel> logger)
    {
        _downloadService = downloadService;
        _packManager = packManager;
        _mauiPackService = mauiPackService;
        _localizationService = localizationService;
        _analyticsService = analyticsService;
        _messenger = messenger;
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
            ShowUpdateMessage = false;

            // Clear cache and get fresh data
            _downloadService.ClearCache();
            var index = await _downloadService.GetAvailablePacksAsync(forceRefresh: true);
            
            if (index == null)
            {
                StatusMessage = _localizationService.GetString("PackDownload_NetworkErrorMessage");
                HasNoPacks = true;
                return;
            }

            Packs.Clear();
            int updateCount = 0;

            foreach (var pack in index.Packs)
            {
                var packItem = new PackItemViewModel(pack, _localizationService);
                Packs.Add(packItem);
                
                if (pack.HasUpdate)
                {
                    updateCount++;
                }
            }

            HasNoPacks = Packs.Count == 0;
            
            if (HasNoPacks)
            {
                StatusMessage = _localizationService.GetString("PackDownload_NoPacksMessage");
            }
            else
            {
                // Show update status message
                if (updateCount > 0)
                {
                    UpdateCheckMessage = updateCount == 1 
                        ? _localizationService.GetString("Update_SinglePackAvailable")
                        : _localizationService.GetString("Update_MultiplePacksAvailable").Replace("{0}", updateCount.ToString());
                }
                else
                {
                    UpdateCheckMessage = _localizationService.GetString("PackDownload_AllPacksUpToDate");
                }
                
                ShowUpdateMessage = true;
                
                // Hide message after 5 seconds
                _ = Task.Run(async () =>
                {
                    await Task.Delay(5000);
                    await MainThread.InvokeOnMainThreadAsync(() => ShowUpdateMessage = false);
                });
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

        var downloadStartTime = DateTime.UtcNow;
        var isUpdate = packItem.Pack.HasUpdate;
        var previousVersion = packItem.Pack.Version; // Capture version before update
        
        try
        {
            // Track download start analytics
            await _analyticsService.TrackEventAsync(
                isUpdate ? AnalyticsEvents.PackUpdated : AnalyticsEvents.PackDownloaded,
                new Dictionary<string, object>
                {
                    [AnalyticsParameters.PackName] = packItem.Pack.InternationalName,
                    [AnalyticsParameters.PackLanguage] = packItem.Pack.LanguageCode,
                    [AnalyticsParameters.PackSizeMB] = Math.Round(packItem.Pack.FileSize / (1024.0 * 1024.0), 2),
                    [AnalyticsParameters.UpdateAvailable] = isUpdate
                });

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
                        packItem.DownloadProgress = 0;
                        
                        // Show specific error message based on error type
                        StatusMessage = GetErrorMessage(p);
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
                var downloadEndTime = DateTime.UtcNow;
                var downloadDuration = (downloadEndTime - downloadStartTime).TotalSeconds;

                // Track successful download/update analytics
                await _analyticsService.TrackEventAsync(
                    isUpdate ? AnalyticsEvents.PackUpdated : AnalyticsEvents.PackDownloaded,
                    new Dictionary<string, object>
                    {
                        [AnalyticsParameters.PackName] = packItem.Pack.InternationalName,
                        [AnalyticsParameters.PackLanguage] = packItem.Pack.LanguageCode,
                        [AnalyticsParameters.PackSizeMB] = Math.Round(packItem.Pack.FileSize / (1024.0 * 1024.0), 2),
                        [AnalyticsParameters.DownloadDurationSeconds] = (int)downloadDuration,
                        [AnalyticsParameters.DownloadSuccess] = true,
                        [AnalyticsParameters.UpdateAvailable] = isUpdate
                    });

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

                // Send appropriate message to notify other ViewModels
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    try
                    {
                        if (isUpdate)
                        {
                            var updateMessage = PackUpdatedMessage.FromPack(packItem.Pack, previousVersion, downloadDuration);
                            _messenger.Send(updateMessage);
                            _logger.LogInformation("Sent PackUpdatedMessage for pack {PackId} (v{PreviousVersion} → v{NewVersion})", 
                                packItem.Pack.Id, previousVersion, packItem.Pack.Version);
                        }
                        else
                        {
                            var downloadMessage = PackDownloadedMessage.FromPack(packItem.Pack, downloadDuration);
                            _messenger.Send(downloadMessage);
                            _logger.LogInformation("Sent PackDownloadedMessage for pack {PackId} (v{Version})", 
                                packItem.Pack.Id, packItem.Pack.Version);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to send pack message for {PackId}", packItem.Pack.Id);
                    }
                });

                // Refresh to update UI state
                await RefreshPacksAsync();
            }
            else
            {
                // Track failed download analytics
                var downloadEndTime = DateTime.UtcNow;
                var downloadDuration = (downloadEndTime - downloadStartTime).TotalSeconds;

                await _analyticsService.TrackEventAsync(
                    isUpdate ? AnalyticsEvents.PackUpdated : AnalyticsEvents.PackDownloaded,
                    new Dictionary<string, object>
                    {
                        [AnalyticsParameters.PackName] = packItem.Pack.InternationalName,
                        [AnalyticsParameters.PackLanguage] = packItem.Pack.LanguageCode,
                        [AnalyticsParameters.DownloadDurationSeconds] = (int)downloadDuration,
                        [AnalyticsParameters.DownloadSuccess] = false,
                        [AnalyticsParameters.UpdateAvailable] = isUpdate
                    });
            }
        }
        catch (InsufficientStorageException ex)
        {
            var requiredMB = Math.Ceiling(ex.RequiredBytes / (1024.0 * 1024.0));
            StatusMessage = _localizationService.GetString("PackDownload_InsufficientStorageError")
                .Replace("{0}", requiredMB.ToString());
            _logger.LogError(ex, "Insufficient storage for pack {PackId}: Required {RequiredMB:F1}MB", packItem.Pack.Id, requiredMB);
        }
        catch (HttpRequestException ex)
        {
            StatusMessage = _localizationService.GetString("PackDownload_NetworkError");
            _logger.LogError(ex, "Network error downloading pack {PackId}", packItem.Pack.Id);
        }
        catch (PackExtractionException ex)
        {
            StatusMessage = _localizationService.GetString("PackDownload_ExtractionError");
            _logger.LogError(ex, "Extraction failed for pack {PackId}", packItem.Pack.Id);
        }
        catch (PackValidationException ex)
        {
            StatusMessage = _localizationService.GetString("PackDownload_ValidationError");
            _logger.LogError(ex, "Validation failed for pack {PackId}", packItem.Pack.Id);
        }
        catch (Exception ex)
        {
            StatusMessage = _localizationService.GetString("PackDownload_ErrorMessage");
            _logger.LogError(ex, "Unexpected error downloading pack {PackId}", packItem.Pack.Id);
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
            // Check if this will be the last pack before deletion
            var downloadedPacksCount = Packs.Count(p => p.Pack.IsDownloaded);
            var wasLastPack = downloadedPacksCount <= 1;
            
            var success = await _downloadService.DeletePackAsync(packItem.Pack.Id);
            
            // Track pack deletion analytics
            await _analyticsService.TrackEventAsync(AnalyticsEvents.PackDeleted, new Dictionary<string, object>
            {
                [AnalyticsParameters.PackName] = packItem.Pack.InternationalName,
                [AnalyticsParameters.PackLanguage] = packItem.Pack.LanguageCode,
                [AnalyticsParameters.Success] = success
            });
            
            if (success)
            {
                // Send delete message to notify other ViewModels
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    try
                    {
                        var deleteMessage = PackDeletedMessage.FromPack(packItem.Pack, wasLastPack);
                        _messenger.Send(deleteMessage);
                        _logger.LogInformation("Sent PackDeletedMessage for pack {PackId} (was last pack: {WasLastPack})", 
                            packItem.Pack.Id, wasLastPack);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to send pack delete message for {PackId}", packItem.Pack.Id);
                    }
                });
                
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

    private string GetErrorMessage(DownloadProgress progress)
    {
        return progress.ErrorType switch
        {
            DownloadErrorType.InsufficientStorage => 
                _localizationService.GetString("PackDownload_InsufficientStorageError")
                    .Replace("{0}", Math.Ceiling((progress.RequiredStorageBytes ?? 0) / (1024.0 * 1024.0)).ToString()),
            DownloadErrorType.NetworkError => 
                _localizationService.GetString("PackDownload_NetworkError"),
            DownloadErrorType.ExtractionError => 
                _localizationService.GetString("PackDownload_ExtractionError"),
            DownloadErrorType.ValidationError => 
                _localizationService.GetString("PackDownload_ValidationError"),
            _ => progress.ErrorMessage ?? _localizationService.GetString("PackDownload_ErrorMessage")
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
    public bool ShowDownloadButton => !Pack.IsDownloaded || Pack.HasUpdate;

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