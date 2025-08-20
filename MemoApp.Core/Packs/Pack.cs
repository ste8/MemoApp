using System.Text.Json.Serialization;

namespace MemoApp.Core.Packs;

public class Pack
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("international_name")]
    public string InternationalName { get; set; } = string.Empty;

    [JsonPropertyName("native_name")]
    public string NativeName { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("native_description")]
    public string NativeDescription { get; set; } = string.Empty;

    [JsonPropertyName("version")]
    public string Version { get; set; } = string.Empty;

    [JsonPropertyName("language_code")]
    public string LanguageCode { get; set; } = string.Empty;

    [JsonPropertyName("author")]
    public string Author { get; set; } = string.Empty;

    [JsonPropertyName("file_size")]
    public long FileSize { get; set; }

    [JsonPropertyName("download_url")]
    public string? DownloadUrl { get; set; }
    
    [JsonIgnore]
    public bool IsDownloaded { get; set; }
    
    [JsonIgnore]
    public DateTime? DownloadedAt { get; set; }
    
    [JsonIgnore]
    public string? LocalPath { get; set; }
    
    [JsonIgnore]
    public bool HasUpdate { get; set; }
    
    [JsonIgnore]
    public bool ThumbnailsGenerated { get; set; }

    public string GetDisplayName(bool useNativeName = false)
    {
        return useNativeName && !string.IsNullOrEmpty(NativeName) 
            ? NativeName 
            : InternationalName;
    }

    public string GetDisplayDescription(bool useNativeDescription = false)
    {
        return useNativeDescription && !string.IsNullOrEmpty(NativeDescription) 
            ? NativeDescription 
            : Description;
    }

    public string GetFormattedFileSize()
    {
        const long KB = 1024;
        const long MB = KB * 1024;
        const long GB = MB * 1024;

        if (FileSize >= GB)
            return $"{FileSize / (double)GB:F2} GB";
        if (FileSize >= MB)
            return $"{FileSize / (double)MB:F2} MB";
        if (FileSize >= KB)
            return $"{FileSize / (double)KB:F2} KB";

        return $"{FileSize} bytes";
    }
}