using System.Linq;
using System.Text.Json;
using MemoApp.Core.MajorSystem.Models;
using MemoApp.Core.Packs;
using MemoApp.Core.Services.Packs;
using Microsoft.Extensions.Logging;

namespace MemoApp.Core.MajorSystem.Services;

/// <summary>
/// Implementation of the pack associations service
/// </summary>
public class PackAssociationsService : IPackAssociationsService
{
    private readonly IPackManagerService _packManager;
    private readonly IMajorSystemCategoryService _categoryService;
    private readonly ITokenDefaultsService _tokenDefaultsService;
    private readonly ICustomAssociationsService _customAssociationsService;
    private readonly IPackAssociationsCacheService _cacheService;
    private readonly ILogger<PackAssociationsService> _logger;
    private readonly string _appDataDirectory;
    
    private Pack? _currentPack;
    private Dictionary<string, Dictionary<string, string>>? _packDefaults;
    
    public event EventHandler? PackChanged;
    
    public Pack? CurrentPack => _currentPack;
    
    public PackAssociationsService(
        IPackManagerService packManager,
        IMajorSystemCategoryService categoryService,
        ITokenDefaultsService tokenDefaultsService,
        ICustomAssociationsService customAssociationsService,
        IPackAssociationsCacheService cacheService,
        ILogger<PackAssociationsService> logger,
        string appDataDirectory)
    {
        _packManager = packManager;
        _categoryService = categoryService;
        _tokenDefaultsService = tokenDefaultsService;
        _customAssociationsService = customAssociationsService;
        _cacheService = cacheService;
        _logger = logger;
        _appDataDirectory = appDataDirectory;
        
        // Load initial pack
        _ = Task.Run(async () => await RefreshPackDataAsync());
    }
    
    public async Task<bool> HasPackAvailableAsync()
    {
        _currentPack = await _packManager.GetCurrentPackAsync();
        _logger.LogInformation("Current pack: {Pack}", _currentPack?.Id ?? "NULL");
        if (_currentPack != null)
        {
            var packPath = await GetCurrentPackPathAsync().ConfigureAwait(false);
            _logger.LogInformation("Pack path would be: {PackPath}", packPath);
            if (!string.IsNullOrEmpty(packPath) && Directory.Exists(packPath))
            {
                _logger.LogInformation("Pack directory exists, checking for major system structure...");
                var thumbnailsPath = Path.Combine(packPath, "thumbnails");
                var imagesPath = Path.Combine(packPath, "images");
                _logger.LogInformation("Thumbnails path exists: {Exists1}, Images path exists: {Exists2}", 
                    Directory.Exists(thumbnailsPath), Directory.Exists(imagesPath));
            }
            else
            {
                _logger.LogWarning("Pack directory does not exist: {PackPath}", packPath);
            }
        }
        return _currentPack != null; // Return actual pack availability
    }
    
    public async Task<string?> GetCurrentPackPathAsync()
    {
        if (_currentPack == null) return null;
        
        // Use the pack manager to get the correct pack path, then add major_system subdirectory
        try
        {
            var packPath = await _packManager.GetPackDirectoryAsync(_currentPack.Id).ConfigureAwait(false);
            return Path.Combine(packPath, "major_system");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting pack directory for {PackId}", _currentPack.Id);
            return null;
        }
    }
    
