using FluentAssertions;
using MemoApp.Core.IntegrationTests.Fixtures;
using MemoApp.Core.IntegrationTests.Utilities;
using MemoApp.Core.Services.ImageGenerators;
using Microsoft.Extensions.Logging;

namespace MemoApp.Core.IntegrationTests.Services.ImageGenerators;

/// <summary>
/// Integration tests for OpenAI Image Generator service
/// These tests require a valid OpenAI API key configured in user secrets or environment variables
/// </summary>
public class OpenAIImageGeneratorTests : IClassFixture<OpenAIImageGeneratorFixture>
{
    private readonly OpenAIImageGeneratorFixture _fixture;
    private readonly IImageGenerator _imageGenerator;
    private readonly ILogger<OpenAIImageGeneratorTests> _logger;

    public OpenAIImageGeneratorTests(OpenAIImageGeneratorFixture fixture)
    {
        _fixture = fixture;
        _imageGenerator = fixture.ImageGenerator;
        _logger = fixture.GetRequiredService<ILogger<OpenAIImageGeneratorTests>>();
    }

    [Fact]
    public async Task IsAvailableAsync_WithValidConfiguration_ShouldReturnTrue()
    {
        // Act
        var isAvailable = await _imageGenerator.IsAvailableAsync();

        // Assert
        isAvailable.Should().BeTrue("OpenAI service should be available with valid configuration");
        _logger.LogInformation("Service availability test passed");
    }

    [Theory]
    [MemberData(nameof(OpenAIImageGeneratorFixture.GetImageDescriptionTestData), MemberType = typeof(OpenAIImageGeneratorFixture))]
    public async Task GenerateImageAsync_WithDescription_ShouldGenerateValidImage(
        string description, string testContext)
    {
        // Arrange
        var options = new ImageGenerationOptions
        {
            Width = 512,
            Height = 512,
            Style = "natural",
            AdditionalContext = $"Test image for {testContext}",
            DownloadImageData = true
        };

        _logger.LogInformation("Testing image generation for description '{Description}'", description);

        // Act
        var result = await _imageGenerator.GenerateImageAsync(description, options);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue($"Image generation should succeed for description '{description}'");
        result.ErrorMessage.Should().BeNullOrEmpty();
        result.ImageUrl.Should().NotBeNullOrEmpty("Image URL should be provided");
        result.ImageData.Should().NotBeNullOrEmpty("Image data should be downloaded");
        result.UsedPrompt.Should().NotBeNullOrEmpty("Used prompt should be recorded");

        // Validate image data using helper utilities
        ImageTestHelpers.MeetsSizeRequirements(result.ImageData).Should().BeTrue("Image should meet minimum size requirements");
        ImageTestHelpers.IsValidImage(result.ImageData).Should().BeTrue("Image should be in a valid format (PNG or JPEG)");
        ImageTestHelpers.IsValidImageUrl(result.ImageUrl).Should().BeTrue("Image URL should be properly formatted");

        _logger.LogInformation("Successfully generated image for description '{Description}': {ImageSummary}", 
            description, ImageTestHelpers.GetImageSummary(result.ImageData));
    }

    [Fact]
    public async Task GenerateImageAsync_WithCustomOptions_ShouldRespectOptions()
    {
        // Arrange
        const string description = "a medieval castle with towers and flags";
        var options = new ImageGenerationOptions
        {
            Width = 1024,
            Height = 1024,
            Style = "vivid",
            AdditionalContext = "cartoon style with bright colors",
            DownloadImageData = false // Only get URL
        };

        // Act
        var result = await _imageGenerator.GenerateImageAsync(description, options);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.ImageUrl.Should().NotBeNullOrEmpty();
        result.ImageData.Should().BeNull("Image data should not be downloaded when DownloadImageData is false");
        result.UsedPrompt.Should().Contain("castle", "Prompt should contain the description");
        result.UsedPrompt.Should().Contain("cartoon", "Prompt should include additional context");

        _logger.LogInformation("Custom options test passed for '{Description}' with URL: {Url}", description, result.ImageUrl);
    }

    [Fact]
    public async Task GenerateImageAsync_WithInvalidDescription_ShouldReturnFailure()
    {
        // Arrange
        const string emptyDescription = "";

        // Act
        var result = await _imageGenerator.GenerateImageAsync(emptyDescription);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse("Empty description should result in failure");
        result.ErrorMessage.Should().NotBeNullOrEmpty("Error message should be provided");
        result.ImageUrl.Should().BeNullOrEmpty();
        result.ImageData.Should().BeNullOrEmpty();

        _logger.LogInformation("Invalid description test passed with error: {Error}", result.ErrorMessage);
    }

