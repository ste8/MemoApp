using MemoApp.UI.MauiApp.Models.Freemium;
using Microsoft.Extensions.Logging;
using Plugin.InAppBilling;

namespace MemoApp.UI.MauiApp.Services.Freemium;

public class SubscriptionService : ISubscriptionService, IDisposable
{
    private readonly ILogger<SubscriptionService> _logger;
    private readonly IInAppBilling _billing;
    private SubscriptionStatus? _cachedStatus;
    private DateTime _lastCacheTime = DateTime.MinValue;
    private readonly TimeSpan _cacheExpiration = TimeSpan.FromMinutes(5);
    private bool _disposed = false;
    
    // Subscription product IDs
    private const string MonthlySubscriptionId = "com.stefanoottaviani.memoapp.premium.monthly";
    private const string YearlySubscriptionId = "com.stefanoottaviani.memoapp.premium.yearly";
    private const string LifetimeSubscriptionId = "com.stefanoottaviani.memoapp.premium.lifetime";
    
    public bool IsPremiumUser => _cachedStatus?.IsActive ?? false;
    
    public event EventHandler<SubscriptionStatus>? SubscriptionStatusChanged;
    
    public SubscriptionService(ILogger<SubscriptionService> logger)
    {
        _logger = logger;
        _billing = CrossInAppBilling.Current;
    }
    
    public async Task<SubscriptionStatus> GetCurrentSubscriptionStatusAsync()
    {
        try
        {
            // Return cached status if still valid
            if (_cachedStatus != null && DateTime.UtcNow - _lastCacheTime < _cacheExpiration)
            {
                return _cachedStatus;
            }
            
            // First check local storage for saved subscription
            var savedStatus = await LoadSavedSubscriptionStatusAsync();
            if (savedStatus != null && savedStatus.IsActive)
            {
                // Validate with store if needed
                if ((DateTime.UtcNow - savedStatus.LastChecked).TotalHours > 24)
                {
                    var isValid = await ValidateSubscriptionAsync();
                    if (!isValid)
                    {
                        savedStatus = new SubscriptionStatus { Tier = SubscriptionTier.Free };
                        await SaveSubscriptionStatusAsync(savedStatus);
                    }
                }
                
                _cachedStatus = savedStatus;
                _lastCacheTime = DateTime.UtcNow;
                return savedStatus;
            }
            
            // Check with the store
            var connected = await _billing.ConnectAsync();
            if (!connected)
            {
                _logger.LogWarning("Could not connect to billing service");
                return savedStatus ?? new SubscriptionStatus { Tier = SubscriptionTier.Free };
            }
            
            var purchases = await _billing.GetPurchasesAsync(ItemType.Subscription);
            
            SubscriptionStatus status = new SubscriptionStatus { Tier = SubscriptionTier.Free };
            
            foreach (var purchase in purchases.Where(p => p.State == PurchaseState.Purchased))
            {
                if (purchase.ProductId == LifetimeSubscriptionId)
                {
                    status = new SubscriptionStatus
                    {
                        Tier = SubscriptionTier.Lifetime,
                        TransactionId = purchase.Id,
                        LastChecked = DateTime.UtcNow
                    };
                    break;
                }
                else if (purchase.ProductId == YearlySubscriptionId)
                {
                    status = new SubscriptionStatus
                    {
                        Tier = SubscriptionTier.Yearly,
                        ExpirationDate = purchase.TransactionDateUtc.AddYears(1),
                        TransactionId = purchase.Id,
                        LastChecked = DateTime.UtcNow
                    };
                }
                else if (purchase.ProductId == MonthlySubscriptionId && status.Tier != SubscriptionTier.Yearly)
                {
                    status = new SubscriptionStatus
                    {
                        Tier = SubscriptionTier.Monthly,
                        ExpirationDate = purchase.TransactionDateUtc.AddMonths(1),
                        TransactionId = purchase.Id,
                        LastChecked = DateTime.UtcNow
                    };
                }
            }
            
            await _billing.DisconnectAsync();
            
            // Save and cache the status
            await SaveSubscriptionStatusAsync(status);
            _cachedStatus = status;
            _lastCacheTime = DateTime.UtcNow;
            
            SubscriptionStatusChanged?.Invoke(this, status);
            
            return status;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting subscription status");
            return new SubscriptionStatus { Tier = SubscriptionTier.Free };
        }
    }
    
    public async Task<List<SubscriptionOption>> GetAvailableSubscriptionsAsync()
    {
        var options = new List<SubscriptionOption>();
        
        try
        {
            var connected = await _billing.ConnectAsync();
            if (!connected)
            {
                _logger.LogWarning("Could not connect to billing service");
                return GetDefaultSubscriptionOptions();
            }
            
            var productIds = new string[] { MonthlySubscriptionId, YearlySubscriptionId, LifetimeSubscriptionId };
            var products = await _billing.GetProductInfoAsync(ItemType.Subscription, productIds);
            
            foreach (var product in products)
            {
                var option = new SubscriptionOption
                {
                    Id = product.ProductId,
                    LocalizedPrice = product.LocalizedPrice,
                    CurrencyCode = product.CurrencyCode,
                    DisplayPrice = product.LocalizedPrice
                };
                
                options.Add(option);
            }
            
            await _billing.DisconnectAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting available subscriptions");
            return GetDefaultSubscriptionOptions();
        }
        
        return options.Any() ? options : GetDefaultSubscriptionOptions();
    }
    
