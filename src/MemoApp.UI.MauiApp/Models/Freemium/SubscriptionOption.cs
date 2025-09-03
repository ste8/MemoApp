namespace MemoApp.UI.MauiApp.Models.Freemium;

public class SubscriptionOption
{
    public string Id { get; set; } = string.Empty; // "premium_monthly", "premium_yearly", "premium_lifetime"
    public string LocalizedPrice { get; set; } = string.Empty; // "€9,99", "$9.99", etc. (original price from platform)
    public string CurrencyCode { get; set; } = string.Empty; // "EUR", "USD", etc.
    public int? DiscountPercentage { get; set; } // 50 (for 50% off)
    public string DisplayPrice { get; set; } = string.Empty; // "€4,99" (final display price after discount)
    public bool HasDiscount => DiscountPercentage.HasValue && DiscountPercentage > 0;
}