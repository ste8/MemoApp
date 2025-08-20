using System.Text.Json.Serialization;

namespace MemoApp.Core.Packs;

public class PackIndex
{
    [JsonPropertyName("last_updated")]
    public DateTime LastUpdated { get; set; }

    [JsonPropertyName("packs")]
    public List<Pack> Packs { get; set; } = new();
    
    [JsonIgnore]
    public DateTime? CachedAt { get; set; }
    
    public bool IsCacheExpired(int cacheMinutes = 30)
    {
        if (!CachedAt.HasValue) return true;
        return DateTime.UtcNow > CachedAt.Value.AddMinutes(cacheMinutes);
    }
}