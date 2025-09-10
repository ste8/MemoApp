using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MemoApp.Core.Packs;
using MemoApp.Core.Services.Analytics;
using MemoApp.Core.Services.Packs;
using MemoApp.Localization.Services;
using MemoApp.UI.MauiApp.Services;
using Microsoft.Extensions.Logging;

namespace MemoApp.UI.MauiApp.ViewModels.Onboarding;

public partial class PackDownloadOnboardingViewModel : BaseViewModel
{
    private readonly IPackDownloadService _downloadService;
    private readonly IPackManagerService _packManager;
    private readonly MauiPackService _mauiPackService;
    private readonly ILocalizationService _localizationService;
    private readonly IAnalyticsService _analyticsService;
    private readonly ILogger<PackDownloadOnboardingViewModel> _logger;
    private CancellationTokenSource? _downloadCancellation;

    [ObservableProperty]
    private ObservableCollection<PackCardViewModel> availablePacks = new();


    [ObservableProperty]
    private bool isLoading = true;

    [ObservableProperty]
    private bool isDownloading;

    [ObservableProperty]
    private bool hasError;

    [ObservableProperty]
    private string? errorMessage;

    [ObservableProperty]
    private bool hasDownloadedPack;

    [ObservableProperty]
    private double downloadProgress;

    [ObservableProperty]
    private string? progressText;

    [ObservableProperty]
    private bool showSkipOption = true;

    public bool HasMultiplePacks => AvailablePacks?.Count > 1;

    public PackDownloadOnboardingViewModel(
        IPackDownloadService downloadService,
        IPackManagerService packManager,
        MauiPackService mauiPackService,
        ILocalizationService localizationService,
        IAnalyticsService analyticsService,
        ILogger<PackDownloadOnboardingViewModel> logger)
    {
        _downloadService = downloadService;
        _packManager = packManager;
        _mauiPackService = mauiPackService;
        _localizationService = localizationService;
        _analyticsService = analyticsService;
        _logger = logger;

        Title = _localizationService.GetString("OnboardingPackDownload_Title");
    }

    public async Task InitializeAsync()
    {
        try
        {
            IsLoading = true;
            HasError = false;
            ErrorMessage = null;

            // Check if user already has packs downloaded
            HasDownloadedPack = await _packManager.HasAnyPackDownloadedAsync();
            
            if (HasDownloadedPack)
            {
                _logger.LogInformation("User already has packs downloaded, showing pack status");
                return;
            }

            // Load available packs
            var packIndex = await _downloadService.GetAvailablePacksAsync();
            
            if (packIndex?.Packs == null || packIndex.Packs.Count == 0)
            {
                HasError = true;
                ErrorMessage = _localizationService.GetString("OnboardingPackDownload_NoPacksError");
                return;
            }

            // Populate available packs for onboarding context
            AvailablePacks.Clear();
            foreach (var pack in packIndex.Packs.Where(p => !p.IsDownloaded))
            {
                var packCard = new PackCardViewModel(pack, _localizationService, PackCardContext.Onboarding);
                packCard.DownloadCommand = new RelayCommand<PackCardViewModel>(async (p) => await DownloadSpecificPackCard(p));
                AvailablePacks.Add(packCard);
            }
            
            _logger.LogInformation("Loaded {PackCount} available packs for onboarding", AvailablePacks.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize pack download onboarding");
            HasError = true;
            ErrorMessage = _localizationService.GetString("OnboardingPackDownload_LoadError");
        }
        finally
        {
            IsLoading = false;
        }
    }


    [RelayCommand]
    private async Task DownloadSpecificPack(PackItemViewModel packItem)
    {
        if (packItem == null || IsDownloading)
            return;

        await DownloadPackAsync(packItem);
    }

    private async Task DownloadSpecificPackCard(PackCardViewModel? packCard)
    {
        if (packCard == null || IsDownloading)
            return;

        await DownloadPackCardAsync(packCard);
    }

    [RelayCommand]
    private void CancelDownload()
    {
        _downloadCancellation?.Cancel();
        IsDownloading = false;
        ShowSkipOption = true;
    }

    private async Task DownloadPackAsync(PackItemViewModel packItem)
    {
        var downloadStartTime = DateTime.UtcNow;
        
        try
        {
            // Track download start analytics
            await _analyticsService.TrackEventAsync(
                AnalyticsEvents.OnboardingPackDownloadStarted,
                new Dictionary<string, object>
                {
                    [AnalyticsParameters.PackName] = packItem.Pack.InternationalName,
                    [AnalyticsParameters.PackLanguage] = packItem.Pack.LanguageCode,
                    [AnalyticsParameters.PackSizeMB] = Math.Round(packItem.Pack.FileSize / (1024.0 * 1024.0), 2)
                });

            IsDownloading = true;
            ShowSkipOption = false;
            packItem.IsDownloading = true;
            _downloadCancellation = new CancellationTokenSource();

            var progress = new Progress<DownloadProgress>(p =>
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    DownloadProgress = p.ProgressPercentage;
                    packItem.DownloadProgress = p.ProgressPercentage;
                    ProgressText = GetProgressText(p);
                    packItem.ProgressText = ProgressText;
                    
                    if (p.State == DownloadState.Failed)
                    {
                        HandleDownloadError(p);
                    }
                });
            });

