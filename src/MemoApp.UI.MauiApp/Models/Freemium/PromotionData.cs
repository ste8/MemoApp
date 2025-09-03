namespace MemoApp.UI.MauiApp.Models.Freemium;

public class PromotionData
{
    public bool IsActive { get; set; }
    public string PromotionKey { get; set; } = string.Empty;
    public Dictionary<string, PromotionMessage> Messages { get; set; } = new();
    public Dictionary<string, int> Discounts { get; set; } = new(); // tier -> percentage
    public DateTime ValidUntil { get; set; }
    public int? Priority { get; set; } // Optional: for complex promotion hierarchies
    public DateTime CacheTimestamp { get; set; } // Added by client when caching, not from server
    public bool IsLocal { get; set; } // true for achievement-based
}

public class PromotionMessage
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}