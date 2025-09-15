using System.ComponentModel.DataAnnotations;
using System.ComponentModel;

namespace MemoApp.Core.Models;

public enum FeedbackType
{
    Bug = 1,
    Feature = 2,
    Other = 3
}

public class FeedbackModel
{
    [Required(ErrorMessage = "Subject is required")]
    [StringLength(200, MinimumLength = 5, ErrorMessage = "Subject must be between 5 and 200 characters")]
    public string Subject { get; set; } = string.Empty;

    [Required(ErrorMessage = "Message is required")]
    [StringLength(2000, MinimumLength = 10, ErrorMessage = "Message must be between 10 and 2000 characters")]
    public string Message { get; set; } = string.Empty;

    [Required(ErrorMessage = "Feedback type is required")]
    public FeedbackType Type { get; set; } = FeedbackType.Other;

    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Please enter a valid email address")]
    [StringLength(254, ErrorMessage = "Email address is too long")]
    public string UserEmail { get; set; } = string.Empty;

    public string? UserName { get; set; }

    public List<AttachedImage> AttachedImages { get; set; } = new();

    public Dictionary<string, string> SystemInfo { get; set; } = new();

    public string AppVersion { get; set; } = string.Empty;

    public string Platform { get; set; } = string.Empty;

    public string DeviceModel { get; set; } = string.Empty;

    public string OperatingSystemVersion { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public string Language { get; set; } = string.Empty;
}

public class AttachedImage : INotifyPropertyChanged
{
    public byte[] Data { get; set; } = Array.Empty<byte>();

    public string FileName { get; set; } = string.Empty;

    public string ContentType { get; set; } = string.Empty;

    public long FileSize { get; set; }

    public DateTime AttachedAt { get; set; } = DateTime.UtcNow;

    // For UI display - not serialized to API
    private object? _imageSource;
    public object? ImageSource
    {
        get => _imageSource;
        set
        {
            if (_imageSource != value)
            {
                // Dispose old image source if it implements IDisposable
                if (_imageSource is IDisposable disposable)
                {
                    disposable.Dispose();
                }
                _imageSource = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ImageSource)));
            }
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
}