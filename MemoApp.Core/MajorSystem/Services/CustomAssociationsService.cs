using MemoApp.Core.MajorSystem.Models;
using MemoApp.Core.Services.Packs;
using Microsoft.Extensions.Logging;

namespace MemoApp.Core.MajorSystem.Services;

/// <summary>
/// Implementation of the custom associations service
/// </summary>
public class CustomAssociationsService : ICustomAssociationsService
{
    private readonly IPackManagerService _packManager;
    private readonly IImageProcessingService _imageProcessor;
    private readonly ITokenDefaultsService _tokenDefaultsService;
    private readonly ILogger<CustomAssociationsService> _logger;

    public CustomAssociationsService(
        IPackManagerService packManager,
        IImageProcessingService imageProcessor,
        ITokenDefaultsService tokenDefaultsService,
        ILogger<CustomAssociationsService> logger)
    {
        _packManager = packManager;
        _imageProcessor = imageProcessor;
        _tokenDefaultsService = tokenDefaultsService;
        _logger = logger;
    }

    public async Task<TokenAssociation> CreateCustomAssociationAsync(
        string packId, 
        string categoryKey, 
        string token, 
        string word, 
        Stream imageStream, 
        bool setAsDefault = true)
    {
        try
        {
            _logger.LogInformation("Creating custom association for {PackId}/{Category}/{Token} with word '{Word}'", 
                packId, categoryKey, token, word);

            // Ensure custom directories exist
            await EnsureCustomDirectoriesAsync(packId, categoryKey);

            // Generate unique filename with timestamp
            var timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
            var filename = $"{token}_{word}_{timestamp}.png";

            var customImagesPath = GetCustomImagesPath(packId, categoryKey);
            var customThumbnailsPath = GetCustomThumbnailsPath(packId, categoryKey);

            var imagePath = Path.Combine(customImagesPath, filename);
            var thumbnailPath = Path.Combine(customThumbnailsPath, filename);

            // Save the original image
            await SaveImageStreamAsync(imageStream, imagePath);
            _logger.LogInformation("Saved image to: {Path} (exists: {Exists})", imagePath, File.Exists(imagePath));

            // Generate and save thumbnail
            await _imageProcessor.GenerateThumbnailAsync(imagePath, thumbnailPath);
            _logger.LogInformation("Generated thumbnail to: {Path} (exists: {Exists})", thumbnailPath, File.Exists(thumbnailPath));

            // Create the association object
            var association = new TokenAssociation
            {
                Token = token,
                Category = categoryKey,
                Word = word,
                ImagePath = imagePath,
                ThumbnailPath = thumbnailPath,
                IsDefault = setAsDefault,
                IsCustom = true
            };

            // Set as default if requested
            if (setAsDefault)
            {
                await _tokenDefaultsService.SetTokenDefaultAsync(packId, categoryKey, token, filename);
            }

            _logger.LogInformation("Successfully created custom association {Token} -> {Word} at {Path}", 
                token, word, imagePath);

            return association;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating custom association for {PackId}/{Category}/{Token}", 
                packId, categoryKey, token);
            throw;
        }
    }

    public async Task<List<TokenAssociation>> GetCustomAssociationsAsync(string packId, string categoryKey)
    {
        var associations = new List<TokenAssociation>();

        try
        {
            var customImagesPath = GetCustomImagesPath(packId, categoryKey);
            if (!Directory.Exists(customImagesPath))
            {
                return associations;
            }

            var imageFiles = Directory.GetFiles(customImagesPath, "*.png");
            var customThumbnailsPath = GetCustomThumbnailsPath(packId, categoryKey);
            
            _logger.LogInformation("Looking for custom images in: {Path}", customImagesPath);
            _logger.LogInformation("Found {Count} image files: {Files}", imageFiles.Length, string.Join(", ", imageFiles.Select(Path.GetFileName)));

            foreach (var imageFile in imageFiles)
            {
                var filename = Path.GetFileName(imageFile);
                var thumbnailFile = Path.Combine(customThumbnailsPath, filename);

                var (token, word) = ParseFilename(filename);
                if (!string.IsNullOrEmpty(token) && !string.IsNullOrEmpty(word))
                {
                    associations.Add(new TokenAssociation
                    {
                        Token = token,
                        Category = categoryKey,
                        Word = word,
                        ImagePath = imageFile,
                        ThumbnailPath = File.Exists(thumbnailFile) ? thumbnailFile : string.Empty,
                        IsDefault = false, // Will be set by calling service based on defaults
                        IsCustom = true
                    });
                }
            }

            _logger.LogInformation("Found {Count} custom associations for {PackId}/{Category}", 
                associations.Count, packId, categoryKey);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading custom associations for {PackId}/{Category}", packId, categoryKey);
        }

        return associations;
    }

    public async Task<List<TokenAssociation>> GetCustomAssociationsForTokenAsync(string packId, string categoryKey, string token)
    {
        var allCustom = await GetCustomAssociationsAsync(packId, categoryKey);
        return allCustom.Where(a => a.Token == token).ToList();
    }

    public async Task<bool> DeleteCustomAssociationAsync(TokenAssociation association)
    {
        try
        {
            if (!association.IsCustom)
            {
                _logger.LogWarning("Attempted to delete non-custom association: {Token} -> {Word}", 
                    association.Token, association.Word);
                return false;
            }

            _logger.LogInformation("Deleting custom association: {Token} -> {Word}", 
                association.Token, association.Word);

            // Delete image file
            if (!string.IsNullOrEmpty(association.ImagePath) && File.Exists(association.ImagePath))
            {
                File.Delete(association.ImagePath);
                _logger.LogDebug("Deleted image file: {Path}", association.ImagePath);
            }

            // Delete thumbnail file
            if (!string.IsNullOrEmpty(association.ThumbnailPath) && File.Exists(association.ThumbnailPath))
            {
                File.Delete(association.ThumbnailPath);
                _logger.LogDebug("Deleted thumbnail file: {Path}", association.ThumbnailPath);
            }

            // If this was the default, we need to clear the default (the calling service should handle this)
            // The TokenDefaultsService will handle setting a new default if needed

            _logger.LogInformation("Successfully deleted custom association: {Token} -> {Word}", 
                association.Token, association.Word);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting custom association: {Token} -> {Word}", 
                association.Token, association.Word);
            return false;
        }
    }

    public async Task<bool> EnsureCustomDirectoriesAsync(string packId, string categoryKey)
    {
        try
        {
            var customImagesPath = GetCustomImagesPath(packId, categoryKey);
            var customThumbnailsPath = GetCustomThumbnailsPath(packId, categoryKey);

            if (!Directory.Exists(customImagesPath))
            {
                Directory.CreateDirectory(customImagesPath);
                _logger.LogInformation("Created custom images directory: {Path}", customImagesPath);
            }

            if (!Directory.Exists(customThumbnailsPath))
            {
                Directory.CreateDirectory(customThumbnailsPath);
                _logger.LogInformation("Created custom thumbnails directory: {Path}", customThumbnailsPath);
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error ensuring custom directories for {PackId}/{Category}", packId, categoryKey);
            return false;
        }
    }

    public string GetCustomImagesPath(string packId, string categoryKey)
    {
        var packPath = _packManager.GetPackDirectoryAsync(packId).Result;
        return Path.Combine(packPath, "major_system", "custom", "images", categoryKey);
    }

    public string GetCustomThumbnailsPath(string packId, string categoryKey)
    {
        var packPath = _packManager.GetPackDirectoryAsync(packId).Result;
        return Path.Combine(packPath, "major_system", "custom", "thumbnails", categoryKey);
    }

    private async Task SaveImageStreamAsync(Stream imageStream, string filePath)
    {
        using var fileStream = File.Create(filePath);
        imageStream.Seek(0, SeekOrigin.Begin);
        await imageStream.CopyToAsync(fileStream);
        await fileStream.FlushAsync();
    }

    private (string token, string word) ParseFilename(string filename)
    {
        try
        {
            // Remove extension
            var nameWithoutExt = Path.GetFileNameWithoutExtension(filename);
            
            // Split by underscore: {token}_{word}_{timestamp}
            var parts = nameWithoutExt.Split('_');
            if (parts.Length >= 3)
            {
                var token = parts[0];
                // Word is everything between token and timestamp (last part)
                var word = string.Join("_", parts.Skip(1).Take(parts.Length - 2));
                return (token, word);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing filename: {Filename}", filename);
        }
        
        return (string.Empty, string.Empty);
    }
}