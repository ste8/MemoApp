using System.Collections.ObjectModel;
using System.Net;
using System.Net.Http;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using MemoApp.Core.Packs;
using MemoApp.Core.Services.Analytics;
using MemoApp.Core.Services.Packs;
using MemoApp.Localization.Services;
using MemoApp.UI.MauiApp.Services;
using MemoApp.UI.MauiApp.Services.ErrorHandling;
using MemoApp.UI.MauiApp.Messages;
using Microsoft.Extensions.Logging;

namespace MemoApp.UI.MauiApp.ViewModels;

public partial class PackDownloadViewModel : PackDownloadBaseViewModel
{
    private readonly IMessenger _messenger;
    private readonly ILogger<PackDownloadViewModel> _typedLogger;
    private readonly IExceptionHandler _exceptionHandler;

    [ObservableProperty]
    private ObservableCollection<PackCardViewModel> packs = new();

    [ObservableProperty]
    private bool isRefreshing;

    [ObservableProperty]
    private bool isInitialLoading;

    [ObservableProperty]
    private string? statusMessage;

    [ObservableProperty]
    private bool hasNoPacks;

    [ObservableProperty]
    private int updateCount;

    [ObservableProperty]
    private bool hasUpdatesAvailable;

    [ObservableProperty]
    private PackCardViewModel? currentDownloadingPack;

    public ICommand RefreshCommand { get; }
    public ICommand DownloadPackCommand { get; }
    public ICommand CancelDownloadCommand { get; }
    public ICommand DeletePackCommand { get; }
    public ICommand ScrollToPackCommand { get; }

    public PackDownloadViewModel(
        IPackDownloadService downloadService,
        IPackManagerService packManager,
        MauiPackService mauiPackService,
        ILocalizationService localizationService,
        IAnalyticsService analyticsService,
        IMessenger messenger,
        ILogger<PackDownloadViewModel> logger,
        IExceptionHandler exceptionHandler)
        : base(downloadService, packManager, mauiPackService, localizationService, analyticsService, logger)
    {
        _messenger = messenger;
        _typedLogger = logger;
        _exceptionHandler = exceptionHandler;

        Title = _localizationService.GetString("PackDownload_Title");

        RefreshCommand = new AsyncRelayCommand(RefreshPacksAsync);
        DownloadPackCommand = new AsyncRelayCommand<PackItemViewModel>(DownloadPackAsync);
        CancelDownloadCommand = new RelayCommand(CancelDownload);
        DeletePackCommand = new AsyncRelayCommand<PackItemViewModel>(DeletePackAsync);
        ScrollToPackCommand = new AsyncRelayCommand<PackCardViewModel>(ScrollToPackAsync);
    }

    public async Task InitializeAsync()
    {
        try
        {
            IsInitialLoading = true;
            _exceptionHandler.AddBreadcrumb("Starting pack download page initialization", "pack_download");
            await RefreshPacksAsync();
        }
        catch (Exception ex)
        {
            // Centralized exception handling with context
            await _exceptionHandler.HandleExceptionAsync(ex, "pack_download_initialize",
                new Dictionary<string, object>
                {
                    { "is_initial_load", true },
                    { "packs_count", Packs.Count }
                });

            // Set UI state appropriately
            StatusMessage = _localizationService.GetString("PackDownload_ErrorMessage");
            HasNoPacks = true;
        }
        finally
        {
            IsInitialLoading = false;
        }

        // Log application data directory for debugging
        _typedLogger.LogInformation("Application data directory: {DataDir}", FileSystem.Current.AppDataDirectory);
    }