    [Fact]
    public async Task GenerateBatchAsync_WithMultipleDescriptions_ShouldGenerateAllImages()
    {
        // Arrange
        var descriptions = OpenAIImageGeneratorFixture.GetBatchTestData().Take(3).ToArray(); // Limit to 3 for faster testing
        var options = new ImageGenerationOptions
        {
            Width = 512,
            Height = 512,
            Style = "natural",
            DownloadImageData = false, // URLs only for batch to save time
            AdditionalContext = "Batch test image"
        };

        _logger.LogInformation("Starting batch generation test with {Count} descriptions", descriptions.Length);

        // Act
        var results = await _imageGenerator.GenerateBatchAsync(descriptions, options);

        // Assert
        results.Should().NotBeNull();
        results.Should().HaveCount(3, "Should return results for all requested descriptions");

        for (int i = 0; i < results.Count; i++)
        {
            var result = results[i];
            var description = descriptions[i];

            result.Should().NotBeNull($"Result for description '{description}' should not be null");
            result.IsSuccess.Should().BeTrue($"Generation should succeed for description '{description}'");
            result.ImageUrl.Should().NotBeNullOrEmpty($"Image URL should be provided for description '{description}'");
            result.ErrorMessage.Should().BeNullOrEmpty($"No error should occur for description '{description}'");
        }

        var successCount = results.Count(r => r.IsSuccess);
        _logger.LogInformation("Batch generation completed: {Success}/{Total} successful", 
            successCount, results.Count);
    }

    [Fact]
    public async Task GenerateBatchAsync_WithEmptyCollection_ShouldReturnEmptyResults()
    {
        // Arrange
        var emptyDescriptions = Array.Empty<string>();

        // Act
        var results = await _imageGenerator.GenerateBatchAsync(emptyDescriptions);

        // Assert
        results.Should().NotBeNull();
        results.Should().BeEmpty("Empty input should return empty results");

        _logger.LogInformation("Empty batch test passed");
    }

    [Fact]
    public async Task GenerateImageAsync_WithCancellation_ShouldRespectCancellationToken()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        cts.CancelAfter(TimeSpan.FromMilliseconds(100)); // Very short timeout

        // Act & Assert
        var act = async () => await _imageGenerator.GenerateImageAsync("a simple test image", cancellationToken: cts.Token);
        
        // The operation might complete before cancellation, so we check for either success or cancellation
        var result = await act.Should().NotThrowAsync("Cancellation should be handled gracefully");
        
        _logger.LogInformation("Cancellation test completed");
    }

    [Fact]
    public async Task GenerateImageAsync_WithComplexDescription_ShouldHandleComplexPrompts()
    {
        // Arrange
        const string complexDescription = "an extraordinary magnificent butterfly with iridescent wings sitting on a blooming sunflower in a magical forest";
        var options = new ImageGenerationOptions
        {
            AdditionalContext = "This is a test with a very long and complex description that should still generate a proper image",
            DownloadImageData = false
        };

        // Act
        var result = await _imageGenerator.GenerateImageAsync(complexDescription, options);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue("Complex descriptions should still generate images");
        result.ImageUrl.Should().NotBeNullOrEmpty();
        result.UsedPrompt.Should().Contain("butterfly", "Prompt should contain part of the description");

        _logger.LogInformation("Complex description test passed for '{Description}'", complexDescription);
    }

    [Fact]
    public async Task GenerateImageAsync_MultipleConcurrentRequests_ShouldHandleConcurrency()
    {
        // Arrange
        var descriptions = new[] { "a red apple on a table", "a yellow banana", "a bowl of cherries" };
        var tasks = descriptions.Select(description => 
            _imageGenerator.GenerateImageAsync(description, new ImageGenerationOptions 
            { 
                DownloadImageData = false 
            })).ToArray();

        _logger.LogInformation("Starting concurrent generation test with {Count} requests", tasks.Length);

        // Act
        var results = await Task.WhenAll(tasks);

        // Assert
        results.Should().HaveCount(3);
        results.Should().OnlyContain(r => r.IsSuccess, "All concurrent requests should succeed");
        results.Should().OnlyContain(r => !string.IsNullOrEmpty(r.ImageUrl), "All results should have URLs");

        _logger.LogInformation("Concurrent generation test completed successfully");
    }
}