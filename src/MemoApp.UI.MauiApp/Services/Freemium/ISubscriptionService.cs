using MemoApp.UI.MauiApp.Models.Freemium;

namespace MemoApp.UI.MauiApp.Services.Freemium;

public interface ISubscriptionService
{
    Task<SubscriptionStatus> GetCurrentSubscriptionStatusAsync();
    Task<List<SubscriptionOption>> GetAvailableSubscriptionsAsync();
    Task<bool> PurchaseSubscriptionAsync(string subscriptionId);
    Task<bool> RestoreSubscriptionsAsync();
    Task<bool> ValidateSubscriptionAsync();
    Task<DateTime?> GetSubscriptionExpirationDateAsync();
    bool IsPremiumUser { get; }
    event EventHandler<SubscriptionStatus>? SubscriptionStatusChanged;
}