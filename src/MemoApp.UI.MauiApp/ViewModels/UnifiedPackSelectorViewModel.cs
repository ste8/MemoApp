using System.Collections.ObjectModel;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MemoApp.Core.Packs;
using MemoApp.Core.Services.Packs;
using MemoApp.Localization.Services;
using MemoApp.UI.MauiApp.Controls;

namespace MemoApp.UI.MauiApp.ViewModels;

/// <summary>
/// Unified ViewModel for pack selection that works in both onboarding and management contexts
/// </summary>
public partial class UnifiedPackSelectorViewModel : BaseViewModel
{
    private readonly IPackDownloadService _downloadService;
    private readonly IPackManagerService _packManager;
    private readonly ILocalizationService _localizationService;
    
    #region Observable Properties
    
    [ObservableProperty]
    private PackSelectorMode mode = PackSelectorMode.Management;
    
    [ObservableProperty]
    private ObservableCollection<UnifiedPackItemViewModel> packs = new();
    
    [ObservableProperty]
    private ObservableCollection<UnifiedPackItemViewModel> filteredPacks = new();
    
    [ObservableProperty]
    private UnifiedPackItemViewModel? selectedPack;
    
    [ObservableProperty]
    private bool isLoading;
    
    [ObservableProperty]
    private bool hasError;
    
    [ObservableProperty]
    private string? errorMessage;
    
    [ObservableProperty]
    private bool hasPacks;
    
    [ObservableProperty]
    private bool hasSelectedPack;
    
    [ObservableProperty]
    private string headerTitle = string.Empty;
    
    [ObservableProperty]
    private string? headerSubtitle;
    
    [ObservableProperty]
    private bool showRefreshButton;
    
    [ObservableProperty]
    private bool showFilters;
    
    [ObservableProperty]
    private bool isOnboardingMode;
    
    [ObservableProperty]
    private string selectionMode = "None";
    
    // Filtering properties
    [ObservableProperty]
    private ObservableCollection<string> availableLanguages = new();
    
    [ObservableProperty]
    private string? selectedLanguage;
    
    [ObservableProperty]
    private ObservableCollection<string> availableCompanies = new();
    
    [ObservableProperty]
    private string? selectedCompany;
    
    [ObservableProperty]
    private bool hasMultipleCompanies;
    
    #endregion
    
    #region Commands
    
    public ICommand RefreshCommand { get; }
    public ICommand RetryCommand { get; }
    public ICommand DownloadSelectedCommand { get; }
    public ICommand SkipCommand { get; }
    public ICommand FilterChangedCommand { get; }
    
    #endregion
    
    public UnifiedPackSelectorViewModel(
        IPackDownloadService downloadService,
        IPackManagerService packManager,
        ILocalizationService localizationService)
    {
        _downloadService = downloadService;
        _packManager = packManager;
        _localizationService = localizationService;
        
        RefreshCommand = new AsyncRelayCommand(RefreshPacksAsync);
        RetryCommand = new AsyncRelayCommand(RefreshPacksAsync);
        DownloadSelectedCommand = new AsyncRelayCommand(DownloadSelectedPackAsync);
        SkipCommand = new RelayCommand(HandleSkip);
        FilterChangedCommand = new RelayCommand(ApplyFilters);
    }
    
    public async Task InitializeAsync(PackSelectorMode mode)
    {
        Mode = mode;
        ConfigureForMode(mode);
        await RefreshPacksAsync();
    }
    
    private void ConfigureForMode(PackSelectorMode mode)
    {
        IsOnboardingMode = mode == PackSelectorMode.Onboarding;
        
        switch (mode)
        {
            case PackSelectorMode.Onboarding:
                HeaderTitle = _localizationService.GetString("OnboardingPackDownload_Title");
                HeaderSubtitle = _localizationService.GetString("OnboardingPackDownload_Subtitle");
                ShowRefreshButton = false;
                ShowFilters = false;
                SelectionMode = "Single";
                break;
                
            case PackSelectorMode.Management:
                HeaderTitle = _localizationService.GetString("PackDownload_Title");
                HeaderSubtitle = null;
                ShowRefreshButton = true;
                ShowFilters = Packs.Count > 5; // Show filters when there are many packs
                SelectionMode = "None";
                break;
        }
    }
    
