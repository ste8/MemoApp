using System.Text.Json;
using MemoApp.Localization.Services;
using MemoApp.UI.MauiApp.Models.Freemium;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace MemoApp.UI.MauiApp.Services.Freemium;

public class PromotionService : IPromotionService, IDisposable
{
    private readonly ILogger<PromotionService> _logger;
    private readonly IConfiguration _configuration;
    private readonly ILocalizationService _localizationService;
    private readonly HttpClient _httpClient;
    private readonly List<AchievementMilestone> _milestones;
    private PromotionData? _cachedServerPromotion;
    private bool _localPromotionsPaused = false;
    private bool _disposed = false;
    
    public PromotionService(
        ILogger<PromotionService> logger,
        IConfiguration configuration,
        ILocalizationService localizationService)
    {
        _logger = logger;
        _configuration = configuration;
        _localizationService = localizationService;
        _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
        _milestones = AchievementMilestone.GetDefaultMilestones();
        
        _ = LoadMilestoneProgressAsync();
    }
    
    public async Task<PromotionData?> GetActivePromotionAsync()
    {
        try
        {
            // Get both server and local promotions
            var serverPromotion = await GetServerPromotionAsync();
            var localPromotion = await GetLocalPromotionAsync();
            
            // If local promotions are paused, only return server promotion
            if (_localPromotionsPaused)
                return serverPromotion;
            
            // If both exist, return the one with higher discount
            if (serverPromotion != null && localPromotion != null)
            {
                var maxServerDiscount = serverPromotion.Discounts.Values.DefaultIfEmpty(0).Max();
                var maxLocalDiscount = localPromotion.Discounts.Values.DefaultIfEmpty(0).Max();
                
                return maxServerDiscount >= maxLocalDiscount ? serverPromotion : localPromotion;
            }
            
            // Return whichever is available
            return serverPromotion ?? localPromotion;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting active promotion");
            return null;
        }
    }
    
    public async Task<AchievementMilestone?> GetCurrentAchievementMilestoneAsync()
    {
        var exerciseCount = await GetTotalExerciseCountAsync();
        
        foreach (var milestone in _milestones.OrderBy(m => m.ExerciseCountRequired))
        {
            if (!milestone.WasCompleted && exerciseCount >= milestone.ExerciseCountRequired)
            {
                // Check if expired
                if (milestone.HasExpired)
                {
                    milestone.WasCompleted = true;
                    await SaveMilestoneProgressAsync();
                    continue;
                }
                
                // Mark as shown if first time
                if (!milestone.FirstShownDate.HasValue)
                {
                    milestone.FirstShownDate = DateTime.UtcNow;
                    await SaveMilestoneProgressAsync();
                }
                
                return milestone;
            }
        }
        
        return null;
    }
    
    public async Task UpdateExerciseCountAsync()
    {
        var count = await GetTotalExerciseCountAsync();
        count++;
        Preferences.Set("total_exercise_count", count);
        await Task.CompletedTask;
    }
    
    public async Task<int> GetTotalExerciseCountAsync()
    {
        return await Task.FromResult(Preferences.Get("total_exercise_count", 0));
    }
    
    public async Task<List<SubscriptionOption>> ApplyPromotionsToSubscriptionsAsync(List<SubscriptionOption> subscriptions)
    {
        var promotion = await GetActivePromotionAsync();
        
        if (promotion == null || !promotion.IsActive)
            return subscriptions;
        
        foreach (var subscription in subscriptions)
        {
            var tierKey = subscription.Id switch
            {
                var id when id.Contains("monthly") => "monthly",
                var id when id.Contains("yearly") => "yearly",
                var id when id.Contains("lifetime") => "lifetime",
                _ => null
            };
            
            if (tierKey != null && promotion.Discounts.TryGetValue(tierKey, out var discount))
            {
                subscription.DiscountPercentage = discount;
                
                // Calculate discounted price
                if (TryParsePrice(subscription.LocalizedPrice, out var originalPrice, out var currency))
                {
                    var discountedPrice = originalPrice * (1 - discount / 100.0m);
                    subscription.DisplayPrice = FormatPrice(discountedPrice, currency);
                }
            }
        }
        
        return subscriptions;
    }
    
    public void PauseLocalPromotionTimers()
    {
        _localPromotionsPaused = true;
    }
    
    public void ResumeLocalPromotionTimers()
    {
        _localPromotionsPaused = false;
    }
    