    public async Task RefreshPackDataAsync()
    {
        try
        {
            var previousPackId = _currentPack?.Id;
            _currentPack = await _packManager.GetCurrentPackAsync();
            _packDefaults = null; // Clear cached defaults
            
            // Invalidate cache when pack changes
            if (previousPackId != null && previousPackId != _currentPack?.Id)
            {
                _cacheService.InvalidatePackCache(previousPackId);
            }
            
            if (_currentPack != null)
            {
                _packDefaults = await LoadPackDefaultsAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing pack data");
        }
    }
    
    public async Task<List<TokenAssociation>> LoadCategoryAssociationsAsync(string categoryKey)
    {
        // Load all associations without pagination
        return await LoadCategoryAssociationsAsync(categoryKey, 0, int.MaxValue);
    }

    public async Task<List<TokenAssociation>> LoadCategoryAssociationsAsync(string categoryKey, int skip, int take)
    {
        // Default to padded format for backward compatibility
        return await LoadCategoryAssociationsAsync(categoryKey, skip, take, NumberFormat.Padded);
    }

    public async Task<List<TokenAssociation>> LoadCategoryAssociationsAsync(string categoryKey, int skip, int take, NumberFormat numberFormat)
    {
        var associations = new List<TokenAssociation>();
        
        try
        {
            _logger.LogInformation("Loading associations for category: {CategoryKey} (skip: {Skip}, take: {Take}, format: {NumberFormat})", categoryKey, skip, take, numberFormat);
            
            var packPath = await GetCurrentPackPathAsync().ConfigureAwait(false);
            _logger.LogInformation("Pack path: {PackPath}", packPath ?? "NULL");
            
            if (string.IsNullOrEmpty(packPath))
            {
                _logger.LogWarning("Pack path is empty, returning test data for debugging");
                var testData = CreateTestAssociations(categoryKey);
                return testData.Skip(skip).Take(take).ToList();
            }
            
            // Check if pack has major system structure
            var thumbnailsPath = Path.Combine(packPath, "thumbnails");
            var imagesPath = Path.Combine(packPath, "images");
            
            if (!Directory.Exists(thumbnailsPath) || !Directory.Exists(imagesPath))
            {
                _logger.LogWarning("Pack {PackPath} doesn't have major system structure (thumbnails/images dirs missing), using test data", packPath);
                var testData = CreateTestAssociations(categoryKey);
                return testData.Skip(skip).Take(take).ToList();
            }
            
            var categoryThumbnailsPath = Path.Combine(thumbnailsPath, categoryKey);
            if (!Directory.Exists(categoryThumbnailsPath))
            {
                _logger.LogWarning("Category {CategoryKey} thumbnails directory doesn't exist at {Path}, using test data", categoryKey, categoryThumbnailsPath);
                var testData = CreateTestAssociations(categoryKey);
                return testData.Skip(skip).Take(take).ToList();
            }
            
            // Debug: List all files in the category directory
            var filesInCategory = ImageFormatHelper.GetImageFiles(categoryThumbnailsPath);
            _logger.LogInformation("Found {FileCount} PNG files in {CategoryKey} directory: {Files}", 
                filesInCategory.Length, categoryKey, string.Join(", ", filesInCategory.Take(10)));
            
            if (filesInCategory.Length == 0)
            {
                _logger.LogWarning("No PNG files found in {CategoryKey} thumbnails directory, using test data", categoryKey);
                var testData = CreateTestAssociations(categoryKey);
                return testData.Skip(skip).Take(take).ToList();
            }
            
            // Get tokens for the category with proper number format
            var tokens = _categoryService.GetCategoryTokens(categoryKey, numberFormat);
            _logger.LogInformation("Got {TokenCount} tokens for category {CategoryKey} with format {NumberFormat}: {Tokens}", 
                tokens.Count, categoryKey, numberFormat, string.Join(", ", tokens.Take(10)));
            
            // For symbols, discover dynamic tokens
            if (categoryKey == MajorSystemCategoryService.CategorySymbols)
            {
                tokens = await _categoryService.DiscoverDynamicTokensAsync(packPath, categoryKey);
                _logger.LogInformation("Discovered {TokenCount} dynamic tokens for symbols", tokens.Count);
            }
            
            // Apply pagination to tokens
            var pagedTokens = tokens.Skip(skip).Take(take).ToList();
            _logger.LogInformation("Processing {PagedTokenCount} tokens after pagination (skip: {Skip}, take: {Take})", 
                pagedTokens.Count, skip, take);
            
            // Load defaults using cache (fixes redundant JSON parsing performance issue)
            if (_packDefaults == null)
            {
                _packDefaults = await LoadPackDefaultsAsync();
                _logger.LogInformation("Loaded pack defaults with {CategoryCount} categories", _packDefaults.Count);
            }
            
            // Process tokens with batched file operations for better performance
            var results = await GetAssociationsForTokensBatchAsync(packPath, categoryKey, pagedTokens);
            
            // Add all associations (both found and placeholders)
            for (int i = 0; i < pagedTokens.Count; i++)
            {
                var token = pagedTokens[i];
                var association = results[i];
                
                if (association != null)
                {
                    associations.Add(association);
                    _logger.LogDebug("Added association for token {Token}: {Word}", 
                        token, association.Word);
                }
                else
                {
                    // Add placeholder for tokens without associations
                    associations.Add(new TokenAssociation
                    {
                        Token = token,
                        Category = categoryKey,
                        Word = "No association",
                        ImagePath = string.Empty,
                        ThumbnailPath = string.Empty,
                        IsDefault = false
                    });
                    _logger.LogDebug("Added placeholder for token {Token}", token);
                }
            }
            
            _logger.LogInformation("Returning {AssociationCount} associations for category {CategoryKey} (skip: {Skip}, take: {Take})", 
                associations.Count, categoryKey, skip, take);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading category associations for {Category}", categoryKey);
        }
        
        return associations;
    }
    
    public async Task<List<TokenAssociation>> GetTokenAssociationsAsync(string categoryKey, string token)
    {
        var associations = new List<TokenAssociation>();
        
        try
        {
            var packPath = await GetCurrentPackPathAsync().ConfigureAwait(false);
            if (string.IsNullOrEmpty(packPath))
            {
                return associations;
            }
            
            var thumbnailPath = Path.Combine(packPath, "thumbnails", categoryKey);
            var imagePath = Path.Combine(packPath, "images", categoryKey);
            
            if (!Directory.Exists(thumbnailPath))
            {
                return associations;
            }
            
            // Find all files for this token
            var pattern = $"{token}_*";
            var thumbnailFiles = ImageFormatHelper.GetImageFilesWithPattern(thumbnailPath, pattern);
            
            // Load defaults using cache to determine which is default
            if (_packDefaults == null)
            {
                _packDefaults = await LoadPackDefaultsAsync();
            }
            
            // Get effective default (user custom or pack default)
            string? defaultFilename = null;
            if (_currentPack != null)
            {
                defaultFilename = await _tokenDefaultsService.GetEffectiveDefaultAsync(
                    _currentPack.Id, categoryKey, token, _packDefaults);
            }
            
            foreach (var thumbnailFile in thumbnailFiles)
            {
                var filename = Path.GetFileName(thumbnailFile);
                var word = ExtractWordFromFilename(filename);
                var imageFile = Path.Combine(imagePath, filename);
                
                associations.Add(new TokenAssociation
                {
                    Token = token,
                    Category = categoryKey,
                    Word = word,
                    ThumbnailPath = thumbnailFile,
                    ImagePath = File.Exists(imageFile) ? imageFile : string.Empty,
                    IsDefault = filename == defaultFilename
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting token associations for {Category}/{Token}", categoryKey, token);
        }
        
        return associations;
    }
    
    public async Task<Dictionary<string, Dictionary<string, string>>> LoadPackDefaultsAsync()
    {
        if (_currentPack == null)
        {
            return new Dictionary<string, Dictionary<string, string>>();
        }

        // Use cache to eliminate redundant JSON parsing (critical performance fix)
        return await _cacheService.GetCachedPackDefaultsAsync(_currentPack.Id, async () =>
        {
            var defaults = new Dictionary<string, Dictionary<string, string>>();
            
            try
            {
                var packPath = await GetCurrentPackPathAsync().ConfigureAwait(false);
                if (string.IsNullOrEmpty(packPath))
                {
                    return defaults;
                }
                
                var defaultsFile = Path.Combine(packPath, "defaults.json");
                
                if (File.Exists(defaultsFile))
                {
                    var json = await File.ReadAllTextAsync(defaultsFile).ConfigureAwait(false);
                    var jsonDefaults = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, string>>>(json);
                    
                    if (jsonDefaults != null)
                    {
                        defaults = jsonDefaults;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading pack defaults");
            }
            
            return defaults;
        }).ConfigureAwait(false);
    }

    private async Task<TokenAssociation?[]> GetAssociationsForTokensBatchAsync(string packPath, string categoryKey, List<string> tokens)
    {
        try
        {
            var thumbnailPath = Path.Combine(packPath, "thumbnails", categoryKey);
            var imagePath = Path.Combine(packPath, "images", categoryKey);
            
            // Batch operation: Check directory existence once
            if (!Directory.Exists(thumbnailPath))
            {
                return new TokenAssociation?[tokens.Count];
            }
            
            // Batch operation: Get all files once instead of per-token Directory.GetFiles calls
            var allThumbnailFiles = ImageFormatHelper.GetImageFiles(thumbnailPath);
            var thumbnailFilesByToken = allThumbnailFiles
                .GroupBy(file => ExtractTokenFromFilename(Path.GetFileName(file)))
                .ToDictionary(g => g.Key, g => g.ToArray());
            
            // Batch operation: Check if image directory exists once
            var imageDirectoryExists = Directory.Exists(imagePath);
            HashSet<string>? existingImageFiles = null;
            if (imageDirectoryExists)
            {
                // Pre-load all image files to avoid individual File.Exists calls
                var imageFiles = ImageFormatHelper.GetImageFiles(imagePath);
                existingImageFiles = new HashSet<string>(imageFiles.Select(f => Path.GetFileName(f)).Where(f => f != null)!, StringComparer.OrdinalIgnoreCase);
            }
            
            // Batch operation: Get custom paths once instead of per-token calls
            string? customThumbnailBasePath = null;
            string? customImageBasePath = null;
            HashSet<string>? existingCustomThumbnails = null;
            HashSet<string>? existingCustomImages = null;
            
            if (_currentPack != null)
            {
                customThumbnailBasePath = await _customAssociationsService.GetCustomThumbnailsPathAsync(_currentPack.Id, categoryKey).ConfigureAwait(false);
                customImageBasePath = await _customAssociationsService.GetCustomImagesPathAsync(_currentPack.Id, categoryKey).ConfigureAwait(false);
                
                if (Directory.Exists(customThumbnailBasePath))
                {
                    var customThumbnailFiles = ImageFormatHelper.GetImageFiles(customThumbnailBasePath);
                    existingCustomThumbnails = new HashSet<string>(customThumbnailFiles.Select(f => Path.GetFileName(f)).Where(f => f != null)!, StringComparer.OrdinalIgnoreCase);
                }
                
                if (Directory.Exists(customImageBasePath))
                {
                    var customImageFiles = ImageFormatHelper.GetImageFiles(customImageBasePath);
                    existingCustomImages = new HashSet<string>(customImageFiles.Select(f => Path.GetFileName(f)).Where(f => f != null)!, StringComparer.OrdinalIgnoreCase);
                }
            }
            
            // Optimize parallel processing: batch all async calls first, then process in parallel
            
            // Step 1: Batch all GetEffectiveDefaultAsync calls in parallel to minimize async overhead
            Dictionary<string, string?> tokenDefaults;
            if (_currentPack != null)
            {
                var defaultTasks = tokens.Select(async token => 
                    new { Token = token, Default = await _tokenDefaultsService.GetEffectiveDefaultAsync(_currentPack.Id, categoryKey, token, _packDefaults).ConfigureAwait(false) })
                    .ToArray();
                
                var defaultResults = await Task.WhenAll(defaultTasks);
                tokenDefaults = defaultResults.ToDictionary(r => r.Token, r => r.Default);
            }
            else
            {
                tokenDefaults = new Dictionary<string, string?>();
            }
            
            // Step 2: Process all tokens in parallel using CPU-bound operations only
            var results = new TokenAssociation?[tokens.Count];
            Parallel.For(0, tokens.Count, i =>
            {
                var token = tokens[i];
                tokenDefaults.TryGetValue(token, out var defaultFilename);
                
                results[i] = GetDefaultAssociationForTokenSynchronous(
                    thumbnailPath, imagePath, categoryKey, token, defaultFilename,
                    thumbnailFilesByToken, existingImageFiles,
                    customThumbnailBasePath, customImageBasePath,
                    existingCustomThumbnails, existingCustomImages);
            });
            
            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting batched associations for {Category}", categoryKey);
            return new TokenAssociation?[tokens.Count];
        }
    }

    private async Task<TokenAssociation?> GetDefaultAssociationForTokenBatchedAsync(
        string thumbnailBasePath, string imageBasePath, string categoryKey, string token,
        Dictionary<string, string[]> thumbnailFilesByToken, HashSet<string>? existingImageFiles,
        string? customThumbnailBasePath, string? customImageBasePath,
        HashSet<string>? existingCustomThumbnails, HashSet<string>? existingCustomImages)
    {
        try
        {
            // Get effective default (user custom or pack default)
            string? defaultFilename = null;
            if (_currentPack != null)
            {
                defaultFilename = await _tokenDefaultsService.GetEffectiveDefaultAsync(
                    _currentPack.Id, categoryKey, token, _packDefaults);
            }
            
            // If there's a default, try to find it
            if (!string.IsNullOrEmpty(defaultFilename))
            {
                var thumbnailFile = Path.Combine(thumbnailBasePath, defaultFilename);
                var imageFile = Path.Combine(imageBasePath, defaultFilename);
                
                // First, check standard pack directories using batched data
                if (File.Exists(thumbnailFile)) // Still need this check as defaultFilename may not match actual files
                {
                    var word = ExtractWordFromFilename(defaultFilename);
                    
                    return new TokenAssociation
                    {
                        Token = token,
                        Category = categoryKey,
                        Word = word,
                        ThumbnailPath = thumbnailFile,
                        ImagePath = (existingImageFiles?.Contains(defaultFilename) == true) ? imageFile : string.Empty,
                        IsDefault = true,
                        IsCustom = false
                    };
                }
                
                // Check custom directories using batched data
                if (customThumbnailBasePath != null && customImageBasePath != null)
                {
                    var hasCustomThumbnail = existingCustomThumbnails?.Contains(defaultFilename) == true;
                    var hasCustomImage = existingCustomImages?.Contains(defaultFilename) == true;
                    
                    if (hasCustomThumbnail || hasCustomImage)
                    {
                        var word = ExtractWordFromFilename(defaultFilename);
                        
                        return new TokenAssociation
                        {
                            Token = token,
                            Category = categoryKey,
                            Word = word,
                            ThumbnailPath = hasCustomThumbnail ? Path.Combine(customThumbnailBasePath, defaultFilename) : string.Empty,
                            ImagePath = hasCustomImage ? Path.Combine(customImageBasePath, defaultFilename) : string.Empty,
                            IsDefault = true,
                            IsCustom = true
                        };
                    }
                }
            }
            
            // Otherwise, find the first available file for this token using batched data
            if (thumbnailFilesByToken.TryGetValue(token, out var tokenFiles) && tokenFiles.Length > 0)
            {
                var filename = Path.GetFileName(tokenFiles[0]);
                var imageFile = Path.Combine(imageBasePath, filename);
                var word = ExtractWordFromFilename(filename);
                
                return new TokenAssociation
                {
                    Token = token,
                    Category = categoryKey,
                    Word = word,
                    ThumbnailPath = tokenFiles[0],
                    ImagePath = (existingImageFiles?.Contains(filename) == true) ? imageFile : string.Empty,
                    IsDefault = false
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting batched default association for {Category}/{Token}", categoryKey, token);
        }
        
        return null;
    }
    
    private async Task<TokenAssociation?> GetDefaultAssociationForTokenAsync(string packPath, string categoryKey, string token)
    {
        try
        {
            var thumbnailPath = Path.Combine(packPath, "thumbnails", categoryKey);
            var imagePath = Path.Combine(packPath, "images", categoryKey);
            
            if (!Directory.Exists(thumbnailPath))
            {
                return null;
            }
            
            // Get effective default (user custom or pack default)
            string? defaultFilename = null;
            if (_currentPack != null)
            {
                defaultFilename = await _tokenDefaultsService.GetEffectiveDefaultAsync(
                    _currentPack.Id, categoryKey, token, _packDefaults);
            }
            
            // If there's a default, try to find it
            if (!string.IsNullOrEmpty(defaultFilename))
            {
                var thumbnailFile = Path.Combine(thumbnailPath, defaultFilename);
                var imageFile = Path.Combine(imagePath, defaultFilename);
                
                // First, check standard pack directories
                if (File.Exists(thumbnailFile))
                {
                    var word = ExtractWordFromFilename(defaultFilename);
                    
                    return new TokenAssociation
                    {
                        Token = token,
                        Category = categoryKey,
                        Word = word,
                        ThumbnailPath = thumbnailFile,
                        ImagePath = File.Exists(imageFile) ? imageFile : string.Empty,
                        IsDefault = true,
                        IsCustom = false
                    };
                }
                
                // If not found in standard directories, check custom directories
                if (_currentPack != null)
                {
                    var customThumbnailPath = await _customAssociationsService.GetCustomThumbnailsPathAsync(_currentPack.Id, categoryKey).ConfigureAwait(false);
                    var customImagePath = await _customAssociationsService.GetCustomImagesPathAsync(_currentPack.Id, categoryKey).ConfigureAwait(false);
                    
                    var customThumbnailFile = Path.Combine(customThumbnailPath, defaultFilename);
                    var customImageFile = Path.Combine(customImagePath, defaultFilename);
                    
                    if (File.Exists(customThumbnailFile) || File.Exists(customImageFile))
                    {
                        var word = ExtractWordFromFilename(defaultFilename);
                        
                        return new TokenAssociation
                        {
                            Token = token,
                            Category = categoryKey,
                            Word = word,
                            ThumbnailPath = File.Exists(customThumbnailFile) ? customThumbnailFile : string.Empty,
                            ImagePath = File.Exists(customImageFile) ? customImageFile : string.Empty,
                            IsDefault = true,
                            IsCustom = true
                        };
                    }
                }
            }
            
            // Otherwise, find the first available file for this token
            var pattern = $"{token}_*";
            var files = ImageFormatHelper.GetImageFilesWithPattern(thumbnailPath, pattern);
            
            if (files.Length > 0)
            {
                var filename = Path.GetFileName(files[0]);
                var imageFile = Path.Combine(imagePath, filename);
                var word = ExtractWordFromFilename(filename);
                
                return new TokenAssociation
                {
                    Token = token,
                    Category = categoryKey,
                    Word = word,
                    ThumbnailPath = files[0],
                    ImagePath = File.Exists(imageFile) ? imageFile : string.Empty,
                    IsDefault = false
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting default association for {Category}/{Token}", categoryKey, token);
        }
        
        return null;
    }

    private TokenAssociation? GetDefaultAssociationForTokenSynchronous(
        string thumbnailBasePath, string imageBasePath, string categoryKey, string token, string? defaultFilename,
        Dictionary<string, string[]> thumbnailFilesByToken, HashSet<string>? existingImageFiles,
        string? customThumbnailBasePath, string? customImageBasePath,
        HashSet<string>? existingCustomThumbnails, HashSet<string>? existingCustomImages)
    {
        try
        {
            // If there's a default, try to find it
            if (!string.IsNullOrEmpty(defaultFilename))
            {
                var thumbnailFile = Path.Combine(thumbnailBasePath, defaultFilename);
                var imageFile = Path.Combine(imageBasePath, defaultFilename);
                
                // First, check standard pack directories using batched data
                if (File.Exists(thumbnailFile)) // Still need this check as defaultFilename may not match actual files
                {
                    var word = ExtractWordFromFilename(defaultFilename);
                    
                    return new TokenAssociation
                    {
                        Token = token,
                        Category = categoryKey,
                        Word = word,
                        ThumbnailPath = thumbnailFile,
                        ImagePath = (existingImageFiles?.Contains(defaultFilename) == true) ? imageFile : string.Empty,
                        IsDefault = true,
                        IsCustom = false
                    };
                }
                
                // Check custom directories using batched data
                if (customThumbnailBasePath != null && customImageBasePath != null)
                {
                    var hasCustomThumbnail = existingCustomThumbnails?.Contains(defaultFilename) == true;
                    var hasCustomImage = existingCustomImages?.Contains(defaultFilename) == true;
                    
                    if (hasCustomThumbnail || hasCustomImage)
                    {
                        var word = ExtractWordFromFilename(defaultFilename);
                        
                        return new TokenAssociation
                        {
                            Token = token,
                            Category = categoryKey,
                            Word = word,
                            ThumbnailPath = hasCustomThumbnail ? Path.Combine(customThumbnailBasePath, defaultFilename) : string.Empty,
                            ImagePath = hasCustomImage ? Path.Combine(customImageBasePath, defaultFilename) : string.Empty,
                            IsDefault = true,
                            IsCustom = true
                        };
                    }
                }
            }
            
            // Otherwise, find the first available file for this token using batched data
            if (thumbnailFilesByToken.TryGetValue(token, out var tokenFiles) && tokenFiles.Length > 0)
            {
                var filename = Path.GetFileName(tokenFiles[0]);
                var imageFile = Path.Combine(imageBasePath, filename);
                var word = ExtractWordFromFilename(filename);
                
                return new TokenAssociation
                {
                    Token = token,
                    Category = categoryKey,
                    Word = word,
                    ThumbnailPath = tokenFiles[0],
                    ImagePath = (existingImageFiles?.Contains(filename) == true) ? imageFile : string.Empty,
                    IsDefault = false
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting synchronous default association for {Category}/{Token}", categoryKey, token);
        }
        
        return null;
    }
    
    private string ExtractTokenFromFilename(string filename)
    {
        try
        {
            // Remove extension
            var nameWithoutExt = Path.GetFileNameWithoutExtension(filename);
            
            // Split by underscore and take the first part (token)
            var parts = nameWithoutExt.Split('_');
            if (parts.Length > 0)
            {
                return parts[0];
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting token from filename: {Filename}", filename);
        }
        
        return string.Empty;
    }

    private string ExtractWordFromFilename(string filename)
    {
        try
        {
            // Remove extension
            var nameWithoutExt = Path.GetFileNameWithoutExtension(filename);

            // Split by underscore
            var parts = nameWithoutExt.Split('_');
            if (parts.Length > 1)
            {
                // Check if this is a custom association filename with timestamp
                // Custom association format: {token}_{word}_{timestamp}
                // Pack association format: {token}_{word}
                if (parts.Length >= 3)
                {
                    // Check if the last part looks like a timestamp (14 digits: yyyyMMddHHmmss)
                    var lastPart = parts[^1];
                    if (lastPart.Length == 14 && lastPart.All(char.IsDigit))
                    {
                        // This is a custom association filename, extract word without timestamp
                        return string.Join("_", parts.Skip(1).Take(parts.Length - 2));
                    }
                }

                // Standard pack association or custom association without valid timestamp
                return string.Join("_", parts.Skip(1));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting word from filename: {Filename}", filename);
        }

        return string.Empty;
    }
    
    private List<TokenAssociation> CreateTestAssociations(string categoryKey)
    {
        _logger.LogInformation("Creating test data for category: {CategoryKey}", categoryKey);
        
        var associations = new List<TokenAssociation>();
        
        // Create limited test data for each category
        switch (categoryKey)
        {
            case MajorSystemCategoryService.CategoryNumbers:
                for (int i = 0; i < 10; i++) // Just first 10 numbers for testing
                {
                    associations.Add(new TokenAssociation
                    {
                        Token = i.ToString("00"),
                        Category = categoryKey,
                        Word = $"Number {i:00}",
                        ImagePath = string.Empty,
                        ThumbnailPath = string.Empty,
                        IsDefault = i == 0
                    });
                }
                break;
                
            case MajorSystemCategoryService.CategoryLettersUpper:
                var upperLetters = new[] { "A", "B", "C", "D", "E" };
                foreach (var letter in upperLetters)
                {
                    associations.Add(new TokenAssociation
                    {
                        Token = letter,
                        Category = categoryKey,
                        Word = $"Letter {letter}",
                        ImagePath = string.Empty,
                        ThumbnailPath = string.Empty,
                        IsDefault = letter == "A"
                    });
                }
                break;
                
            case MajorSystemCategoryService.CategoryLettersLower:
                var lowerLetters = new[] { "a", "b", "c", "d", "e" };
                foreach (var letter in lowerLetters)
                {
                    associations.Add(new TokenAssociation
                    {
                        Token = letter,
                        Category = categoryKey,
                        Word = $"Letter {letter}",
                        ImagePath = string.Empty,
                        ThumbnailPath = string.Empty,
                        IsDefault = letter == "a"
                    });
                }
                break;
                
            case MajorSystemCategoryService.CategorySymbols:
                var symbols = new[] { "@", "#", "$", "&", "%" };
                foreach (var symbol in symbols)
                {
                    associations.Add(new TokenAssociation
                    {
                        Token = symbol,
                        Category = categoryKey,
                        Word = $"Symbol {symbol}",
                        ImagePath = string.Empty,
                        ThumbnailPath = string.Empty,
                        IsDefault = symbol == "@"
                    });
                }
                break;
                
            case MajorSystemCategoryService.CategoryMonths:
                var months = new[] { "January", "February", "March", "April", "May" };
                for (int i = 0; i < months.Length; i++)
                {
                    associations.Add(new TokenAssociation
                    {
                        Token = (i + 1).ToString(),
                        Category = categoryKey,
                        Word = months[i],
                        ImagePath = string.Empty,
                        ThumbnailPath = string.Empty,
                        IsDefault = i == 0
                    });
                }
                break;
        }
        
        _logger.LogInformation("Created {Count} test associations for category {CategoryKey}", associations.Count, categoryKey);
        return associations;
    }
    
    public async Task<string?> GetCurrentPackNameAsync()
    {
        await Task.CompletedTask; // Make it async for consistency
        return _currentPack?.GetDisplayName();
    }
}