    private async Task RefreshPacksAsync()
    {
        try
        {
            // Only set IsRefreshing if not in initial loading state
            if (!IsInitialLoading)
            {
                IsRefreshing = true;
            }
            StatusMessage = null;
            HasNoPacks = false;

            // Clear cache and get fresh data
            _downloadService.ClearCache();
            var index = await _downloadService.GetAvailablePacksAsync(forceRefresh: true);
            
            if (index == null)
            {
                StatusMessage = _localizationService.GetString("PackDownload_NetworkErrorMessage");
                HasNoPacks = true;
                return;
            }

            // Prepare pack cards to avoid CollectionView layout issues
            var packCards = new List<PackCardViewModel>();
            int availableUpdates = 0;

            foreach (var pack in index.Packs)
            {
                var packCard = new PackCardViewModel(pack, _localizationService, PackCardContext.Management);
                packCard.DownloadCommand = new RelayCommand<PackCardViewModel>(p => { if (p != null) _ = DownloadPackCardAsync(p); });
                packCard.DeleteCommand = new RelayCommand<PackCardViewModel>(p => { if (p != null) _ = DeletePackCardAsync(p); });
                packCards.Add(packCard);

                if (pack.HasUpdate)
                {
                    availableUpdates++;
                }
            }

            // Use the base class iOS-safe collection update method
            await UpdatePackCollectionSafelyAsync(Packs, packCards, "Packs");

            HasNoPacks = packCards.Count == 0;

            if (HasNoPacks)
            {
                StatusMessage = _localizationService.GetString("PackDownload_NoPacksMessage");
            }

            // Update the counter for the refresh button badge
            UpdateCount = availableUpdates;
            HasUpdatesAvailable = availableUpdates > 0;
        }
        catch (HttpRequestException ex)
        {
            _typedLogger.LogError(ex, "Network error while refreshing packs");
            StatusMessage = _localizationService.GetString("PackDownload_NetworkErrorMessage");
            HasNoPacks = true;
        }
        catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
        {
            _typedLogger.LogError(ex, "Timeout while refreshing packs");
            StatusMessage = _localizationService.GetString("PackDownload_NetworkErrorMessage");
            HasNoPacks = true;
        }
        catch (Exception ex)
        {
            _typedLogger.LogError(ex, "Failed to refresh packs");
            StatusMessage = _localizationService.GetString("PackDownload_ErrorMessage");
            HasNoPacks = true;
        }
        finally
        {
            // Only clear IsRefreshing if not in initial loading state
            if (!IsInitialLoading)
            {
                IsRefreshing = false;
            }
        }
    }

    private async Task DownloadPackAsync(PackItemViewModel? packItem)
    {
        if (packItem == null || IsDownloading)
            return;

        // Convert PackItemViewModel to PackCardViewModel for base class method
        var packCard = new PackCardViewModel(packItem.Pack, _localizationService, PackCardContext.Management);
        await DownloadPackCardAsync(packCard);
    }

    private async void CancelDownload()
    {
        if (_downloadCancellation == null || CurrentDownloadingPack == null)
            return;

        // Show confirmation dialog
        var packName = CurrentDownloadingPack.DisplayName;
        var confirm = await Application.Current!.MainPage!.DisplayAlert(
            _localizationService.GetString("PackDownload_CancelConfirmTitle"),
            _localizationService.GetString("PackDownload_CancelConfirmMessage").Replace("{0}", packName),
            _localizationService.GetString("Common_Yes"),
            _localizationService.GetString("Common_No"));

        if (!confirm)
            return;

        try
        {
            // Cancel the download
            _downloadCancellation.Cancel();

            // Track analytics
            await _analyticsService.TrackEventAsync(AnalyticsEvents.PackDownloadCanceled,
                new Dictionary<string, object>
                {
                    [AnalyticsParameters.PackName] = CurrentDownloadingPack.Pack.InternationalName,
                    ["progress_percent"] = CurrentDownloadingPack.DownloadProgress
                });

            _typedLogger.LogInformation("User canceled download of pack {PackId} at {Progress}%",
                CurrentDownloadingPack.Pack.Id, CurrentDownloadingPack.DownloadProgress);
        }
        catch (Exception ex)
        {
            _typedLogger.LogError(ex, "Failed to cancel download for pack {PackId}",
                CurrentDownloadingPack?.Pack.Id);
        }
        finally
        {
            CurrentDownloadingPack = null;
        }
    }

