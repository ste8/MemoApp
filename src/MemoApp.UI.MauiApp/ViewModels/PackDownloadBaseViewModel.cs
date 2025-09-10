using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using MemoApp.Core.Packs;
using MemoApp.Core.Services.Analytics;
using MemoApp.Core.Services.Packs;
using MemoApp.Localization.Services;
using MemoApp.UI.MauiApp.Services;
using Microsoft.Extensions.Logging;

namespace MemoApp.UI.MauiApp.ViewModels;

public abstract partial class PackDownloadBaseViewModel : BaseViewModel
{
    protected readonly IPackDownloadService _downloadService;
    protected readonly IPackManagerService _packManager;
    protected readonly MauiPackService _mauiPackService;
    protected readonly ILocalizationService _localizationService;
    protected readonly IAnalyticsService _analyticsService;
    protected readonly ILogger _logger;
    protected CancellationTokenSource? _downloadCancellation;

    [ObservableProperty]
    private bool isDownloading;

    [ObservableProperty]
    private bool hasError;

    [ObservableProperty]
    private string? errorMessage;

    [ObservableProperty]
    private double downloadProgress;

    [ObservableProperty]
    private string? progressText;

    protected PackDownloadBaseViewModel(
        IPackDownloadService downloadService,
        IPackManagerService packManager,
        MauiPackService mauiPackService,
        ILocalizationService localizationService,
        IAnalyticsService analyticsService,
        ILogger logger)
    {
        _downloadService = downloadService;
        _packManager = packManager;
        _mauiPackService = mauiPackService;
        _localizationService = localizationService;
        _analyticsService = analyticsService;
        _logger = logger;
    }

