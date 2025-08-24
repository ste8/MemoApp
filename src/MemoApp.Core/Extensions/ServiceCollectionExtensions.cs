using MemoApp.Core.Services.ImageGenerators;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OpenAI;

namespace MemoApp.Core.Extensions;

/// <summary>
/// Extension methods for configuring Core services in the dependency injection container
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds the OpenAI image generator service to the dependency injection container
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configureOptions">Action to configure OpenAI options</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddOpenAIImageGenerator(
        this IServiceCollection services,
        Action<OpenAIImageGeneratorOptions> configureOptions)
    {
        if (configureOptions == null)
            throw new ArgumentNullException(nameof(configureOptions));

        // Configure options
        services.Configure(configureOptions);

        // Add HttpClient for downloading images
        services.AddHttpClient<OpenAIImageGenerator>();

        // Register OpenAI client
        services.AddSingleton<OpenAIClient>(serviceProvider =>
        {
            var options = serviceProvider.GetRequiredService<IOptions<OpenAIImageGeneratorOptions>>().Value;
            
            if (string.IsNullOrWhiteSpace(options.ApiKey))
                throw new InvalidOperationException("OpenAI API key is required. Please configure OpenAIImageGeneratorOptions.ApiKey.");

            var clientOptions = new OpenAIClientOptions();
            
            if (!string.IsNullOrWhiteSpace(options.BaseUrl))
                clientOptions.Endpoint = new Uri(options.BaseUrl);

            var apiKeyCredential = new System.ClientModel.ApiKeyCredential(options.ApiKey);
            return new OpenAIClient(apiKeyCredential, clientOptions);
        });

        // Register the image generator service
        services.AddScoped<IImageGenerator, OpenAIImageGenerator>();

        return services;
    }

    /// <summary>
    /// Adds the OpenAI image generator service with API key configuration
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="apiKey">OpenAI API key</param>
    /// <param name="organizationId">Optional organization ID</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddOpenAIImageGenerator(
        this IServiceCollection services,
        string apiKey,
        string? organizationId = null)
    {
        return services.AddOpenAIImageGenerator(options =>
        {
            options.ApiKey = apiKey;
            options.OrganizationId = organizationId;
        });
    }
}