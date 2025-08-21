using MemoApp.Core.MajorSystem.Models;
using Microsoft.Extensions.Logging;

namespace MemoApp.Core.MajorSystem.Services;

/// <summary>
/// Implementation of the Major System category service
/// </summary>
public class MajorSystemCategoryService : IMajorSystemCategoryService
{
    private readonly ILogger<MajorSystemCategoryService> _logger;
    
    // Category keys as constants
    public const string CategoryNumbers = "numbers";
    public const string CategoryLettersUpper = "letters_upper";
    public const string CategoryLettersLower = "letters_lower";
    public const string CategorySymbols = "symbols";
    public const string CategoryMonths = "months";
    
    public MajorSystemCategoryService(ILogger<MajorSystemCategoryService> logger)
    {
        _logger = logger;
    }
    
    public List<CategoryTokens> GetAllCategories()
    {
        var categories = new List<CategoryTokens>
        {
            new CategoryTokens
            {
                CategoryKey = CategoryNumbers,
                DisplayName = CategoryNumbers, // Will be localized in the UI layer
                Tokens = GetCategoryTokens(CategoryNumbers),
                SortOrder = 1
            },
            new CategoryTokens
            {
                CategoryKey = CategoryLettersUpper,
                DisplayName = CategoryLettersUpper, // Will be localized in the UI layer
                Tokens = GetCategoryTokens(CategoryLettersUpper),
                SortOrder = 2
            },
            new CategoryTokens
            {
                CategoryKey = CategoryLettersLower,
                DisplayName = CategoryLettersLower, // Will be localized in the UI layer
                Tokens = GetCategoryTokens(CategoryLettersLower),
                SortOrder = 3
            },
            new CategoryTokens
            {
                CategoryKey = CategorySymbols,
                DisplayName = CategorySymbols, // Will be localized in the UI layer
                Tokens = new List<string>(), // Dynamic, will be populated from pack
                SortOrder = 4
            },
            new CategoryTokens
            {
                CategoryKey = CategoryMonths,
                DisplayName = CategoryMonths, // Will be localized in the UI layer
                Tokens = GetCategoryTokens(CategoryMonths),
                SortOrder = 5
            }
        };
        
        return categories.OrderBy(c => c.SortOrder).ToList();
    }
    
    public List<string> GetCategoryTokens(string categoryKey)
    {
        return categoryKey switch
        {
            CategoryNumbers => GenerateNumberTokens(),
            CategoryLettersUpper => GenerateUpperLetterTokens(),
            CategoryLettersLower => GenerateLowerLetterTokens(),
            CategoryMonths => GenerateMonthTokens(),
            CategorySymbols => new List<string>(), // Dynamic from pack
            _ => new List<string>()
        };
    }
    
    public string GetCategoryDisplayName(string categoryKey)
    {
        // Return the category key - localization will be handled in the UI layer
        return categoryKey;
    }
    
    public async Task<List<string>> DiscoverDynamicTokensAsync(string packPath, string category)
    {
        var tokens = new List<string>();
        
        try
        {
            var categoryPath = Path.Combine(packPath, "thumbnails", category);
            
            if (Directory.Exists(categoryPath))
            {
                var files = Directory.GetFiles(categoryPath, "*.png");
                
                foreach (var file in files)
                {
                    var filename = Path.GetFileNameWithoutExtension(file);
                    // Extract token from filename (e.g., "@_at.png" -> "@")
                    var parts = filename.Split('_');
                    if (parts.Length > 0)
                    {
                        var token = parts[0];
                        if (!tokens.Contains(token))
                        {
                            tokens.Add(token);
                        }
                    }
                }
                
                tokens.Sort();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error discovering dynamic tokens for category {Category}", category);
        }
        
        return await Task.FromResult(tokens);
    }
    
    private List<string> GenerateNumberTokens()
    {
        var tokens = new List<string>();
        
        // For now, always use padded numbers. The UI layer can decide based on user preferences
        for (int i = 0; i <= 99; i++)
        {
            tokens.Add(i.ToString("00"));
        }
        
        return tokens;
    }
    
    private List<string> GenerateUpperLetterTokens()
    {
        var tokens = new List<string>();
        
        for (char c = 'A'; c <= 'Z'; c++)
        {
            tokens.Add(c.ToString());
        }
        
        return tokens;
    }
    
    private List<string> GenerateLowerLetterTokens()
    {
        var tokens = new List<string>();
        
        for (char c = 'a'; c <= 'z'; c++)
        {
            tokens.Add(c.ToString());
        }
        
        return tokens;
    }
    
    private List<string> GenerateMonthTokens()
    {
        var tokens = new List<string>();
        
        for (int i = 1; i <= 12; i++)
        {
            tokens.Add(i.ToString());
        }
        
        return tokens;
    }
}