    public async Task<bool> PurchaseSubscriptionAsync(string subscriptionId)
    {
        try
        {
            var connected = await _billing.ConnectAsync();
            if (!connected)
            {
                _logger.LogWarning("Could not connect to billing service");
                return false;
            }
            
            var purchase = await _billing.PurchaseAsync(subscriptionId, ItemType.Subscription);
            
            if (purchase?.State == PurchaseState.Purchased)
            {
                // Update subscription status
                var tier = subscriptionId switch
                {
                    MonthlySubscriptionId => SubscriptionTier.Monthly,
                    YearlySubscriptionId => SubscriptionTier.Yearly,
                    LifetimeSubscriptionId => SubscriptionTier.Lifetime,
                    _ => SubscriptionTier.Free
                };
                
                var status = new SubscriptionStatus
                {
                    Tier = tier,
                    TransactionId = purchase.Id,
                    LastChecked = DateTime.UtcNow
                };
                
                if (tier == SubscriptionTier.Monthly)
                    status.ExpirationDate = DateTime.UtcNow.AddMonths(1);
                else if (tier == SubscriptionTier.Yearly)
                    status.ExpirationDate = DateTime.UtcNow.AddYears(1);
                
                await SaveSubscriptionStatusAsync(status);
                _cachedStatus = status;
                _lastCacheTime = DateTime.UtcNow;
                
                SubscriptionStatusChanged?.Invoke(this, status);
                
                await _billing.DisconnectAsync();
                return true;
            }
            
            await _billing.DisconnectAsync();
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error purchasing subscription");
            return false;
        }
    }
    
    public async Task<bool> RestoreSubscriptionsAsync()
    {
        try
        {
            var connected = await _billing.ConnectAsync();
            if (!connected)
            {
                _logger.LogWarning("Could not connect to billing service");
                return false;
            }
            
            var purchases = await _billing.GetPurchasesAsync(ItemType.Subscription);
            
            if (purchases.Any(p => p.State == PurchaseState.Purchased))
            {
                // Force refresh of subscription status
                _cachedStatus = null;
                _lastCacheTime = DateTime.MinValue;
                await GetCurrentSubscriptionStatusAsync();
                
                await _billing.DisconnectAsync();
                return true;
            }
            
            await _billing.DisconnectAsync();
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error restoring subscriptions");
            return false;
        }
    }
    
    public async Task<bool> ValidateSubscriptionAsync()
    {
        try
        {
            var status = await GetCurrentSubscriptionStatusAsync();
            return status.IsActive;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating subscription");
            return false;
        }
    }
    
    public async Task<DateTime?> GetSubscriptionExpirationDateAsync()
    {
        var status = await GetCurrentSubscriptionStatusAsync();
        return status.ExpirationDate;
    }
    
    private async Task SaveSubscriptionStatusAsync(SubscriptionStatus status)
    {
        try
        {
            await SecureStorage.Default.SetAsync("subscription_tier", status.Tier.ToString());
            
            if (status.ExpirationDate.HasValue)
                await SecureStorage.Default.SetAsync("subscription_expiration", status.ExpirationDate.Value.ToString("O"));
            else
                SecureStorage.Remove("subscription_expiration");
            
            if (!string.IsNullOrEmpty(status.TransactionId))
                await SecureStorage.Default.SetAsync("subscription_transaction", status.TransactionId);
            
            await SecureStorage.Default.SetAsync("subscription_last_checked", status.LastChecked.ToString("O"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving subscription status");
        }
    }
    
    private async Task<SubscriptionStatus?> LoadSavedSubscriptionStatusAsync()
    {
        try
        {
            var tierString = await SecureStorage.Default.GetAsync("subscription_tier");
            if (string.IsNullOrEmpty(tierString))
                return null;
            
            if (!Enum.TryParse<SubscriptionTier>(tierString, out var tier))
                return null;
            
            var status = new SubscriptionStatus { Tier = tier };
            
            var expirationString = await SecureStorage.Default.GetAsync("subscription_expiration");
            if (!string.IsNullOrEmpty(expirationString) && DateTime.TryParse(expirationString, out var expiration))
                status.ExpirationDate = expiration;
            
            status.TransactionId = await SecureStorage.Default.GetAsync("subscription_transaction");
            
            var lastCheckedString = await SecureStorage.Default.GetAsync("subscription_last_checked");
            if (!string.IsNullOrEmpty(lastCheckedString) && DateTime.TryParse(lastCheckedString, out var lastChecked))
                status.LastChecked = lastChecked;
            
            return status;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading saved subscription status");
            return null;
        }
    }
    
    private List<SubscriptionOption> GetDefaultSubscriptionOptions()
    {
        return new List<SubscriptionOption>
        {
            new() { Id = MonthlySubscriptionId, LocalizedPrice = "€0.99", CurrencyCode = "EUR", DisplayPrice = "€0.99" },
            new() { Id = YearlySubscriptionId, LocalizedPrice = "€9.99", CurrencyCode = "EUR", DisplayPrice = "€9.99" },
            new() { Id = LifetimeSubscriptionId, LocalizedPrice = "€24.99", CurrencyCode = "EUR", DisplayPrice = "€24.99" }
        };
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
            try
            {
                _billing?.DisconnectAsync()?.Wait();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error disconnecting from billing service during disposal");
            }
        }
        
        _disposed = true;
    }
}