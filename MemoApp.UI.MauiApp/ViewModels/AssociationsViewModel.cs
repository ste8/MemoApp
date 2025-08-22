using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MemoApp.Core.MajorSystem.Models;
using MemoApp.Core.MajorSystem.Services;
using MemoApp.Localization.Services;
using Microsoft.Extensions.Logging;

namespace MemoApp.UI.MauiApp.ViewModels;

/// <summary>
/// ViewModel for the Associations page
/// </summary>
public partial class AssociationsViewModel : ObservableObject
{
    private readonly IMajorSystemCategoryService _categoryService;
    private readonly IPackAssociationsService _packAssociationsService;
    private readonly ITokenDefaultsService _tokenDefaultsService;
    private readonly ILocalizationService _localizationService;
    private readonly ILogger<AssociationsViewModel> _logger;
    
    [ObservableProperty]
    private bool _isLoading;
    
    [ObservableProperty]
    private bool _hasPackAvailable;
    
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
    
    public AssociationsViewModel(
        IMajorSystemCategoryService categoryService,
        IPackAssociationsService packAssociationsService,
        ITokenDefaultsService tokenDefaultsService,
        ILocalizationService localizationService,
        ILogger<AssociationsViewModel> logger)
    {
        _categoryService = categoryService;
        _packAssociationsService = packAssociationsService;
        _tokenDefaultsService = tokenDefaultsService;
        _localizationService = localizationService;
        _logger = logger;
        
        // Subscribe to pack changes
        _packAssociationsService.PackChanged += OnPackChanged;
        
        // Subscribe to token default changes for smart refresh
        _tokenDefaultsService.TokenDefaultChanged += OnTokenDefaultChanged;
        
        // Initialize localized strings
        UpdateLocalizedStrings();
        
        // Load initial data - don't block constructor
        _ = LoadDataAsync();
    }
    
    private void OnPackChanged(object? sender, EventArgs e)
    {
        _ = Task.Run(async () => await LoadDataAsync());
    }
    
    private void OnTokenDefaultChanged(object? sender, Core.MajorSystem.Models.TokenDefaultChangedEventArgs e)
    {
        // Only refresh if the changed token is in the currently selected category
        if (SelectedCategory != null && SelectedCategory.CategoryKey == e.Category)
        {
            _logger.LogInformation("Token default changed for {Category}/{Token}, refreshing current category", e.Category, e.Token);
            // Run on main thread to ensure IsLoading updates are visible
            MainThread.BeginInvokeOnMainThread(async () => await RefreshCurrentCategoryAsync());
        }
        else
        {
            _logger.LogDebug("Token default changed for {Category}/{Token}, but not current category {CurrentCategory}", 
                e.Category, e.Token, SelectedCategory?.CategoryKey ?? "none");
        }
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
        }
        finally
        {
            IsLoading = false;
            _logger.LogInformation("Finished loading associations data");
        }
    }
    
    private async Task LoadCategoriesAsync()
    {
        try
        {
            var categories = _categoryService.GetAllCategories();
            
            // Localize display names
            foreach (var category in categories)
            {
                category.DisplayName = GetLocalizedCategoryName(category.CategoryKey);
            }
            
            // For symbols category, load dynamic tokens if pack is available
            var symbolsCategory = categories.FirstOrDefault(c => c.CategoryKey == MajorSystemCategoryService.CategorySymbols);
            if (symbolsCategory != null && _packAssociationsService.CurrentPack != null)
            {
                var packPath = _packAssociationsService.GetCurrentPackPath();
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
            
            // Start loading task and minimum display time task concurrently
            var loadingTask = Task.Run(async () => 
                await _packAssociationsService.LoadCategoryAssociationsAsync(categoryKey, 0, PageSize));
            var minDisplayTask = Task.Delay(300); // Reduced to 300ms for better perceived performance
            
            // Wait for both data loading and minimum display time
            var associations = await loadingTask;
            await minDisplayTask;
            
            if (associations.Count < PageSize)
            {
                _hasMoreItems = false;
            }
            
            _logger.LogInformation("Found {Count} initial associations for category {CategoryKey}", associations.Count, categoryKey);
            
            // Update UI on main thread, preserving scroll position
            await UpdateAssociationsPreservingScrollAsync(associations);
            _logger.LogInformation("Finished loading {Count} initial associations for category {CategoryKey}. UI collection now has {UICount} items.", associations.Count, categoryKey, Associations.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading category associations for {CategoryKey}", categoryKey);
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
            await _tokenDefaultsService.SetTokenDefaultAsync(
                _packAssociationsService.CurrentPack.Id,
                association.Category,
                association.Token,
                filename);
            
            // Refresh the current category to show updated default
            if (SelectedCategory != null)
            {
                await LoadCategoryAssociationsAsync(SelectedCategory.CategoryKey);
            }
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
            // Navigate to pack download page (implementation depends on navigation service)
            _logger.LogInformation("Navigating to pack download");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error navigating to pack download");
        }
    }
    
    [RelayCommand]
    private async Task RefreshAsync()
    {
        await LoadDataAsync();
    }
    
    /// <summary>
    /// Refreshes the currently selected category's associations
    /// </summary>
    public async Task RefreshCurrentCategoryAsync()
    {
        if (SelectedCategory != null)
        {
            await LoadCategoryAssociationsAsync(SelectedCategory.CategoryKey);
        }
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
            
            var moreAssociations = await _packAssociationsService.LoadCategoryAssociationsAsync(
                SelectedCategory.CategoryKey, 
                Associations.Count, 
                PageSize);
            
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
    
    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            _packAssociationsService.PackChanged -= OnPackChanged;
            _tokenDefaultsService.TokenDefaultChanged -= OnTokenDefaultChanged;
        }
    }
}