    private async Task RefreshPacksAsync()
    {
        try
        {
            IsLoading = true;
            HasError = false;
            ErrorMessage = null;
            
            var index = await _downloadService.GetAvailablePacksAsync(forceRefresh: true);
            
            if (index?.Packs == null || !index.Packs.Any())
            {
                HasPacks = false;
                return;
            }
            
            // Convert to unified view models
            var packItems = index.Packs.Select(p => new UnifiedPackItemViewModel(p, _localizationService)
            {
                ActionCommand = new AsyncRelayCommand<UnifiedPackItemViewModel>(HandlePackActionAsync),
                IsSelectable = Mode == PackSelectorMode.Onboarding
            }).ToList();
            
            Packs = new ObservableCollection<UnifiedPackItemViewModel>(packItems);
            
            // Extract unique languages and companies for filters
            ExtractFilterOptions();
            
            // Apply initial filters
            ApplyFilters();
            
            HasPacks = FilteredPacks.Any();
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = _localizationService.GetString("PackDownload_LoadError");
            HasPacks = false;
        }
        finally
        {
            IsLoading = false;
        }
    }
    
    private void ExtractFilterOptions()
    {
        var languages = Packs.Select(p => p.LanguageText).Distinct().OrderBy(l => l);
        AvailableLanguages = new ObservableCollection<string>(languages);
        
        var companies = Packs.Where(p => !string.IsNullOrEmpty(p.CompanyName))
                             .Select(p => p.CompanyName!)
                             .Distinct()
                             .OrderBy(c => c);
        AvailableCompanies = new ObservableCollection<string>(companies);
        HasMultipleCompanies = AvailableCompanies.Count > 1;
    }
    
    private void ApplyFilters()
    {
        var filtered = Packs.AsEnumerable();
        
        if (!string.IsNullOrEmpty(SelectedLanguage))
        {
            filtered = filtered.Where(p => p.LanguageText == SelectedLanguage);
        }
        
        if (!string.IsNullOrEmpty(SelectedCompany))
        {
            filtered = filtered.Where(p => p.CompanyName == SelectedCompany);
        }
        
        FilteredPacks = new ObservableCollection<UnifiedPackItemViewModel>(filtered);
        HasPacks = FilteredPacks.Any();
    }
    
    private async Task HandlePackActionAsync(UnifiedPackItemViewModel? pack)
    {
        if (pack == null) return;
        
        if (Mode == PackSelectorMode.Onboarding)
        {
            // In onboarding, selecting a pack just marks it as selected
            SelectedPack = pack;
            HasSelectedPack = true;
            
            // Update visual state for all packs
            foreach (var p in FilteredPacks)
            {
                p.IsSelected = p == pack;
            }
        }
        else
        {
            // In management mode, execute the action immediately
            await ExecutePackActionAsync(pack);
        }
    }
    
    private async Task ExecutePackActionAsync(UnifiedPackItemViewModel pack)
    {
        if (pack.IsDownloaded && !pack.HasUpdate)
        {
            // Show delete confirmation
            var confirm = await Application.Current!.MainPage!.DisplayAlert(
                _localizationService.GetString("PackDownload_DeleteTitle"),
                _localizationService.GetString("PackDownload_DeleteConfirmation"),
                _localizationService.GetString("Common_Delete"),
                _localizationService.GetString("Common_Cancel"));
                
            if (confirm)
            {
                await DeletePackAsync(pack);
            }
        }
        else
        {
            // Download or update
            await DownloadPackAsync(pack);
        }
    }
    
    private async Task DownloadSelectedPackAsync()
    {
        if (SelectedPack == null) return;
        await DownloadPackAsync(SelectedPack);
    }
    
    private async Task DownloadPackAsync(UnifiedPackItemViewModel pack)
    {
        try
        {
            pack.IsProcessing = true;
            pack.Progress = 0;
            pack.ProgressText = _localizationService.GetString("PackDownload_Starting");
            
            var progress = new Progress<DownloadProgress>(p =>
            {
                pack.Progress = p.ProgressPercentage / 100.0;
                pack.ProgressText = GetProgressText(p);
            });
            
            var result = await _downloadService.DownloadPackAsync(
                pack.Pack,
                progress,
                CancellationToken.None);
                
            if (result.State == DownloadState.Completed)
            {
                pack.IsDownloaded = true;
                pack.Pack.IsDownloaded = true;
                pack.RefreshState();
                
                if (Mode == PackSelectorMode.Onboarding)
                {
                    // Navigate to next onboarding step
                    await Shell.Current.GoToAsync("//onboarding/complete");
                }
            }
        }
        catch (Exception ex)
        {
            await Application.Current!.MainPage!.DisplayAlert(
                _localizationService.GetString("Common_Error"),
                _localizationService.GetString("PackDownload_DownloadError"),
                _localizationService.GetString("Common_OK"));
        }
        finally
        {
            pack.IsProcessing = false;
            pack.Progress = 0;
            pack.ProgressText = null;
        }
    }
    
