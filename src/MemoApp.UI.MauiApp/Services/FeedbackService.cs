using System.Globalization;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using MemoApp.Core.Models;
using MemoApp.Localization.Services;
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
    private readonly ILocalizationService _localizationService;

    private readonly IConfiguration _configuration;
    private const string ApiKeyStorageKey = "FeedbackApiKey";
    private const int MaxImageSize = 2 * 1024 * 1024; // 2MB max
    private const int JpegQuality = 80; // 80% quality for compression

    public FeedbackService(
        HttpClient httpClient,
        IAppInfoService appInfoService,
        IConfiguration configuration,
        ILocalizationService localizationService,
        ILogger<FeedbackService> logger)
    {
        _httpClient = httpClient;
        _appInfoService = appInfoService;
        _configuration = configuration;
        _localizationService = localizationService;
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

            // Add attached images if present
            for (int i = 0; i < feedback.AttachedImages.Count; i++)
            {
                var image = feedback.AttachedImages[i];
                var imageContent = new ByteArrayContent(image.Data);
                imageContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(image.ContentType);
                content.Add(imageContent, $"attachment_{i}", image.FileName);
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

    public async Task<List<AttachedImage>> PickImagesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Starting PickImagesAsync method");
            var attachedImages = new List<AttachedImage>();

            // Use MediaPicker for better gallery access on mobile platforms
            var results = new List<FileResult>();

#if ANDROID || IOS
            // On mobile platforms, use MediaPicker for direct gallery access
            try
            {
                _logger.LogInformation("Using MediaPicker for mobile gallery access");

                // Add timeout to prevent hanging
                using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
                using var combinedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

                var mediaResult = await MediaPicker.Default.PickPhotoAsync(new MediaPickerOptions
                {
                    Title = _localizationService.GetString("Feedback_SelectImagesTitle")
                });

                if (mediaResult != null)
                {
                    results.Add(mediaResult);
                    _logger.LogInformation("Image selected from gallery via MediaPicker: {FileName}", mediaResult.FileName);
                }
                else
                {
                    _logger.LogInformation("No image selected from MediaPicker (user cancelled)");
                    return attachedImages; // Return empty list immediately for user cancellation
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                _logger.LogInformation("Image picker was cancelled by user");
                return attachedImages;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "MediaPicker failed, this is expected on some platforms");
                return attachedImages; // Don't try fallback, just return empty for mobile
            }
#else
            // On desktop platforms, use FilePicker for multiple selection
            try
            {
                _logger.LogInformation("Using FilePicker for desktop file selection");

                var pickOptions = new PickOptions
                {
                    FileTypes = FilePickerFileType.Images,
                    PickerTitle = _localizationService.GetString("Feedback_SelectImagesTitle")
                };

                var fileResults = await FilePicker.Default.PickMultipleAsync(pickOptions);
                if (fileResults != null && fileResults.Any())
                {
                    results.AddRange(fileResults);
                    _logger.LogInformation("Images selected via FilePicker on desktop: {Count}", fileResults.Count());
                }
                else
                {
                    _logger.LogInformation("No images selected from FilePicker (user cancelled)");
                    return attachedImages; // Return empty list for user cancellation
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "FilePicker failed on desktop platform");
                return attachedImages; // Return empty list instead of throwing
            }
#endif

            if (!results.Any())
            {
                _logger.LogInformation("No file results to process");
                return attachedImages;
            }

            foreach (var result in results)
            {
                Stream? fileStream = null;
                MemoryStream? memoryStream = null;

                try
                {
                    fileStream = await result.OpenReadAsync();
                    memoryStream = new MemoryStream();

                    await fileStream.CopyToAsync(memoryStream, cancellationToken);
                    var imageData = memoryStream.ToArray();

                    // Validate image size - reject if too large rather than corrupt
                    var validatedData = await ValidateAndProcessImageAsync(imageData, cancellationToken);
                    if (validatedData == null)
                    {
                        _logger.LogWarning("Image skipped due to size: {FileName} ({Size}KB > {MaxSize}KB)",
                            result.FileName, imageData.Length / 1024, MaxImageSize / 1024);
                        continue; // Skip this image and continue with others
                    }

                    var attachedImage = new AttachedImage
                    {
                        Data = validatedData,
                        FileName = result.FileName ?? $"image_{DateTime.UtcNow:yyyyMMdd_HHmmss}.jpg",
                        ContentType = result.ContentType ?? "image/jpeg",
                        FileSize = validatedData.Length,
                        AttachedAt = DateTime.UtcNow
                        // ImageSource will be created in the ViewModel for UI display
                    };

                    attachedImages.Add(attachedImage);

                    _logger.LogInformation("Image attached: {FileName} ({Size}KB)",
                        attachedImage.FileName, attachedImage.FileSize / 1024);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to process selected image: {FileName}", result.FileName);
                    // Continue with other images
                }
                finally
                {
                    // Explicitly dispose streams to ensure cleanup
                    fileStream?.Dispose();
                    memoryStream?.Dispose();
                }
            }

            _logger.LogInformation("PickImagesAsync completed successfully with {Count} images", attachedImages.Count);
            return attachedImages;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error picking images");
            return new List<AttachedImage>();
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

    private Task<byte[]?> ValidateAndProcessImageAsync(byte[] originalData, CancellationToken cancellationToken)
    {
        return Task.Run(() =>
        {
            try
            {
                // If image is already small enough, return as-is
                if (originalData.Length <= MaxImageSize)
                {
                    _logger.LogInformation("Image size acceptable: {Size}KB", originalData.Length / 1024);
                    return originalData;
                }

                // Image is too large - reject rather than corrupt
                _logger.LogWarning("Image rejected: {Size}KB exceeds maximum {MaxSize}KB",
                    originalData.Length / 1024, MaxImageSize / 1024);

                return null; // Signal that image was rejected
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating image data");
                return null;
            }
        }, cancellationToken);
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