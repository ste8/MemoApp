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
    
    private CategoryTokens? _selectedCategory;
    
    public CategoryTokens? SelectedCategory 
    { 
        get => _selectedCategory;
        set
        {
            if (SetProperty(ref _selectedCategory, value) && value != null)
            {
                // Automatically load associations when category is selected
                _ = Task.Run(async () => await SelectCategoryAsync(value));
            }
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
        
        // Initialize localized strings
        UpdateLocalizedStrings();
        
        // Load initial data - don't block constructor
        _ = LoadDataAsync();
    }
    
    private void OnPackChanged(object? sender, EventArgs e)
    {
        _ = Task.Run(async () => await LoadDataAsync());
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
    private async Task SelectCategoryAsync(CategoryTokens category)
    {
        if (category == null || category == SelectedCategory) 
            return;
        
        try
        {
            IsLoading = true;
            SelectedCategory = category;
            _logger.LogInformation("User selected category: {CategoryKey}", category.CategoryKey);
            await LoadCategoryAssociationsAsync(category.CategoryKey);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error selecting category {CategoryKey}", category?.CategoryKey ?? "NULL");
        }
        finally
        {
            IsLoading = false;
        }
    }
    
    private async Task LoadCategoryAssociationsAsync(string categoryKey)
    {
        try
        {
            _logger.LogInformation("Loading associations for category: {CategoryKey}", categoryKey);
            
            // Run the heavy data loading on background thread
            var associations = await Task.Run(async () => 
                await _packAssociationsService.LoadCategoryAssociationsAsync(categoryKey));
            
            _logger.LogInformation("Found {Count} associations for category {CategoryKey}", associations.Count, categoryKey);
            
            // Update UI on main thread
            Associations.Clear();
            foreach (var association in associations)
            {
                Associations.Add(association);
                _logger.LogInformation("Added association to UI: {Token} -> {Word}, ThumbnailPath: {ThumbnailPath}", 
                    association.Token, association.Word, association.ThumbnailPath ?? "NULL");
            }
            _logger.LogInformation("Finished loading {Count} associations for category {CategoryKey}. UI collection now has {UICount} items.", associations.Count, categoryKey, Associations.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading category associations for {CategoryKey}", categoryKey);
        }
    }
    
    [RelayCommand]
    private async Task ViewTokenDetailsAsync(TokenAssociation association)
    {
        if (association == null) return;
        
        try
        {
            SelectedAssociation = association;
            
            // Load all available associations for this token
            var tokenAssociations = await _packAssociationsService.GetTokenAssociationsAsync(
                association.Category, association.Token);
            
            // Navigate to token details page (implementation depends on navigation service)
            // For now, just log the action
            _logger.LogInformation("Viewing details for token {Token} in category {Category}", 
                association.Token, association.Category);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error viewing token details for {Token}", association.Token);
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
    
    [RelayCommand]
    private Task LoadMoreAsync()
    {
        // For now, do nothing - all data is loaded at once
        // Could implement pagination in the future
        return Task.CompletedTask;
    }
    
    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            _packAssociationsService.PackChanged -= OnPackChanged;
        }
    }
}