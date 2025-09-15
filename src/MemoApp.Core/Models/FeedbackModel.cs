using System.ComponentModel.DataAnnotations;

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

    public byte[]? ScreenshotData { get; set; }

    public string? ScreenshotFileName { get; set; }

    public Dictionary<string, string> SystemInfo { get; set; } = new();

    public string AppVersion { get; set; } = string.Empty;

    public string Platform { get; set; } = string.Empty;

    public string DeviceModel { get; set; } = string.Empty;

    public string OperatingSystemVersion { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public string Language { get; set; } = string.Empty;

    public bool IncludeScreenshot { get; set; } = false;
}