    protected async Task DownloadPackCardAsync(PackCardViewModel? packCard)
    {
        if (packCard == null || IsDownloading)
            return;

        var downloadStartTime = DateTime.UtcNow;
        var isUpdate = packCard.Pack.HasUpdate;
        
        try
        {
            // Track download start analytics with context-specific event
            await _analyticsService.TrackEventAsync(
                GetDownloadStartEventName(isUpdate),
                new Dictionary<string, object>
                {
                    [AnalyticsParameters.PackName] = packCard.Pack.InternationalName,
                    [AnalyticsParameters.PackLanguage] = packCard.Pack.LanguageCode,
                    [AnalyticsParameters.PackSizeMB] = Math.Round(packCard.Pack.FileSize / (1024.0 * 1024.0), 2)
                });

            OnDownloadStarted(packCard);
            
            IsDownloading = true;
            packCard.IsDownloading = true;
            _downloadCancellation = new CancellationTokenSource();

            var progress = new Progress<DownloadProgress>(p =>
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    DownloadProgress = p.ProgressPercentage;
                    packCard.DownloadProgress = p.ProgressPercentage;
                    ProgressText = GetProgressText(p);
                    packCard.ProgressText = ProgressText;
                    
                    if (p.State == DownloadState.Failed)
                    {
                        HandleDownloadError(p);
                    }
                });
            });

            var result = await _downloadService.DownloadPackAsync(
                packCard.Pack, progress, _downloadCancellation.Token);

            if (result.State == DownloadState.Completed)
            {
                var downloadEndTime = DateTime.UtcNow;
                var downloadDuration = (downloadEndTime - downloadStartTime).TotalSeconds;

                // Track successful download analytics with context-specific event
                await _analyticsService.TrackEventAsync(
                    GetDownloadCompletedEventName(isUpdate),
                    new Dictionary<string, object>
                    {
                        [AnalyticsParameters.PackName] = packCard.Pack.InternationalName,
                        [AnalyticsParameters.PackLanguage] = packCard.Pack.LanguageCode,
                        [AnalyticsParameters.PackSizeMB] = Math.Round(packCard.Pack.FileSize / (1024.0 * 1024.0), 2),
                        [AnalyticsParameters.DownloadDurationSeconds] = (int)downloadDuration,
                        [AnalyticsParameters.DownloadSuccess] = true
                    });

                _logger.LogInformation("Pack {PackId} downloaded successfully", packCard.Pack.Id);
                
                // Generate thumbnails after successful download
                ProgressText = _localizationService.GetString("PackDownload_GeneratingThumbnailsLabel");
                await _mauiPackService.GenerateThumbnailsForPackAsync(packCard.Pack.Id, null);

                // Update pack state
                packCard.Pack.IsDownloaded = true;
                packCard.RefreshState();
                
                await OnDownloadCompleted(packCard, downloadDuration);
            }
            else
            {
                // Track failed download analytics
                var downloadEndTime = DateTime.UtcNow;
                var downloadDuration = (downloadEndTime - downloadStartTime).TotalSeconds;

                await _analyticsService.TrackEventAsync(
                    GetDownloadCompletedEventName(isUpdate),
                    new Dictionary<string, object>
                    {
                        [AnalyticsParameters.PackName] = packCard.Pack.InternationalName,
                        [AnalyticsParameters.PackLanguage] = packCard.Pack.LanguageCode,
                        [AnalyticsParameters.DownloadDurationSeconds] = (int)downloadDuration,
                        [AnalyticsParameters.DownloadSuccess] = false,
                        [AnalyticsParameters.ErrorMessage] = result.ErrorMessage ?? "Unknown error"
                    });

                HandleDownloadError(result);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error downloading pack {PackId}", packCard.Pack.Id);
            HasError = true;
            ErrorMessage = GetErrorMessage(ex);
        }
        finally
        {
            IsDownloading = false;
            if (packCard != null)
            {
                packCard.IsDownloading = false;
            }
            _downloadCancellation?.Dispose();
            _downloadCancellation = null;
            
            OnDownloadFinished(packCard);
        }
    }

    // Constants for iOS CollectionView timing workarounds
    private const int COLLECTION_READY_DELAY_MS = 50;
    private const int FALLBACK_BATCH_DELAY_MS = 10;
    private const int FALLBACK_ITEM_DELAY_MS = 5;

    protected async Task UpdatePackCollectionSafelyAsync<T>(ObservableCollection<T> collection, List<T> newItems, string collectionName)
    {
        // Update collection on main thread using bulk replacement to avoid iOS CollectionView issues
        await MainThread.InvokeOnMainThreadAsync(async () =>
        {
            try
            {
                // Small delay to ensure CollectionView is ready
                await Task.Delay(COLLECTION_READY_DELAY_MS);
                
                // Use a temporary collection and replace the entire ObservableCollection
                var newCollection = new ObservableCollection<T>(newItems);
                collection.Clear();
                foreach (var item in newCollection)
                {
                    collection.Add(item);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating {CollectionName} collection", collectionName);
                // Fallback: try individual adds with delays
                try
                {
                    collection.Clear();
                    await Task.Delay(FALLBACK_BATCH_DELAY_MS);
                    foreach (var item in newItems)
                    {
                        collection.Add(item);
                        await Task.Delay(FALLBACK_ITEM_DELAY_MS);
                    }
                }
                catch (Exception fallbackEx)
                {
                    _logger.LogError(fallbackEx, "Fallback collection update also failed for {CollectionName}", collectionName);
                }
            }
        });
    }

    protected void HandleDownloadError(DownloadProgress progress)
    {
        HasError = true;
        ErrorMessage = GetErrorMessage(progress);
        IsDownloading = false;
        OnDownloadError(progress);
    }

    protected string GetProgressText(DownloadProgress progress)
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

    protected virtual string GetErrorMessage(DownloadProgress progress)
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
            _ => progress.ErrorMessage ?? _localizationService.GetString("PackDownload_DownloadError")
        };
    }

    protected virtual string GetErrorMessage(Exception ex)
    {
        return _localizationService.GetString("PackDownload_ErrorMessage");
    }

    // Abstract methods that must be implemented by derived classes
    protected abstract string GetDownloadStartEventName(bool isUpdate);
    protected abstract string GetDownloadCompletedEventName(bool isUpdate);
    protected abstract Task OnDownloadCompleted(PackCardViewModel packCard, double downloadDuration);

    // Virtual methods that can be optionally overridden
    protected virtual void OnDownloadStarted(PackCardViewModel packCard) { }
    protected virtual void OnDownloadError(DownloadProgress progress) { }
    protected virtual void OnDownloadFinished(PackCardViewModel? packCard) { }
}