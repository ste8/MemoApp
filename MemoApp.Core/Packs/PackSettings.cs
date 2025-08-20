using System.Text.Json.Serialization;

namespace MemoApp.Core.Packs;

public class PackSettings
{
    [JsonPropertyName("current_pack_id")]
    public string? CurrentPackId { get; set; }
    
    [JsonPropertyName("downloaded_packs")]
    public List<DownloadedPackInfo> DownloadedPacks { get; set; } = new();
    
    [JsonPropertyName("last_index_check")]
    public DateTime? LastIndexCheck { get; set; }
}

public class DownloadedPackInfo
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;
    
    [JsonPropertyName("version")]
    public string Version { get; set; } = string.Empty;
    
    [JsonPropertyName("downloaded_at")]
    public DateTime DownloadedAt { get; set; }
    
    [JsonPropertyName("path")]
    public string Path { get; set; } = string.Empty;
    
    [JsonPropertyName("thumbnails_generated")]
    public bool ThumbnailsGenerated { get; set; }
}