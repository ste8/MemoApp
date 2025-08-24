namespace MemoApp.Core.Packs;

public class DownloadProgress
{
    public string PackId { get; set; } = string.Empty;
    public DownloadState State { get; set; }
    public long BytesReceived { get; set; }
    public long TotalBytes { get; set; }
    public double ProgressPercentage => TotalBytes > 0 ? (BytesReceived * 100.0) / TotalBytes : 0;
    public string? StatusMessage { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    
    public TimeSpan ElapsedTime => (EndTime ?? DateTime.UtcNow) - StartTime;
    
    public string GetFormattedProgress()
    {
        const long KB = 1024;
        const long MB = KB * 1024;
        
        string FormatSize(long bytes)
        {
            if (bytes >= MB)
                return $"{bytes / (double)MB:F2} MB";
            if (bytes >= KB)
                return $"{bytes / (double)KB:F2} KB";
            return $"{bytes} bytes";
        }
        
        return $"{FormatSize(BytesReceived)} / {FormatSize(TotalBytes)}";
    }
}

public enum DownloadState
{
    Pending,
    Downloading,
    Extracting,
    GeneratingThumbnails,
    Completed,
    Failed,
    Cancelled
}