            var result = await _downloadService.DownloadPackAsync(
                packItem.Pack, progress, _downloadCancellation.Token);

            if (result.State == DownloadState.Completed)
            {
                var downloadEndTime = DateTime.UtcNow;
                var downloadDuration = (downloadEndTime - downloadStartTime).TotalSeconds;

                // Track successful download analytics
                await _analyticsService.TrackEventAsync(
                    AnalyticsEvents.OnboardingPackDownloadCompleted,
                    new Dictionary<string, object>
                    {
                        [AnalyticsParameters.PackName] = packItem.Pack.InternationalName,
                        [AnalyticsParameters.PackLanguage] = packItem.Pack.LanguageCode,
                        [AnalyticsParameters.PackSizeMB] = Math.Round(packItem.Pack.FileSize / (1024.0 * 1024.0), 2),
                        [AnalyticsParameters.DownloadDurationSeconds] = (int)downloadDuration,
                        [AnalyticsParameters.DownloadSuccess] = true
                    });

                _logger.LogInformation("Pack {PackId} downloaded successfully during onboarding", packItem.Pack.Id);
                
                // Generate thumbnails after successful download
                ProgressText = _localizationService.GetString("PackDownload_GeneratingThumbnailsLabel");
                await _mauiPackService.GenerateThumbnailsForPackAsync(packItem.Pack.Id, null);

                // Update state
                packItem.Pack.IsDownloaded = true;
                HasDownloadedPack = true;
                ProgressText = _localizationService.GetString("OnboardingPackDownload_CompletedMessage");
            }
            else
            {
                // Track failed download analytics
                var downloadEndTime = DateTime.UtcNow;
                var downloadDuration = (downloadEndTime - downloadStartTime).TotalSeconds;

                await _analyticsService.TrackEventAsync(
                    AnalyticsEvents.OnboardingPackDownloadCompleted,
                    new Dictionary<string, object>
                    {
                        [AnalyticsParameters.PackName] = packItem.Pack.InternationalName,
                        [AnalyticsParameters.PackLanguage] = packItem.Pack.LanguageCode,
                        [AnalyticsParameters.DownloadDurationSeconds] = (int)downloadDuration,
                        [AnalyticsParameters.DownloadSuccess] = false,
                        [AnalyticsParameters.ErrorMessage] = result.ErrorMessage ?? "Unknown error"
                    });

                HandleDownloadError(result);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error downloading pack {PackId} during onboarding", packItem.Pack.Id);
            HasError = true;
            ErrorMessage = _localizationService.GetString("OnboardingPackDownload_DownloadError");
        }
        finally
        {
            IsDownloading = false;
            ShowSkipOption = true;
            packItem.IsDownloading = false;
            _downloadCancellation?.Dispose();
            _downloadCancellation = null;
        }
    }

    private async Task DownloadPackCardAsync(PackCardViewModel packCard)
    {
        var downloadStartTime = DateTime.UtcNow;
        
        try
        {
            // Track download start analytics
            await _analyticsService.TrackEventAsync(
                AnalyticsEvents.OnboardingPackDownloadStarted,
                new Dictionary<string, object>
                {
                    [AnalyticsParameters.PackName] = packCard.Pack.InternationalName,
                    [AnalyticsParameters.PackLanguage] = packCard.Pack.LanguageCode,
                    [AnalyticsParameters.PackSizeMB] = Math.Round(packCard.Pack.FileSize / (1024.0 * 1024.0), 2)
                });

            IsDownloading = true;
            ShowSkipOption = false;
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

                // Track successful download analytics
                await _analyticsService.TrackEventAsync(
                    AnalyticsEvents.OnboardingPackDownloadCompleted,
                    new Dictionary<string, object>
                    {
                        [AnalyticsParameters.PackName] = packCard.Pack.InternationalName,
                        [AnalyticsParameters.PackLanguage] = packCard.Pack.LanguageCode,
                        [AnalyticsParameters.PackSizeMB] = Math.Round(packCard.Pack.FileSize / (1024.0 * 1024.0), 2),
                        [AnalyticsParameters.DownloadDurationSeconds] = (int)downloadDuration,
                        [AnalyticsParameters.DownloadSuccess] = true
                    });

                _logger.LogInformation("Pack {PackId} downloaded successfully during onboarding", packCard.Pack.Id);
                
                // Generate thumbnails after successful download
                ProgressText = _localizationService.GetString("PackDownload_GeneratingThumbnailsLabel");
                await _mauiPackService.GenerateThumbnailsForPackAsync(packCard.Pack.Id, null);

                // Update state
                packCard.Pack.IsDownloaded = true;
                packCard.RefreshState();
                HasDownloadedPack = true;
                ProgressText = _localizationService.GetString("OnboardingPackDownload_CompletedMessage");
            }
            else
            {
                // Track failed download analytics
                var downloadEndTime = DateTime.UtcNow;
                var downloadDuration = (downloadEndTime - downloadStartTime).TotalSeconds;

                await _analyticsService.TrackEventAsync(
                    AnalyticsEvents.OnboardingPackDownloadCompleted,
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
            _logger.LogError(ex, "Unexpected error downloading pack {PackId} during onboarding", packCard.Pack.Id);
            HasError = true;
            ErrorMessage = _localizationService.GetString("OnboardingPackDownload_DownloadError");
        }
        finally
        {
            IsDownloading = false;
            ShowSkipOption = true;
            packCard.IsDownloading = false;
            _downloadCancellation?.Dispose();
            _downloadCancellation = null;
        }
    }

    private void HandleDownloadError(DownloadProgress progress)
    {
        HasError = true;
        ErrorMessage = GetErrorMessage(progress);
        IsDownloading = false;
        ShowSkipOption = true;
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
                _localizationService.GetString("OnboardingPackDownload_InsufficientStorageError")
                    .Replace("{0}", Math.Ceiling((progress.RequiredStorageBytes ?? 0) / (1024.0 * 1024.0)).ToString()),
            DownloadErrorType.NetworkError => 
                _localizationService.GetString("OnboardingPackDownload_NetworkError"),
            DownloadErrorType.ExtractionError => 
                _localizationService.GetString("OnboardingPackDownload_ExtractionError"),
            DownloadErrorType.ValidationError => 
                _localizationService.GetString("OnboardingPackDownload_ValidationError"),
            _ => progress.ErrorMessage ?? _localizationService.GetString("OnboardingPackDownload_DownloadError")
        };
    }
}