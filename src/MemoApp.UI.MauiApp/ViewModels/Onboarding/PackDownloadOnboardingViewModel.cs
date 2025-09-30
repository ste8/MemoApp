using System.Collections.ObjectModel;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MemoApp.Core.Packs;
using MemoApp.Core.Services.Analytics;
using MemoApp.Core.Services.Packs;
using MemoApp.Localization.Services;
using MemoApp.UI.MauiApp.Services;
using Microsoft.Extensions.Logging;

namespace MemoApp.UI.MauiApp.ViewModels.Onboarding;

public partial class PackDownloadOnboardingViewModel : PackDownloadBaseViewModel
{
    private readonly ILogger<PackDownloadOnboardingViewModel> _typedLogger;

    [ObservableProperty]
    private ObservableCollection<PackCardViewModel> availablePacks = new();

    [ObservableProperty]
    private bool isLoading = true;

    [ObservableProperty]
    private bool hasDownloadedPack;

    [ObservableProperty]
    private bool showSkipOption = true;

    [ObservableProperty]
    private PackCardViewModel? currentDownloadingPack;

    public bool HasMultiplePacks => AvailablePacks?.Count > 1;

    public PackDownloadOnboardingViewModel(
        IPackDownloadService downloadService,
        IPackManagerService packManager,
        MauiPackService mauiPackService,
        ILocalizationService localizationService,
        IAnalyticsService analyticsService,
        ILogger<PackDownloadOnboardingViewModel> logger)
        : base(downloadService, packManager, mauiPackService, localizationService, analyticsService, logger)
    {
        _typedLogger = logger;
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
                _typedLogger.LogInformation("User already has packs downloaded, showing pack status");
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
            var packCards = new List<PackCardViewModel>();
            foreach (var pack in packIndex.Packs.Where(p => !p.IsDownloaded))
            {
                var packCard = new PackCardViewModel(pack, _localizationService, PackCardContext.Onboarding);
                packCard.DownloadCommand = new RelayCommand<PackCardViewModel>(async (p) => await DownloadSpecificPackCard(p));
                packCards.Add(packCard);
            }
            
            // Use the base class iOS-safe collection update method
            await UpdatePackCollectionSafelyAsync(AvailablePacks, packCards, "AvailablePacks");
            
            _typedLogger.LogInformation("Loaded {PackCount} available packs for onboarding", packCards.Count);
        }
        catch (Exception ex)
        {
            _typedLogger.LogError(ex, "Failed to initialize pack download onboarding");
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

        // Convert PackItemViewModel to PackCardViewModel for base class method
        var packCard = new PackCardViewModel(packItem.Pack, _localizationService, PackCardContext.Onboarding);
        await DownloadPackCardAsync(packCard);
    }

    private async Task DownloadSpecificPackCard(PackCardViewModel? packCard)
    {
        await DownloadPackCardAsync(packCard);
    }

    [RelayCommand]
    private void CancelDownload()
    {
        _downloadCancellation?.Cancel();
        CurrentDownloadingPack = null;
        IsDownloading = false;
        ShowSkipOption = true;
    }

    // Abstract method implementations for base class
    protected override string GetDownloadStartEventName(bool isUpdate)
    {
        return AnalyticsEvents.OnboardingPackDownloadStarted;
    }

    protected override string GetDownloadCompletedEventName(bool isUpdate)
    {
        return AnalyticsEvents.OnboardingPackDownloadCompleted;
    }

    protected override async Task OnDownloadCompleted(PackCardViewModel packCard, double downloadDuration)
    {
        HasDownloadedPack = true;
        ProgressText = _localizationService.GetString("OnboardingPackDownload_CompletedMessage");
    }

    // Virtual method overrides for onboarding-specific behavior
    protected override void OnDownloadStarted(PackCardViewModel packCard)
    {
        base.OnDownloadStarted(packCard);
        CurrentDownloadingPack = packCard;
        ShowSkipOption = false;
    }

    protected override void OnDownloadFinished(PackCardViewModel? packCard)
    {
        base.OnDownloadFinished(packCard);
        CurrentDownloadingPack = null;
        ShowSkipOption = true;
    }

    protected override string GetErrorMessage(DownloadProgress progress)
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

    protected override string GetErrorMessage(Exception ex)
    {
        return _localizationService.GetString("OnboardingPackDownload_DownloadError");
    }
}