    private async Task<PromotionData?> GetServerPromotionAsync()
    {
        try
        {
            // Check cache first
            if (_cachedServerPromotion != null && 
                (DateTime.UtcNow - _cachedServerPromotion.CacheTimestamp).TotalHours < 24)
            {
                return _cachedServerPromotion;
            }
            
            var apiUrl = _configuration["Freemium:PromotionApiUrl"];
            if (string.IsNullOrEmpty(apiUrl))
                return null;
            
            var response = await _httpClient.GetAsync(apiUrl);
            if (!response.IsSuccessStatusCode)
                return _cachedServerPromotion; // Return cached if API fails
            
            var json = await response.Content.ReadAsStringAsync();
            var promotion = JsonSerializer.Deserialize<PromotionData>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
            
            if (promotion != null)
            {
                promotion.CacheTimestamp = DateTime.UtcNow;
                promotion.IsLocal = false;
                _cachedServerPromotion = promotion;
                
                // Pause local promotions if server promotion is active
                if (promotion.IsActive)
                    PauseLocalPromotionTimers();
                else
                    ResumeLocalPromotionTimers();
            }
            
            return promotion;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching server promotion");
            return _cachedServerPromotion; // Return cached on error
        }
    }
    
    private async Task<PromotionData?> GetLocalPromotionAsync()
    {
        if (_localPromotionsPaused)
            return null;
        
        var milestone = await GetCurrentAchievementMilestoneAsync();
        
        if (milestone == null || milestone.HasExpired)
            return null;
        
        var currentLanguage = _localizationService.GetCurrentLanguage();
        
        return new PromotionData
        {
            IsActive = true,
            PromotionKey = $"achievement_{milestone.ExerciseCountRequired}",
            Messages = new Dictionary<string, PromotionMessage>
            {
                ["en"] = new PromotionMessage
                {
                    Title = $"Achievement Unlocked! {milestone.DiscountPercentage}% OFF",
                    Description = $"You've completed {milestone.ExerciseCountRequired} exercises! Celebrate with a discount on premium."
                },
                ["it"] = new PromotionMessage
                {
                    Title = $"Obiettivo Raggiunto! {milestone.DiscountPercentage}% di SCONTO",
                    Description = $"Hai completato {milestone.ExerciseCountRequired} esercizi! Festeggia con uno sconto sul premium."
                }
            },
            Discounts = new Dictionary<string, int>
            {
                ["monthly"] = milestone.DiscountPercentage,
                ["yearly"] = milestone.DiscountPercentage
            },
            ValidUntil = milestone.FirstShownDate!.Value.AddDays(7),
            IsLocal = true
        };
    }
    
    private async Task LoadMilestoneProgressAsync()
    {
        try
        {
            var json = await SecureStorage.Default.GetAsync("milestone_progress");
            if (!string.IsNullOrEmpty(json))
            {
                var saved = JsonSerializer.Deserialize<List<AchievementMilestone>>(json);
                if (saved != null && saved.Count == _milestones.Count)
                {
                    for (int i = 0; i < _milestones.Count; i++)
                    {
                        _milestones[i].FirstShownDate = saved[i].FirstShownDate;
                        _milestones[i].WasCompleted = saved[i].WasCompleted;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading milestone progress");
        }
    }
    
    private async Task SaveMilestoneProgressAsync()
    {
        try
        {
            var json = JsonSerializer.Serialize(_milestones);
            await SecureStorage.Default.SetAsync("milestone_progress", json);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving milestone progress");
        }
    }
    
    private bool TryParsePrice(string priceString, out decimal price, out string currency)
    {
        price = 0;
        currency = "";
        
        // Simple parser for common price formats
        var cleaned = System.Text.RegularExpressions.Regex.Replace(priceString, @"[^\d,.]", "");
        currency = System.Text.RegularExpressions.Regex.Replace(priceString, @"[\d,.\s]", "");
        
        if (decimal.TryParse(cleaned.Replace(",", "."), out price))
            return true;
        
        return false;
    }
    
    private string FormatPrice(decimal price, string currency)
    {
        // Simple formatter - in production, use proper culture-specific formatting
        return $"{currency}{price:F2}";
    }
    
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
    
    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
            return;
            
        if (disposing)
        {
            _httpClient?.Dispose();
        }
        
        _disposed = true;
    }
}