    private async Task ScrollToPackAsync(PackCardViewModel? packCard)
    {
        if (packCard == null)
            return;

        try
        {
            // Find the pack in the collection
            var index = Packs.IndexOf(packCard);
            if (index >= 0)
            {
                // TODO: Implement scroll to item in CollectionView
                // This would require passing the CollectionView reference or using MessagingCenter
                _typedLogger.LogInformation("Scroll to pack requested for {PackId} at index {Index}",
                    packCard.Pack.Id, index);
            }
        }
        catch (Exception ex)
        {
            _typedLogger.LogError(ex, "Failed to scroll to pack {PackId}", packCard.Pack.Id);
        }
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
                        _typedLogger.LogInformation("Sent PackDeletedMessage for pack {PackId} (was last pack: {WasLastPack})", 
                            packItem.Pack.Id, wasLastPack);
                    }
                    catch (Exception ex)
                    {
                        _typedLogger.LogError(ex, "Failed to send pack delete message for {PackId}", packItem.Pack.Id);
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
            _typedLogger.LogError(ex, "Failed to delete pack {PackId}", packItem.Pack.Id);
            StatusMessage = _localizationService.GetString("PackDownload_ErrorMessage");
        }
    }

    private async Task DeletePackCardAsync(PackCardViewModel? packCard)
    {
        if (packCard == null)
            return;

        try
        {
            var success = await _downloadService.DeletePackAsync(packCard.Pack.Id);
            packCard.Pack.IsDownloaded = false;
            packCard.Pack.HasUpdate = false;
            packCard.RefreshState();

            StatusMessage = _localizationService.GetString("PackDownload_DeleteCompleted").Replace("{0}", packCard.Pack.GetDisplayName(true));
            _typedLogger.LogInformation("Pack {PackId} deleted successfully", packCard.Pack.Id);
        }
        catch (Exception ex)
        {
            _typedLogger.LogError(ex, "Failed to delete pack {PackId}", packCard.Pack.Id);
            StatusMessage = _localizationService.GetString("PackDownload_DeleteError");
        }
    }

    // Abstract method implementations for base class
    protected override string GetDownloadStartEventName(bool isUpdate)
    {
        return isUpdate ? AnalyticsEvents.PackUpdateStarted : AnalyticsEvents.PackDownloadStarted;
    }

    protected override string GetDownloadCompletedEventName(bool isUpdate)
    {
        return isUpdate ? AnalyticsEvents.PackUpdated : AnalyticsEvents.PackDownloaded;
    }

    protected override void OnDownloadStarted(PackCardViewModel packCard)
    {
        base.OnDownloadStarted(packCard);
        CurrentDownloadingPack = packCard;
    }

    protected override void OnDownloadFinished(PackCardViewModel? packCard)
    {
        base.OnDownloadFinished(packCard);
        CurrentDownloadingPack = null; // Clear global progress on any finish (cancel, fail, or complete)
    }

    protected override async Task OnDownloadCompleted(PackCardViewModel packCard, double downloadDuration)
    {
        var isUpdate = packCard.Pack.HasUpdate;
        var previousVersion = packCard.Pack.Version; // Capture version before update

        // Log pack location for debugging
        var packPath = await _packManager.GetPackDirectoryAsync(packCard.Pack.Id);
        _typedLogger.LogInformation("Pack {PackId} downloaded successfully to: {PackPath}", packCard.Pack.Id, packPath);

        StatusMessage = isUpdate
            ? _localizationService.GetString("PackDownload_UpdateCompleted").Replace("{0}", packCard.Pack.GetDisplayName(true))
            : _localizationService.GetString("PackDownload_DownloadCompleted").Replace("{0}", packCard.Pack.GetDisplayName(true));

        // Send appropriate message to notify other ViewModels
        await MainThread.InvokeOnMainThreadAsync(() =>
        {
            try
            {
                if (isUpdate)
                {
                    var updateMessage = PackUpdatedMessage.FromPack(packCard.Pack, previousVersion, downloadDuration);
                    _messenger.Send(updateMessage);
                    _typedLogger.LogInformation("Sent PackUpdatedMessage for pack {PackId} (v{PreviousVersion} → v{NewVersion})", 
                        packCard.Pack.Id, previousVersion, packCard.Pack.Version);
                }
                else
                {
                    var downloadMessage = PackDownloadedMessage.FromPack(packCard.Pack, downloadDuration);
                    _messenger.Send(downloadMessage);
                    _typedLogger.LogInformation("Sent PackDownloadedMessage for pack {PackId} (v{Version})", 
                        packCard.Pack.Id, packCard.Pack.Version);
                }
            }
            catch (Exception ex)
            {
                _typedLogger.LogError(ex, "Failed to send pack message for {PackId}", packCard.Pack.Id);
            }
        });

        // Refresh to update UI state
        await RefreshPacksAsync();
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