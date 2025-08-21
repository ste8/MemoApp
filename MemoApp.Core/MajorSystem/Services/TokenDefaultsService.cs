using System.Text.Json;
using MemoApp.Core.MajorSystem.Models;
using Microsoft.Extensions.Logging;

namespace MemoApp.Core.MajorSystem.Services;

/// <summary>
/// Implementation of the token defaults service
/// </summary>
public class TokenDefaultsService : ITokenDefaultsService
{
    private readonly ILogger<TokenDefaultsService> _logger;
    private readonly string _appDataDirectory;
    
    private const string UserDefaultsFileName = "user_defaults.json";
    
    /// <summary>
    /// Event fired when a token default is changed
    /// </summary>
    public event EventHandler<TokenDefaultChangedEventArgs>? TokenDefaultChanged;
    
    public TokenDefaultsService(
        ILogger<TokenDefaultsService> logger,
        string appDataDirectory)
    {
        _logger = logger;
        _appDataDirectory = appDataDirectory;
    }
    
    public async Task<UserDefaultPreferences> GetUserDefaultsAsync(string packId)
    {
        try
        {
            var filePath = GetUserDefaultsFilePath(packId);
            
            if (File.Exists(filePath))
            {
                var json = await File.ReadAllTextAsync(filePath);
                var preferences = JsonSerializer.Deserialize<UserDefaultPreferences>(json);
                
                if (preferences != null)
                {
                    return preferences;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading user defaults for pack {PackId}", packId);
        }
        
        // Return empty preferences if file doesn't exist or error occurred
        return new UserDefaultPreferences
        {
            PackId = packId
        };
    }
    
    public async Task SaveUserDefaultsAsync(UserDefaultPreferences preferences)
    {
        try
        {
            var filePath = GetUserDefaultsFilePath(preferences.PackId);
            var directory = Path.GetDirectoryName(filePath);
            
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            
            var options = new JsonSerializerOptions
            {
                WriteIndented = true
            };
            
            var json = JsonSerializer.Serialize(preferences, options);
            await File.WriteAllTextAsync(filePath, json);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving user defaults for pack {PackId}", preferences.PackId);
            throw;
        }
    }
    
    public async Task SetTokenDefaultAsync(string packId, string category, string token, string filename)
    {
        try
        {
            var preferences = await GetUserDefaultsAsync(packId);
            preferences.SetDefault(category, token, filename);
            await SaveUserDefaultsAsync(preferences);
            
            // Fire event to notify listeners that a default has changed
            TokenDefaultChanged?.Invoke(this, new TokenDefaultChangedEventArgs
            {
                PackId = packId,
                Category = category,
                Token = token,
                NewDefault = filename
            });
            
            _logger.LogInformation("Token default changed and event fired for {PackId}/{Category}/{Token}", packId, category, token);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting token default for {PackId}/{Category}/{Token}", packId, category, token);
            throw;
        }
    }
    
    public async Task<string?> GetEffectiveDefaultAsync(string packId, string category, string token, Dictionary<string, Dictionary<string, string>>? packDefaults)
    {
        try
        {
            // First, check if user has a custom default
            var userPreferences = await GetUserDefaultsAsync(packId);
            var userDefault = userPreferences.GetDefault(category, token);
            
            if (!string.IsNullOrEmpty(userDefault))
            {
                return userDefault;
            }
            
            // Fall back to pack default
            if (packDefaults?.TryGetValue(category, out var categoryDefaults) == true)
            {
                if (categoryDefaults.TryGetValue(token, out var packDefault))
                {
                    return packDefault;
                }
            }
            
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting effective default for {PackId}/{Category}/{Token}", packId, category, token);
            return null;
        }
    }
    
    public async Task ClearCategoryDefaultsAsync(string packId, string category)
    {
        try
        {
            var preferences = await GetUserDefaultsAsync(packId);
            preferences.ClearCategoryDefaults(category);
            await SaveUserDefaultsAsync(preferences);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing category defaults for {PackId}/{Category}", packId, category);
            throw;
        }
    }
    
    public Task ClearPackDefaultsAsync(string packId)
    {
        try
        {
            var filePath = GetUserDefaultsFilePath(packId);
            
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
            
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing pack defaults for {PackId}", packId);
            throw;
        }
    }
    
    private string GetUserDefaultsFilePath(string packId)
    {
        return Path.Combine(_appDataDirectory, "major_systems", packId, UserDefaultsFileName);
    }
}