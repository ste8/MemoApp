# Simple Error Monitoring Integration Example

This document shows how to integrate error monitoring into your existing MemoApp without creating enhanced services. This is a minimal, working integration approach.

## Step 1: Update MauiProgram.cs

Add error monitoring to your existing `MauiProgram.cs`:

```csharp
using MemoApp.UI.MauiApp.Services.ErrorMonitoring;
using MemoApp.Core.Services.ErrorMonitoring;

public static class MauiProgram
{
    public static Microsoft.Maui.Hosting.MauiApp CreateMauiApp()
    {
        var builder = Microsoft.Maui.Hosting.MauiApp.CreateBuilder();
        
        builder
            .UseMauiApp<App>()
            // ... existing configuration

        // ADD ERROR MONITORING - Add this line early
        builder.AddErrorMonitoring();

        // ... rest of your existing service registrations

        var app = builder.Build();

        // Initialize error monitoring after building
        Task.Run(async () =>
        {
            try
            {
                var errorService = app.Services.GetRequiredService<IErrorMonitoringService>();
                await errorService.InitializeAsync();
                
                var globalHandler = app.Services.GetRequiredService<GlobalExceptionHandler>();
                globalHandler.Initialize();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to initialize error monitoring: {ex}");
            }
        });

        return app;
    }
}
```

## Step 2: Add to Project File

Add this to your `MemoApp.UI.MauiApp.csproj`:

```xml
<PackageReference Include="Sentry.Maui" Version="4.0.0" />
```

## Step 3: Update appsettings.json

Add your Sentry DSN:

```json
{
  "Sentry": {
    "Dsn": "YOUR_SENTRY_DSN_HERE"
  }
}
```

## Step 4: Use in Existing Services

Add error monitoring to your existing services by injecting `IErrorMonitoringService`:

### Example: Adding to PackDownloadService

```csharp
public class PackDownloadService : IPackDownloadService
{
    private readonly IErrorMonitoringService? _errorMonitoring;
    // ... existing fields

    public PackDownloadService(
        HttpClient httpClient,
        IPackManagerService packManager,
        ILogger<PackDownloadService> logger,
        string packsIndexUrl,
        string dataDirectory,
        int cacheMinutes = 30,
        IErrorMonitoringService? errorMonitoring = null) // Optional
    {
        // ... existing constructor code
        _errorMonitoring = errorMonitoring;
    }

    public async Task<PackIndex?> GetAvailablePacksAsync(bool forceRefresh = false)
    {
        try
        {
            // ... existing code
        }
        catch (HttpRequestException httpEx)
        {
            // Add error monitoring
            await _errorMonitoring?.LogNetworkErrorAsync(
                "pack_index_fetch",
                _packsIndexUrl,
                httpEx,
                networkType: "Unknown") ?? Task.CompletedTask;
            
            // ... existing error handling
        }
        catch (JsonException jsonEx)
        {
            // Add error monitoring  
            await _errorMonitoring?.LogJsonParsingErrorAsync(
                "pack_index",
                "packs.json",
                jsonEx,
                "index_deserialization") ?? Task.CompletedTask;
            
            // ... existing error handling
        }
    }
}
```

### Example: Adding to ViewModels

```csharp
public partial class FlashNumbersViewModel : BaseViewModel
{
    private readonly IErrorMonitoringService? _errorMonitoring;

    public FlashNumbersViewModel(
        ILocalizationService localizationService,
        IAnalyticsService analyticsService,
        IErrorMonitoringService? errorMonitoring = null) // Optional
        : base(localizationService, analyticsService)
    {
        _errorMonitoring = errorMonitoring;
    }

    public override async Task InitializeAsync()
    {
        try
        {
            await _errorMonitoring?.AddBreadcrumbAsync(
                ErrorCategories.Exercise,
                "Flash Numbers initializing",
                new Dictionary<string, object>
                {
                    ["range"] = $"{RangeStart}-{RangeEnd}"
                }) ?? Task.CompletedTask;

            await base.InitializeAsync();
        }
        catch (Exception ex)
        {
            await _errorMonitoring?.LogExerciseInitErrorAsync(
                "flash_numbers",
                ex,
                category: "numbers") ?? Task.CompletedTask;
            
            throw; // Re-throw to maintain existing behavior
        }
    }
}
```

## Step 5: Update Service Registration

Update your service registrations to inject error monitoring:

```csharp
// In MauiProgram.cs
builder.Services.AddSingleton<IPackDownloadService>(serviceProvider =>
{
    var httpClient = serviceProvider.GetRequiredService<HttpClient>();
    var packManager = serviceProvider.GetRequiredService<IPackManagerService>();
    var logger = serviceProvider.GetRequiredService<ILogger<PackDownloadService>>();
    var errorMonitoring = serviceProvider.GetService<IErrorMonitoringService>(); // Optional
    
    return new PackDownloadService(
        httpClient, 
        packManager, 
        logger, 
        packsIndexUrl, 
        dataDirectory, 
        cacheMinutes,
        errorMonitoring);
});

builder.Services.AddTransient<FlashNumbersViewModel>(serviceProvider =>
{
    var localizationService = serviceProvider.GetRequiredService<ILocalizationService>();
    var analyticsService = serviceProvider.GetRequiredService<IAnalyticsService>();
    var errorMonitoring = serviceProvider.GetService<IErrorMonitoringService>(); // Optional
    
    return new FlashNumbersViewModel(localizationService, analyticsService, errorMonitoring);
});
```

## Benefits of This Approach

1. **Non-Breaking**: Won't break existing functionality
2. **Optional**: Error monitoring is optional - app works without it
3. **Gradual**: Can add to services one at a time
4. **Simple**: Minimal code changes needed
5. **Safe**: Uses null-conditional operators for safety

## Testing

After integration, test by:

1. Building the project: `dotnet build`
2. Running tests: `dotnet test`
3. Triggering a test exception to verify Sentry receives it
4. Checking Sentry dashboard for events

## Troubleshooting

If you get build errors:

1. Make sure Sentry DSN is configured
2. Check that all using statements are present
3. Ensure error monitoring service registration comes after `builder.AddErrorMonitoring()`
4. Verify project references are correct

This approach allows you to gradually add error monitoring without major refactoring of your existing code.