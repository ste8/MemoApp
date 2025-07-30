using MemoApp.Core.Services.ImageGenerators;
using Microsoft.Extensions.Logging;

namespace MemoApp.Core.IntegrationTests.Fixtures;

/// <summary>
/// Specialized fixture for OpenAI Image Generator tests
/// </summary>
public class OpenAIImageGeneratorFixture : TestFixture
{
    public IImageGenerator ImageGenerator { get; }

    public OpenAIImageGeneratorFixture()
    {
        if (!IsOpenAIConfigured())
        {
            throw new InvalidOperationException(
                "OpenAI API key is not configured. Please set the OpenAI:ApiKey in user secrets or environment variables.");
        }

        ImageGenerator = GetRequiredService<IImageGenerator>();
    }

    /// <summary>
    /// Gets test data for image description testing
    /// </summary>
    public static IEnumerable<object[]> GetImageDescriptionTestData()
    {
        return new[]
        {
            new object[] { "a cup of tea on a wooden table", "Simple beverage scene" },
            new object[] { "an old wise man with a long beard", "Biblical figure representation" },
            // new object[] { "a caring mother holding a child", "Mother figure" },
            // new object[] { "a field of golden rye grain", "Type of grain field" },
            // new object[] { "a judge's gavel on a law book", "Legal concept representation" },
            // new object[] { "a red leather shoe", "Footwear item" },
            // new object[] { "a black and white cow in a meadow", "Farm animal" },
            // new object[] { "green ivy climbing up a brick wall", "Climbing plant" },
            // new object[] { "a slice of apple pie with steam", "Baked dessert" }
        };
    }

    /// <summary>
    /// Gets batch test data for multiple descriptions
    /// </summary>
    public static IEnumerable<string> GetBatchTestData()
    {
        return new[]
        {
            "a orange tabby cat sitting on a windowsill",
            "a golden retriever playing in a park",
            "a bright yellow sun in a blue sky",
            "a crescent moon with stars around it",
            "a twinkling star in the night sky"
        };
    }
}