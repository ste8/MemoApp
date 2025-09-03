namespace MemoApp.UI.MauiApp.Models.Freemium;

public enum SubscriptionTier
{
    Free,
    Monthly,
    Yearly,
    Lifetime
}

public class SubscriptionStatus
{
    public SubscriptionTier Tier { get; set; } = SubscriptionTier.Free;
    public DateTime? ExpirationDate { get; set; }
    public bool IsActive => Tier != SubscriptionTier.Free && 
                           (Tier == SubscriptionTier.Lifetime || 
                            (ExpirationDate.HasValue && ExpirationDate.Value > DateTime.UtcNow));
    public string? TransactionId { get; set; }
    public DateTime LastChecked { get; set; } = DateTime.UtcNow;
}