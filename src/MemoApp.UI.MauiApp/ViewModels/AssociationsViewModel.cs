using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using MemoApp.Core.MajorSystem;
using MemoApp.Core.MajorSystem.Models;
using MemoApp.Core.MajorSystem.Services;
using MemoApp.Core.Services.Analytics;
using MemoApp.Localization.Services;
using Microsoft.Extensions.Logging;
using MemoApp.UI.MauiApp.Messages;
using MemoApp.UI.MauiApp.Services;
using MemoApp.UI.MauiApp.Services.Freemium;
using Microsoft.Extensions.Configuration;

namespace MemoApp.UI.MauiApp.ViewModels;

/// <summary>
/// ViewModel for the Associations page
/// </summary>
public partial class AssociationsViewModel : ObservableObject, 
    IRecipient<CustomAssociationDeletedMessage>, 
    IRecipient<TokenDefaultChangedMessage>,
    IRecipient<PackDownloadedMessage>,
    IRecipient<PackUpdatedMessage>,
    IRecipient<PackDeletedMessage>
{
    private readonly IMajorSystemCategoryService _categoryService;
    private readonly IPackAssociationsService _packAssociationsService;
    private readonly ITokenDefaultsService _tokenDefaultsService;
    private readonly ILocalizationService _localizationService;
    private readonly IMessenger _messenger;
    private readonly IImageCacheService _imageCacheService;
    private readonly IAnalyticsService _analyticsService;
    private readonly IUserNotificationService _userNotificationService;
    private readonly ILogger<AssociationsViewModel> _logger;
    private readonly IConfiguration _configuration;
    
    [ObservableProperty]
    private bool _isLoading;
    
    [ObservableProperty]
    private bool _hasPackAvailable;
    
    [ObservableProperty]
    private bool _isInitialLoad = true;
    
    [ObservableProperty]
    private string _emptyStateTitle = string.Empty;
    
    [ObservableProperty]
    private string _emptyStateMessage = string.Empty;
    
    [ObservableProperty]
    private CategoryTokens? _selectedCategory;
    
    partial void OnSelectedCategoryChanged(CategoryTokens? value)
    {
        if (value != null)
        {
            // Load associations when category changes
            _ = LoadCategoryAssociationsAsync(value.CategoryKey);
        }
    }
    
    [ObservableProperty]
    private ObservableCollection<CategoryTokens> _categories = new();
    
    [ObservableProperty]
    private ObservableCollection<TokenAssociation> _associations = new();
    
    [ObservableProperty]
    private TokenAssociation? _selectedAssociation;
    
    [ObservableProperty]
    private bool _showAds = true;
    
    
    public AssociationsViewModel(
        IMajorSystemCategoryService categoryService,
        IPackAssociationsService packAssociationsService,
        ITokenDefaultsService tokenDefaultsService,
        ILocalizationService localizationService,
        IMessenger messenger,
        IImageCacheService imageCacheService,
        IAnalyticsService analyticsService,
        IUserNotificationService userNotificationService,
        ILogger<AssociationsViewModel> logger,
        IConfiguration configuration)
    {
        _categoryService = categoryService;
        _packAssociationsService = packAssociationsService;
        _tokenDefaultsService = tokenDefaultsService;
        _localizationService = localizationService;
        _messenger = messenger;
        _imageCacheService = imageCacheService;
        _analyticsService = analyticsService;
        _userNotificationService = userNotificationService;
        _logger = logger;
        _configuration = configuration;
        
        // Always show ads in this version
        ShowAds = true;
        
        // Register for messaging system
        _messenger.Register<CustomAssociationDeletedMessage>(this);
        _messenger.Register<TokenDefaultChangedMessage>(this);
        _messenger.Register<PackDownloadedMessage>(this);
        _messenger.Register<PackUpdatedMessage>(this);
        _messenger.Register<PackDeletedMessage>(this);
        
        // Subscribe to pack changes
        _packAssociationsService.PackChanged += OnPackChanged;
        
        // Subscribe to number format changes
        _localizationService.NumberFormatChanged += OnNumberFormatChanged;
        
        // Initialize localized strings
        UpdateLocalizedStrings();
        
        
        // Load initial data - don't block constructor
        _ = LoadDataAsync();
    }
    
    private void OnPackChanged(object? sender, EventArgs e)
    {
        // Smart refresh: Use incremental update when possible
        _ = Task.Run(async () => 
        {
            _logger.LogInformation("Pack changed, using smart refresh strategy");
            await SmartRefreshAfterPackChangeAsync();
        });
    }
    
    
    private void OnNumberFormatChanged(object? sender, NumberFormat numberFormat)
    {
        _logger.LogInformation("Number format changed to {NumberFormat}, refreshing categories and associations", numberFormat);
        // Run on main thread and reload all data to reflect new number format
        MainThread.BeginInvokeOnMainThread(async () => await LoadDataAsync());
    }
    
    private void UpdateLocalizedStrings()
    {
        EmptyStateTitle = _localizationService.GetString("Associations_NoPackDownloaded");
        EmptyStateMessage = _localizationService.GetString("Associations_NoPackDownloadedMessage");
    }
    
    [RelayCommand]
    private async Task LoadDataAsync()
    {
        try
        {
            IsLoading = true;
            _logger.LogInformation("Starting to load associations data...");
            
            // Check if pack is available
            HasPackAvailable = await _packAssociationsService.HasPackAvailableAsync();
            _logger.LogInformation("Pack available: {HasPack}", HasPackAvailable);
            
            // Mark initial load as complete after pack availability check
            IsInitialLoad = false;
            
            if (HasPackAvailable)
            {
                await LoadCategoriesAsync();
                _logger.LogInformation("Loaded {Count} categories", Categories.Count);
                
                // Select the first category if none is selected
                if (SelectedCategory == null && Categories.Count > 0)
                {
                    SelectedCategory = Categories.First();
                    _logger.LogInformation("Selected first category: {Category}", SelectedCategory.CategoryKey);
                    await LoadCategoryAssociationsAsync(SelectedCategory.CategoryKey);
                }
            }
            else
            {
                Categories.Clear();
                Associations.Clear();
                SelectedCategory = null;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading associations data");
            
            // Show user-friendly error message
            var errorMessage = _localizationService.GetString("AssociationLoading_Failed");
            await _userNotificationService.ShowErrorToastAsync(errorMessage);
        }
        finally
        {
            IsLoading = false;
            IsInitialLoad = false; // Ensure this is always set even if an error occurs
            _logger.LogInformation("Finished loading associations data");
        }
    }
    
    private async Task LoadCategoriesAsync()
    {
        try
        {
            // Get current number format from localization service
            var numberFormat = _localizationService.CurrentNumberFormat;
            var categories = _categoryService.GetAllCategories(numberFormat);
            
            // Localize display names
            foreach (var category in categories)
            {
                category.DisplayName = GetLocalizedCategoryName(category.CategoryKey);
            }
            
            // For symbols category, load dynamic tokens if pack is available
            var symbolsCategory = categories.FirstOrDefault(c => c.CategoryKey == MajorSystemCategoryService.CategorySymbols);
            if (symbolsCategory != null && _packAssociationsService.CurrentPack != null)
            {
                var packPath = await _packAssociationsService.GetCurrentPackPathAsync();
                if (!string.IsNullOrEmpty(packPath))
                {
                    symbolsCategory.Tokens = await _categoryService.DiscoverDynamicTokensAsync(packPath, MajorSystemCategoryService.CategorySymbols);
                }
            }
            
            Categories.Clear();
            foreach (var category in categories)
            {
                Categories.Add(category);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading categories");
            
            // Show user-friendly error message for category loading failures
            var errorMessage = _localizationService.GetString("AssociationLoading_Failed");
            await _userNotificationService.ShowErrorToastAsync(errorMessage);
        }
    }
    
    private string GetLocalizedCategoryName(string categoryKey)
    {
        return categoryKey switch
        {
            MajorSystemCategoryService.CategoryNumbers => _localizationService.GetString("Associations_Category_Numbers"),
            MajorSystemCategoryService.CategoryLettersUpper => _localizationService.GetString("Associations_Category_LettersUpper"),
            MajorSystemCategoryService.CategoryLettersLower => _localizationService.GetString("Associations_Category_LettersLower"),
            MajorSystemCategoryService.CategorySymbols => _localizationService.GetString("Associations_Category_Symbols"),
            MajorSystemCategoryService.CategoryMonths => _localizationService.GetString("Associations_Category_Months"),
            _ => categoryKey
        };
    }
    
    [RelayCommand]
    private void SelectCategory(CategoryTokens category)
    {
        if (category == null) 
            return;
        
        try
        {
            _logger.LogInformation("User selected category: {CategoryKey}", category.CategoryKey);
            SelectedCategory = category; // This will trigger OnSelectedCategoryChanged
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error selecting category {CategoryKey}", category?.CategoryKey ?? "NULL");
        }
    }
    
    private async Task LoadCategoryAssociationsAsync(string categoryKey)
    {
        try
        {
            IsLoading = true;
            _hasMoreItems = true; // Reset pagination state
            _logger.LogInformation("Loading initial associations for category: {CategoryKey}", categoryKey);
            
            // Track category viewing analytics
            _ = _analyticsService.TrackEventAsync(AnalyticsEvents.MajorSystemCategoryViewed, new Dictionary<string, object>
            {
                [AnalyticsParameters.Category] = categoryKey,
                [AnalyticsParameters.PackName] = await GetCurrentPackNameAsync() ?? "unknown"
            });
            
            // Get current number format
            var numberFormat = _localizationService.CurrentNumberFormat;
            
            // Start loading task and minimum display time task concurrently
            var loadingTask = Task.Run(async () => 
                await _packAssociationsService.LoadCategoryAssociationsAsync(categoryKey, 0, PageSize, numberFormat));
            var minDisplayTask = Task.Delay(300); // Reduced to 300ms for better perceived performance
            
            // Wait for both data loading and minimum display time
            var associations = await loadingTask;
            await minDisplayTask;
            
            if (associations.Count < PageSize)
            {
                _hasMoreItems = false;
            }
            
            _logger.LogInformation("Found {Count} initial associations for category {CategoryKey} with format {NumberFormat}", 
                associations.Count, categoryKey, numberFormat);
            
            // Update UI on main thread, preserving scroll position
            await UpdateAssociationsPreservingScrollAsync(associations);
            
            // Preload images asynchronously for better perceived performance
            _ = PreloadAssociationImagesAsync(associations);
            
            _logger.LogInformation("Finished loading {Count} initial associations for category {CategoryKey}. UI collection now has {UICount} items.", associations.Count, categoryKey, Associations.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading category associations for {CategoryKey}", categoryKey);
            
            // Clear associations if they failed to load so user sees empty state rather than stale data
            MainThread.BeginInvokeOnMainThread(() => 
            {
                Associations.Clear();
            });
            
            // Show user-friendly error message
            var errorMessage = _localizationService.GetString("AssociationLoading_Failed");
            await _userNotificationService.ShowErrorToastAsync(errorMessage);
        }
        finally
        {
            IsLoading = false;
        }
    }
    
    /// <summary>
    /// Updates the associations collection while preserving scroll position
    /// </summary>
    private async Task UpdateAssociationsPreservingScrollAsync(List<TokenAssociation> newAssociations)
    {
        await Task.Run(() =>
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                // If collection is empty, just add all items
                if (Associations.Count == 0)
                {
                    foreach (var association in newAssociations)
                    {
                        Associations.Add(association);
                    }
                    return;
                }
                
                // Create lookup for new associations by token
                var newAssociationsLookup = newAssociations.ToDictionary(a => a.Token, a => a);
                
                // Replace existing items or remove if not found (in reverse order to avoid index issues)
                for (int i = Associations.Count - 1; i >= 0; i--)
                {
                    var existing = Associations[i];
                    if (newAssociationsLookup.TryGetValue(existing.Token, out var updated))
                    {
                        // Replace the item at the same index to preserve position
                        Associations[i] = updated;
                        
                        // Remove from lookup so we know it's been processed
                        newAssociationsLookup.Remove(existing.Token);
                        
                        _logger.LogInformation("Updated association: {Token} -> {Word}, IsDefault: {IsDefault}", 
                            updated.Token, updated.Word, updated.IsDefault);
                    }
                    else
                    {
                        // Remove items that are no longer present
                        Associations.RemoveAt(i);
                        _logger.LogInformation("Removed association: {Token}", existing.Token);
                    }
                }
                
                // Add any new items that weren't in the original collection
                foreach (var newAssociation in newAssociationsLookup.Values)
                {
                    Associations.Add(newAssociation);
                    _logger.LogInformation("Added new association: {Token} -> {Word}, IsDefault: {IsDefault}", 
                        newAssociation.Token, newAssociation.Word, newAssociation.IsDefault);
                }
            });
        });
    }
    
    [RelayCommand]
    private async Task ViewTokenDetailsAsync(TokenAssociation association)
    {
        if (association == null) return;
        
        try
        {
            SelectedAssociation = association;
            _logger.LogInformation("Navigating to details for token {Token} in category {Category}", 
                association.Token, association.Category);
            
            // Navigate to token details page with parameters
            var navigationParameter = $"TokenDetailPage?token={association.Token}&category={association.Category}";
            await Shell.Current.GoToAsync(navigationParameter);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error navigating to token details for {Token}", association.Token);
        }
    }
    
    public async Task SetTokenDefaultAsync(TokenAssociation association, string filename)
    {
        if (association == null || _packAssociationsService.CurrentPack == null) return;
        
        try
        {
            // Get the current default before changing it
            var currentPack = _packAssociationsService.CurrentPack;
            var previousDefault = await _tokenDefaultsService.GetEffectiveDefaultAsync(
                currentPack.Id, association.Category, association.Token, null);

            await _tokenDefaultsService.SetTokenDefaultAsync(
                currentPack.Id,
                association.Category,
                association.Token,
                filename);

            // Send message to notify other views about the default change
            var message = new TokenDefaultChangedMessage(
                currentPack.Id,
                association.Category,
                association.Token,
                filename,
                previousDefault);
            
            _messenger.Send(message);
            _logger.LogInformation("Sent TokenDefaultChangedMessage for {Token} in AssociationsViewModel", association.Token);
            
            // Smart refresh: Only update the specific token instead of full reload
            await SmartRefreshTokenAsync(association.Token, association.Category);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting token default for {Token}", association.Token);
        }
    }
    
    [RelayCommand]
    private async Task DownloadPackAsync()
    {
        try
        {
            _logger.LogInformation("User tapped download pack button, navigating to PackDownloadPage");
            
            // Track analytics for navigating to pack download
            _ = _analyticsService.TrackEventAsync(AnalyticsEvents.ButtonClicked, new Dictionary<string, object>
            {
                [AnalyticsParameters.ButtonName] = "download_pack",
                [AnalyticsParameters.SourceScreen] = "associations_empty_state"
            });
            
            // Navigate to the pack download page
            await Shell.Current.GoToAsync("PackDownloadPage");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error navigating to pack download");
            
            // Show user-friendly error message
            var errorMessage = _localizationService.GetString("Common_Error");
            await _userNotificationService.ShowErrorToastAsync(errorMessage);
        }
    }
    
    // RefreshCommand removed - page automatically refreshes on category change and pack updates

    /// <summary>
    /// Receives CustomAssociationDeletedMessage to refresh tokens list when a custom association is deleted
    /// </summary>
    public async void Receive(CustomAssociationDeletedMessage message)
    {
        try
        {
            // Only refresh if the message is for the current category
            if (SelectedCategory != null && message.Category == SelectedCategory.CategoryKey)
            {
                _logger.LogInformation("Received CustomAssociationDeletedMessage for {Token} -> {Word} (was default: {WasDefault}) in category {Category}, refreshing", 
                    message.Token, message.Word, message.WasDefault, message.Category);
                
                // Smart refresh: Only update the specific token instead of full reload
                await SmartRefreshTokenAsync(message.Token, message.Category);
            }
            else
            {
                _logger.LogDebug("Received CustomAssociationDeletedMessage for {Token} -> {Word} in category {Category}, but current category is {CurrentCategory}", 
                    message.Token, message.Word, message.Category, SelectedCategory?.CategoryKey ?? "none");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling CustomAssociationDeletedMessage for {Token} in category {Category}", 
                message.Token, message.Category);
        }
    }

    /// <summary>
    /// Receives TokenDefaultChangedMessage to refresh tokens list when a token's default association is changed
    /// </summary>
    public async void Receive(TokenDefaultChangedMessage message)
    {
        try
        {
            // Only refresh if the message is for the current category
            if (SelectedCategory != null && message.Category == SelectedCategory.CategoryKey)
            {
                _logger.LogInformation("Received TokenDefaultChangedMessage for {Token} in category {Category}, refreshing current category", 
                    message.Token, message.Category);
                
                // Smart refresh: Only update the specific token instead of full reload
                await SmartRefreshTokenAsync(message.Token, message.Category);
            }
            else
            {
                _logger.LogDebug("Received TokenDefaultChangedMessage for {Token} in category {Category}, but current category is {CurrentCategory}", 
                    message.Token, message.Category, SelectedCategory?.CategoryKey ?? "none");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling TokenDefaultChangedMessage for {Token} in category {Category}", 
                message.Token, message.Category);
        }
    }

    /// <summary>
    /// Receives PackDownloadedMessage to refresh when a new pack is downloaded
    /// </summary>
    public async void Receive(PackDownloadedMessage message)
    {
        try
        {
            _logger.LogInformation("Received PackDownloadedMessage for pack {PackName} (v{Version}), performing full refresh", 
                message.PackName, message.Version);

            // New pack downloaded - perform full refresh as structure may have changed
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                await LoadDataAsync();
            });

            // Track analytics for pack download completion
            await _analyticsService.TrackEventAsync("pack_download_ui_refreshed", new Dictionary<string, object>
            {
                ["pack_id"] = message.PackId,
                ["pack_name"] = message.PackName,
                ["language"] = message.LanguageCode,
                ["download_duration_seconds"] = message.DownloadDurationSeconds
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling PackDownloadedMessage for pack {PackId}", message.PackId);
        }
    }

    /// <summary>
    /// Receives PackUpdatedMessage to refresh when a pack is updated
    /// </summary>
    public async void Receive(PackUpdatedMessage message)
    {
        try
        {
            _logger.LogInformation("Received PackUpdatedMessage for pack {PackName} (v{PreviousVersion} → v{NewVersion}), performing smart refresh", 
                message.PackName, message.PreviousVersion, message.NewVersion);

            // Pack updated - use smart refresh to maintain user's position if possible
            await SmartRefreshAfterPackChangeAsync();

            // Track analytics for pack update completion
            await _analyticsService.TrackEventAsync("pack_update_ui_refreshed", new Dictionary<string, object>
            {
                ["pack_id"] = message.PackId,
                ["pack_name"] = message.PackName,
                ["language"] = message.LanguageCode,
                ["previous_version"] = message.PreviousVersion,
                ["new_version"] = message.NewVersion,
                ["update_duration_seconds"] = message.UpdateDurationSeconds
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling PackUpdatedMessage for pack {PackId}", message.PackId);
        }
    }

    /// <summary>
    /// Receives PackDeletedMessage to refresh when a pack is deleted
    /// </summary>
    public async void Receive(PackDeletedMessage message)
    {
        try
        {
            _logger.LogInformation("Received PackDeletedMessage for pack {PackName} (v{Version}), was last pack: {WasLastPack}", 
                message.PackName, message.Version, message.WasLastPack);

            if (message.WasLastPack)
            {
                // No packs left - clear everything
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    Categories.Clear();
                    Associations.Clear();
                    SelectedCategory = null;
                    HasPackAvailable = false;
                });
                
                _logger.LogInformation("Last pack deleted - cleared all data");
            }
            else
            {
                // Other packs still available - refresh to show remaining pack data
                await SmartRefreshAfterPackChangeAsync();
            }

            // Track analytics for pack deletion completion
            await _analyticsService.TrackEventAsync("pack_deletion_ui_refreshed", new Dictionary<string, object>
            {
                ["pack_id"] = message.PackId,
                ["pack_name"] = message.PackName,
                ["language"] = message.LanguageCode,
                ["was_last_pack"] = message.WasLastPack
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling PackDeletedMessage for pack {PackId}", message.PackId);
        }
    }
    
    /// <summary>
    /// Smart refresh for a specific token to avoid full category reload
    /// </summary>
    private async Task SmartRefreshTokenAsync(string token, string category)
    {
        try
        {
            if (SelectedCategory?.CategoryKey != category)
            {
                _logger.LogDebug("Smart refresh skipped - token {Token} not in current category {CurrentCategory}", token, SelectedCategory?.CategoryKey ?? "none");
                return;
            }

            _logger.LogInformation("Smart refreshing token {Token} in category {Category}", token, category);

            // Get current number format
            var numberFormat = _localizationService.CurrentNumberFormat;

            // Load only the specific token's associations efficiently
            var tokenAssociations = await _packAssociationsService.GetTokenAssociationsAsync(category, token);
            
            if (tokenAssociations.Count > 0)
            {
                // Get the default association for this token
                var defaultAssociation = tokenAssociations.FirstOrDefault(a => a.IsDefault) ?? tokenAssociations.First();
                
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    // Find and update the existing token association in the collection
                    var existingIndex = -1;
                    for (int i = 0; i < Associations.Count; i++)
                    {
                        if (Associations[i].Token == token)
                        {
                            existingIndex = i;
                            break;
                        }
                    }

                    if (existingIndex >= 0)
                    {
                        // Update the existing association in-place
                        Associations[existingIndex] = defaultAssociation;
                        _logger.LogInformation("Smart refreshed token {Token}: updated association to {Word} (IsDefault: {IsDefault})", 
                            token, defaultAssociation.Word, defaultAssociation.IsDefault);
                    }
                    else
                    {
                        // Token not found in current view - might be due to pagination
                        _logger.LogInformation("Token {Token} not found in current associations view, may be on different page", token);
                    }
                });
            }
            else
            {
                // Token has no associations - remove from collection if present
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    for (int i = Associations.Count - 1; i >= 0; i--)
                    {
                        if (Associations[i].Token == token)
                        {
                            Associations.RemoveAt(i);
                            _logger.LogInformation("Removed token {Token} from associations (no longer has any associations)", token);
                            break;
                        }
                    }
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in smart refresh for token {Token} in category {Category}, falling back to full reload", token, category);
            
            // Fallback to full reload if smart refresh fails
            if (SelectedCategory?.CategoryKey == category)
            {
                await LoadCategoryAssociationsAsync(category);
            }
        }
    }

    /// <summary>
    /// Optimized refresh that only reloads if necessary
    /// </summary>
    private async Task SmartRefreshCategoryAsync()
    {
        if (SelectedCategory == null)
            return;

        try
        {
            _logger.LogInformation("Smart refreshing category {Category}", SelectedCategory.CategoryKey);
            
            // Check if the pack has changed since last load
            var currentPack = _packAssociationsService.CurrentPack;
            if (currentPack == null)
            {
                // Pack is gone - clear everything
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    Categories.Clear();
                    Associations.Clear();
                    HasPackAvailable = false;
                });
                return;
            }

            // For small changes, use smart token refresh when possible
            // For now, implement basic smart refresh - more sophisticated caching could be added later
            await LoadCategoryAssociationsAsync(SelectedCategory.CategoryKey);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in smart category refresh for {Category}", SelectedCategory.CategoryKey);
            // Fallback to full reload
            await LoadDataAsync();
        }
    }

    /// <summary>
    /// Smart refresh strategy when pack changes to minimize full reloads
    /// </summary>
    private async Task SmartRefreshAfterPackChangeAsync()
    {
        try
        {
            // Check if we still have a pack available
            var hasPackNow = await _packAssociationsService.HasPackAvailableAsync();
            
            if (!hasPackNow)
            {
                // Pack is gone - clear everything efficiently
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    Categories.Clear();
                    Associations.Clear();
                    SelectedCategory = null;
                    HasPackAvailable = false;
                });
                _logger.LogInformation("Pack removed - cleared all data");
                return;
            }

            // Pack is available - check if it's actually different
            HasPackAvailable = true;
            
            // Smart strategy: Only reload if we don't have any categories yet, or if the current category is invalid
            if (Categories.Count == 0)
            {
                _logger.LogInformation("No categories loaded - performing initial load");
                await LoadDataAsync();
            }
            else if (SelectedCategory != null)
            {
                // Check if current category is still valid and refresh it
                _logger.LogInformation("Pack changed but categories exist - refreshing current category {Category}", SelectedCategory.CategoryKey);
                await SmartRefreshCategoryAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in smart refresh after pack change, falling back to full reload");
            await LoadDataAsync();
        }
    }

    /// <summary>
    /// Preloads association images asynchronously for better perceived performance
    /// </summary>
    private async Task PreloadAssociationImagesAsync(List<TokenAssociation> associations)
    {
        try
        {
            // Extract all image paths that need preloading
            var imagePaths = associations
                .SelectMany(a => new[] { a.ThumbnailPath, a.ImagePath })
                .Where(path => !string.IsNullOrEmpty(path))
                .Distinct()
                .ToList();

            if (imagePaths.Count > 0)
            {
                _logger.LogInformation("Preloading {Count} images for better performance", imagePaths.Count);
                
                // Preload in background with cancellation support
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30)); // 30 second timeout
                await _imageCacheService.PreloadImagesAsync(imagePaths, cts.Token);
                
                _logger.LogInformation("Completed preloading images for category");
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Image preloading was cancelled (timeout or app navigation)");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error during image preloading");
        }
    }

    /// <summary>
    /// Refreshes the currently selected category's associations
    /// </summary>
    public async Task RefreshCurrentCategoryAsync()
    {
        await SmartRefreshCategoryAsync();
    }
    
    private bool _hasMoreItems = true;
    private const int PageSize = 20;
    
    [RelayCommand]
    private async Task LoadMoreAsync()
    {
        if (!_hasMoreItems || IsLoading || SelectedCategory == null)
            return;
        
        try
        {
            _logger.LogInformation("Loading more associations for category {CategoryKey}, current count: {Count}", 
                SelectedCategory.CategoryKey, Associations.Count);
            
            // Get current number format
            var numberFormat = _localizationService.CurrentNumberFormat;
            
            var moreAssociations = await _packAssociationsService.LoadCategoryAssociationsAsync(
                SelectedCategory.CategoryKey, 
                Associations.Count, 
                PageSize,
                numberFormat);
            
            if (moreAssociations.Count < PageSize)
            {
                _hasMoreItems = false;
                _logger.LogInformation("Reached end of associations for category {CategoryKey}", SelectedCategory.CategoryKey);
            }
            
            // Add new associations to the existing collection
            foreach (var association in moreAssociations)
            {
                Associations.Add(association);
            }
            
            _logger.LogInformation("Loaded {NewCount} more associations, total now: {TotalCount}", 
                moreAssociations.Count, Associations.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading more associations for category {CategoryKey}", SelectedCategory?.CategoryKey);
        }
    }
    
    private async Task<string?> GetCurrentPackNameAsync()
    {
        try
        {
            return await _packAssociationsService.GetCurrentPackNameAsync();
        }
        catch
        {
            return null;
        }
    }
    
    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            _packAssociationsService.PackChanged -= OnPackChanged;
            _localizationService.NumberFormatChanged -= OnNumberFormatChanged;
        }
    }
    
}