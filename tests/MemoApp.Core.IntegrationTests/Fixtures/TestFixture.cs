using MemoApp.Core.Extensions;
using MemoApp.Core.Services.ImageGenerators;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace MemoApp.Core.IntegrationTests.Fixtures;

/// <summary>
/// Base test fixture for integration tests
/// </summary>
public class TestFixture : IDisposable
{
    public IServiceProvider ServiceProvider { get; }
    public IConfiguration Configuration { get; }

    public TestFixture()
    {
        // Build configuration
        Configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false)
            .AddUserSecrets<TestFixture>()
            .AddEnvironmentVariables()
            .Build();

        // Build service collection
        var services = new ServiceCollection();
        ConfigureServices(services);
        
        ServiceProvider = services.BuildServiceProvider();
    }

    private void ConfigureServices(IServiceCollection services)
    {
        // Add logging
        services.AddLogging(builder =>
        {
            builder.AddConfiguration(Configuration.GetSection("Logging"));
            builder.AddConsole();
        });

        // Add configuration
        services.AddSingleton(Configuration);

        // Add OpenAI Image Generator if API key is configured
        var openAIApiKey = Configuration["OpenAI:ApiKey"];
        if (!string.IsNullOrWhiteSpace(openAIApiKey))
        {
            services.AddOpenAIImageGenerator(options =>
            {
                options.ApiKey = openAIApiKey;
                options.OrganizationId = Configuration["OpenAI:OrganizationId"];
                
                if (int.TryParse(Configuration["OpenAI:TimeoutSeconds"], out var timeout))
                    options.TimeoutSeconds = timeout;
                
                if (int.TryParse(Configuration["OpenAI:MaxConcurrentRequests"], out var maxConcurrent))
                    options.MaxConcurrentRequests = maxConcurrent;

                // Use test-friendly defaults
                options.DefaultGenerationOptions = new ImageGenerationOptions
                {
                    Width = 512,
                    Height = 512,
                    Style = "natural",
                    DownloadImageData = true,
                    AdditionalContext = "Simple test image for integration testing"
                };
            });
        }
    }

    public T GetRequiredService<T>() where T : notnull
    {
        return ServiceProvider.GetRequiredService<T>();
    }

    public T? GetService<T>()
    {
        return ServiceProvider.GetService<T>();
    }

    public bool IsOpenAIConfigured()
    {
        var apiKey = Configuration["OpenAI:ApiKey"];
        return !string.IsNullOrWhiteSpace(apiKey);
    }

    public void Dispose()
    {
        if (ServiceProvider is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }
}