    private async Task DeletePackAsync(UnifiedPackItemViewModel pack)
    {
        try
        {
            var success = await _downloadService.DeletePackAsync(pack.Pack.Id);
            if (success)
            {
                pack.IsDownloaded = false;
                pack.Pack.IsDownloaded = false;
                pack.RefreshState();
            }
        }
        catch (Exception ex)
        {
            await Application.Current!.MainPage!.DisplayAlert(
                _localizationService.GetString("Common_Error"),
                _localizationService.GetString("PackDownload_DeleteError"),
                _localizationService.GetString("Common_OK"));
        }
    }
    
    private void HandleSkip()
    {
        if (Mode == PackSelectorMode.Onboarding)
        {
            // Navigate to next onboarding step without downloading
            Shell.Current.GoToAsync("//onboarding/complete");
        }
    }
    
    private string GetProgressText(DownloadProgress progress)
    {
        return progress.State switch
        {
            DownloadState.Downloading => 
                $"{_localizationService.GetString("PackDownload_Downloading")} {progress.ProgressPercentage:F0}%",
            DownloadState.Extracting => 
                _localizationService.GetString("PackDownload_Extracting"),
            DownloadState.GeneratingThumbnails => 
                _localizationService.GetString("PackDownload_GeneratingThumbnails"),
            _ => string.Empty
        };
    }
    
    partial void OnSelectedPackChanged(UnifiedPackItemViewModel? value)
    {
        HasSelectedPack = value != null;
    }
}

/// <summary>
/// ViewModel for individual pack items in the unified selector
/// </summary>
public partial class UnifiedPackItemViewModel : ObservableObject
{
    private readonly ILocalizationService _localizationService;
    
    [ObservableProperty]
    private Pack pack;
    
    [ObservableProperty]
    private bool isSelected;
    
    [ObservableProperty]
    private bool isProcessing;
    
    [ObservableProperty]
    private double progress;
    
    [ObservableProperty]
    private string? progressText;
    
    [ObservableProperty]
    private bool isSelectable;
    
    [ObservableProperty]
    private bool isDownloaded;
    
    [ObservableProperty]
    private bool hasUpdate;
    
    [ObservableProperty]
    private bool canPerformAction = true;
    
    public ICommand? ActionCommand { get; set; }
    
    public string DisplayName => GetDisplayName();
    public string DisplayDescription => GetDisplayDescription();
    public string SizeText => $"{Pack.GetFormattedFileSize()}";
    public string VersionText => $"v{Pack.Version}";
    public string LanguageText => GetLanguageDisplayName();
    public string? CompanyName => Pack.Author;
    public bool ShowCompanyBadge => !string.IsNullOrEmpty(Pack.Author);
    
    public string ActionButtonText
    {
        get
        {
            if (IsProcessing)
                return _localizationService.GetString("PackDownload_Processing");
            if (IsDownloaded && HasUpdate)
                return _localizationService.GetString("PackDownload_Update");
            if (IsDownloaded)
                return _localizationService.GetString("PackDownload_Delete");
            return _localizationService.GetString("PackDownload_Download");
        }
    }
    
    public Color ActionButtonColor
    {
        get
        {
            if (IsDownloaded && !HasUpdate)
                return Colors.Red; // Delete action
            if (HasUpdate)
                return Colors.Orange; // Update action
            return Color.FromArgb("#512BD4"); // Download action (Primary)
        }
    }
    
    public string AccessibilityDescription => 
        $"{DisplayName}, {DisplayDescription}, {SizeText}, {VersionText}";
        
    public string AccessibilityHint => 
        IsSelectable ? _localizationService.GetString("PackDownload_SelectHint") : ActionButtonText;
    
    public UnifiedPackItemViewModel(Pack pack, ILocalizationService localizationService)
    {
        Pack = pack;
        _localizationService = localizationService;
        IsDownloaded = pack.IsDownloaded;
        HasUpdate = pack.HasUpdate;
    }
    
    public void RefreshState()
    {
        OnPropertyChanged(nameof(ActionButtonText));
        OnPropertyChanged(nameof(ActionButtonColor));
        OnPropertyChanged(nameof(AccessibilityHint));
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
    
    private string GetLanguageDisplayName()
    {
        return Pack.LanguageCode switch
        {
            "it" => "Italiano",
            "en" => "English",
            "es" => "Español",
            "fr" => "Français",
            "de" => "Deutsch",
            _ => Pack.LanguageCode.ToUpper()
        };
    }
}