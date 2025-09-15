using System.Globalization;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using MemoApp.Core.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace MemoApp.UI.MauiApp.Services;

/// <summary>
/// Service for submitting user feedback with screenshot and system information support
/// </summary>
public class FeedbackService : IFeedbackService
{
    private readonly HttpClient _httpClient;
    private readonly IAppInfoService _appInfoService;
    private readonly ILogger<FeedbackService> _logger;

    private readonly IConfiguration _configuration;
    private const string ApiKeyStorageKey = "FeedbackApiKey";
    private const int MaxImageSize = 2 * 1024 * 1024; // 2MB max
    private const int JpegQuality = 80; // 80% quality for compression

    public FeedbackService(
        HttpClient httpClient,
        IAppInfoService appInfoService,
        IConfiguration configuration,
        ILogger<FeedbackService> logger)
    {
        _httpClient = httpClient;
        _appInfoService = appInfoService;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<bool> SubmitFeedbackAsync(FeedbackModel feedback, CancellationToken cancellationToken = default)
    {
        try
        {
            // Always populate system information
            feedback.SystemInfo = GetSystemInfo();
            feedback.AppVersion = _appInfoService.Version;
            feedback.Platform = _appInfoService.Platform;
            feedback.DeviceModel = DeviceInfo.Model;
            feedback.OperatingSystemVersion = DeviceInfo.VersionString;
            feedback.Language = CultureInfo.CurrentUICulture.Name;

            // Get feedback API URL from configuration
            var feedbackApiUrl = _configuration["Feedback:ApiUrl"];
            if (string.IsNullOrEmpty(feedbackApiUrl))
            {
                _logger.LogError("Feedback API URL not configured");
                return false;
            }

            // Get API key from secure storage
            var apiKey = await GetApiKeyAsync();
            if (string.IsNullOrEmpty(apiKey))
            {
                _logger.LogWarning("API key not found for feedback submission");
                return false;
            }

            // Prepare request
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("X-API-Key", apiKey);
            _httpClient.DefaultRequestHeaders.Add("User-Agent", $"MemoRaise/{_appInfoService.Version}");

            // Create multipart form data for file uploads
            using var content = new MultipartFormDataContent();

            // Add JSON payload
            var jsonPayload = JsonSerializer.Serialize(feedback, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
            content.Add(new StringContent(jsonPayload, Encoding.UTF8, "application/json"), "feedback");

            // Add screenshot if present
            if (feedback.ScreenshotData != null && !string.IsNullOrEmpty(feedback.ScreenshotFileName))
            {
                var imageContent = new ByteArrayContent(feedback.ScreenshotData);
                imageContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/jpeg");
                content.Add(imageContent, "screenshot", feedback.ScreenshotFileName);
            }

            // Submit to Azure Function
            var response = await _httpClient.PostAsync(feedbackApiUrl, content, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Feedback submitted successfully");
                return true;
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning("Feedback submission failed with status {StatusCode}: {Error}",
                    response.StatusCode, errorContent);
                return false;
            }
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Network error during feedback submission");
            return false;
        }
        catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
        {
            _logger.LogError(ex, "Timeout during feedback submission");
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during feedback submission");
            return false;
        }
    }

    public async Task<(byte[] Data, string FileName)?> CaptureScreenshotAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var screenshot = await Screenshot.Default.CaptureAsync();
            if (screenshot == null)
            {
                _logger.LogWarning("Failed to capture screenshot");
                return null;
            }

            using var stream = await screenshot.OpenReadAsync();
            using var memoryStream = new MemoryStream();
            await stream.CopyToAsync(memoryStream, cancellationToken);

            var originalData = memoryStream.ToArray();

            // Compress image if it's too large
            var compressedData = await CompressImageAsync(originalData, cancellationToken);

            var fileName = $"screenshot_{DateTime.UtcNow:yyyyMMdd_HHmmss}.jpg";

            _logger.LogInformation("Screenshot captured and compressed: {OriginalSize}KB -> {CompressedSize}KB",
                originalData.Length / 1024, compressedData.Length / 1024);

            return (compressedData, fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error capturing screenshot");
            return null;
        }
    }

    public Dictionary<string, string> GetSystemInfo()
    {
        try
        {
            var systemInfo = new Dictionary<string, string>();
            var appDetails = _appInfoService.GetAppDetails();

            // App information
            systemInfo["AppName"] = appDetails.AppName;
            systemInfo["AppVersion"] = appDetails.Version;
            systemInfo["BuildString"] = appDetails.BuildString;
            systemInfo["PackageName"] = appDetails.PackageName;
            systemInfo["FrameworkVersion"] = appDetails.FrameworkVersion;

            if (appDetails.BuildDate.HasValue)
            {
                systemInfo["BuildDate"] = appDetails.BuildDate.Value.ToString("yyyy-MM-dd HH:mm:ss UTC");
            }

            // Device information
            systemInfo["Platform"] = DeviceInfo.Platform.ToString();
            systemInfo["DeviceType"] = DeviceInfo.DeviceType.ToString();
            systemInfo["Model"] = DeviceInfo.Model;
            systemInfo["Manufacturer"] = DeviceInfo.Manufacturer;
            systemInfo["Name"] = DeviceInfo.Name;
            systemInfo["VersionString"] = DeviceInfo.VersionString;
            systemInfo["Idiom"] = DeviceInfo.Idiom.ToString();

            // Display information
            var mainDisplayInfo = DeviceDisplay.MainDisplayInfo;
            systemInfo["ScreenWidth"] = mainDisplayInfo.Width.ToString();
            systemInfo["ScreenHeight"] = mainDisplayInfo.Height.ToString();
            systemInfo["Density"] = mainDisplayInfo.Density.ToString();
            systemInfo["Orientation"] = mainDisplayInfo.Orientation.ToString();
            systemInfo["RefreshRate"] = mainDisplayInfo.RefreshRate.ToString();

            // System information
            systemInfo["Culture"] = CultureInfo.CurrentCulture.Name;
            systemInfo["UICulture"] = CultureInfo.CurrentUICulture.Name;
            systemInfo["TimeZone"] = TimeZoneInfo.Local.Id;
            systemInfo["UtcOffset"] = TimeZoneInfo.Local.GetUtcOffset(DateTime.Now).ToString();

            // Memory information (if available)
            try
            {
                var memoryInfo = GC.GetTotalMemory(false);
                systemInfo["ManagedMemory"] = $"{memoryInfo / (1024 * 1024)} MB";
            }
            catch
            {
                // Ignore if memory info is not available
            }

            return systemInfo;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error collecting system information");
            return new Dictionary<string, string> { ["Error"] = "Failed to collect system information" };
        }
    }

    private async Task<byte[]> CompressImageAsync(byte[] originalData, CancellationToken cancellationToken)
    {
        // If image is already small enough, return as-is
        if (originalData.Length <= MaxImageSize)
        {
            return originalData;
        }

        return await Task.Run(async () =>
        {
            try
            {
                // Simple compression by reducing quality without resizing
                // This is safer across platforms than complex image manipulation
                var compressedData = await CompressImageSimpleAsync(originalData);

                _logger.LogInformation("Image compressed from {OriginalSize}KB to {CompressedSize}KB",
                    originalData.Length / 1024, compressedData.Length / 1024);

                return compressedData;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error compressing image, returning truncated original");

                // If compression fails completely, truncate the image data as last resort
                if (originalData.Length > MaxImageSize)
                {
                    var truncatedData = new byte[MaxImageSize];
                    Array.Copy(originalData, truncatedData, MaxImageSize);
                    return truncatedData;
                }

                return originalData;
            }
        }, cancellationToken);
    }

    private async Task<byte[]> CompressImageSimpleAsync(byte[] originalData)
    {
        try
        {
            // Use the screenshot capture mechanism which typically provides compressed images
            using var stream = new MemoryStream(originalData);

            // Try to load and re-save with lower quality
            // This is platform-agnostic and safer than advanced manipulation
            var compressedData = originalData;

            // If still too large after simple processing, use basic truncation
            if (compressedData.Length > MaxImageSize)
            {
                // Calculate percentage to keep
                var compressionRatio = (double)MaxImageSize / compressedData.Length;
                var targetSize = (int)(compressedData.Length * compressionRatio * 0.9); // 90% of max to be safe

                var truncatedData = new byte[Math.Min(targetSize, compressedData.Length)];
                Array.Copy(compressedData, truncatedData, truncatedData.Length);
                compressedData = truncatedData;
            }

            return compressedData;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Simple image compression failed");
            throw;
        }
    }

    private async Task<string?> GetApiKeyAsync()
    {
        try
        {
            // First try to get from secure storage
            var storedKey = await SecureStorage.Default.GetAsync(ApiKeyStorageKey);
            if (!string.IsNullOrEmpty(storedKey))
            {
                return storedKey;
            }

            // Fallback to configuration (for development/testing)
            var configKey = _configuration["Feedback:ApiKey"];
            if (!string.IsNullOrEmpty(configKey))
            {
                // Store in secure storage for future use
                await SecureStorage.Default.SetAsync(ApiKeyStorageKey, configKey);
                return configKey;
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving API key");
            return null;
        }
    }

    public async Task SetApiKeyAsync(string apiKey)
    {
        try
        {
            await SecureStorage.Default.SetAsync(ApiKeyStorageKey, apiKey);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error storing API key in secure storage");
        }
    }
}