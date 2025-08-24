# Error Monitoring & Crash Reporting Guide

## Overview

This document describes the comprehensive error monitoring and crash reporting system implemented for the MemoRaise MAUI application. The system uses Sentry for error tracking with privacy-compliant data collection and comprehensive performance monitoring.

## Architecture

### Core Components

1. **IErrorMonitoringService** - Core interface for error monitoring
2. **SentryErrorMonitoringService** - Sentry implementation
3. **ErrorMonitoringExtensions** - Helper methods for common scenarios
4. **GlobalExceptionHandler** - Application-wide exception handling
5. **MauiErrorMonitoringExtensions** - MAUI-specific integration

## Setup Instructions

### 1. Install NuGet Package

```xml
<PackageReference Include="Sentry.Maui" Version="4.0.0" />
```

### 2. Configure Sentry DSN

Add your Sentry DSN to `appsettings.json`:

```json
{
  "Sentry": {
    "Dsn": "YOUR_SENTRY_DSN_HERE",
    "Environment": "production",
    "TracesSampleRate": 0.1,
    "ProfilesSampleRate": 0.1
  }
}
```

### 3. Register in MauiProgram.cs

```csharp
public static MauiApp CreateMauiApp()
{
    var builder = MauiApp.CreateBuilder();
    
    // Add error monitoring
    builder.AddErrorMonitoring();
    
    // ... rest of configuration
    
    var app = builder.Build();
    
    // Initialize global exception handler
    var globalHandler = app.Services.GetRequiredService<GlobalExceptionHandler>();
    globalHandler.Initialize();
    
    // Initialize error monitoring service
    var errorService = app.Services.GetRequiredService<IErrorMonitoringService>();
    await errorService.InitializeAsync();
    
    return app;
}
```

## Usage Examples

### Basic Exception Logging

```csharp
public class MyService
{
    private readonly IErrorMonitoringService _errorMonitoring;
    
    public async Task DoSomethingAsync()
    {
        try
        {
            // Your code here
        }
        catch (Exception ex)
        {
            await _errorMonitoring.LogExceptionAsync(
                ex,
                "Failed to perform operation",
                new Dictionary<string, object>
                {
                    ["operation"] = "something",
                    ["user_action"] = "button_click"
                });
            throw;
        }
    }
}
```

### Pack Download Error Monitoring

```csharp
public class PackService
{
    private readonly IErrorMonitoringService _errorMonitoring;
    
    public async Task<bool> DownloadPackAsync(string packId, string url)
    {
        try
        {
            // Download logic
        }
        catch (HttpRequestException ex)
        {
            await _errorMonitoring.LogPackDownloadErrorAsync(
                packId,
                url,
                ex,
                errorStage: "download",
                downloadSizeMb: 25);
            return false;
        }
    }
}
```

### Exercise Performance Tracking

```csharp
public class ExerciseViewModel
{
    private readonly IErrorMonitoringService _errorMonitoring;
    
    public async Task LoadExerciseAsync()
    {
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            // Load exercise
            
            await _errorMonitoring.TrackExerciseLoadAsync(
                "flash_numbers",
                stopwatch.ElapsedMilliseconds,
                imageCount: 50,
                packLanguage: "italian",
                difficultyLevel: 3);
        }
        catch (Exception ex)
        {
            await _errorMonitoring.LogExerciseInitErrorAsync(
                "flash_numbers",
                ex,
                difficultyLevel: 3,
                category: "numbers",
                packId: "italian_basic");
        }
    }
}
```

### Memory Pressure Monitoring

```csharp
public class MemoryMonitor
{
    private readonly IErrorMonitoringService _errorMonitoring;
    
    public async Task CheckMemoryAsync()
    {
        var memoryInfo = GC.GetTotalMemory(false) / (1024 * 1024);
        
        if (memoryInfo > 400) // MB
        {
            await _errorMonitoring.LogMemoryPressureAsync(
                currentUsageMb: memoryInfo,
                peakUsageMb: 450,
                availableMemoryMb: 50,
                loadedImagesCount: 150,
                activeExercise: "flash_numbers");
        }
    }
}
```

### Adding Breadcrumbs for Navigation

```csharp
public class NavigationService
{
    private readonly IErrorMonitoringService _errorMonitoring;
    
    public async Task NavigateToAsync(string page)
    {
        await _errorMonitoring.AddBreadcrumbAsync(
            ErrorCategories.Navigation,
            $"Navigating to {page}",
            new Dictionary<string, object>
            {
                ["from_page"] = Shell.Current.CurrentPage?.GetType().Name,
                ["to_page"] = page,
                ["timestamp"] = DateTime.UtcNow
            });
            
        await Shell.Current.GoToAsync(page);
    }
}
```

## Error Categories

The system defines standard error categories for consistent tracking:

- `pack_management` - Pack download, extraction, and management errors
- `exercise` - Exercise initialization and execution errors
- `image_loading` - Image loading and processing errors
- `storage` - File system and data persistence errors
- `network` - Network connectivity and HTTP errors
- `settings` - Settings and configuration errors
- `performance` - Performance-related issues
- `ui` - UI rendering and interaction errors
- `navigation` - Navigation and routing errors
- `analytics` - Analytics tracking errors

## Performance Metrics

Standard performance metrics tracked:

- `app_startup` - Application startup time
- `exercise_load` - Exercise loading time
- `pack_download` - Pack download duration
- `pack_extraction` - Pack extraction time
- `image_load` - Image loading time
- `json_parsing` - JSON parsing duration
- `settings_save` - Settings save operation
- `settings_load` - Settings load operation
- `navigation_transition` - Page navigation time

## Privacy & Data Protection

### Data Sanitization

The system automatically sanitizes sensitive data:

1. **User Information**
   - No email addresses collected
   - No IP addresses stored
   - Anonymous user IDs only

2. **File Paths**
   - User-specific paths sanitized
   - Only relative paths stored

3. **Sensitive Keys**
   - Password, token, key, secret fields filtered
   - Credit card and bank information excluded

### User Consent

Users can opt-out of error reporting:

```csharp
// In SettingsViewModel
public async Task SetErrorReportingAsync(bool enabled)
{
    await _errorMonitoring.EnableErrorReportingAsync(enabled);
    await _preferences.SetAsync("error_reporting_enabled", enabled);
}
```

## Platform-Specific Configuration

### iOS Configuration

Add to `Platforms/iOS/Info.plist`:

```xml
<key>NSAppTransportSecurity</key>
<dict>
    <key>NSExceptionDomains</key>
    <dict>
        <key>sentry.io</key>
        <dict>
            <key>NSExceptionAllowsInsecureHTTPLoads</key>
            <false/>
        </dict>
    </dict>
</dict>
```

### Android Configuration

Add to `Platforms/Android/AndroidManifest.xml`:

```xml
<uses-permission android:name="android.permission.INTERNET" />
<uses-permission android:name="android.permission.ACCESS_NETWORK_STATE" />
```

## Environment Configuration

### Development

```json
{
  "Sentry": {
    "Environment": "development",
    "Debug": true,
    "TracesSampleRate": 1.0
  }
}
```

### Staging

```json
{
  "Sentry": {
    "Environment": "staging",
    "Debug": false,
    "TracesSampleRate": 0.5
  }
}
```

### Production

```json
{
  "Sentry": {
    "Environment": "production",
    "Debug": false,
    "TracesSampleRate": 0.1
  }
}
```

## Testing Error Monitoring

### Unit Tests

Run the error monitoring tests:

```bash
dotnet test tests/MemoApp.Core.Tests/Services/ErrorMonitoring/ErrorMonitoringServiceTests.cs
```

### Manual Testing

1. **Test Exception Logging**
   ```csharp
   throw new Exception("Test error - please ignore");
   ```

2. **Test Performance Tracking**
   ```csharp
   await _errorMonitoring.TrackPerformanceAsync("test_operation", 1500, null);
   ```

3. **Test Offline Queuing**
   - Disable network
   - Trigger errors
   - Re-enable network
   - Verify errors are sent

## Monitoring Dashboard

### Key Metrics to Track

1. **Crash-Free Session Rate**
   - Target: > 95%
   - Alert threshold: < 90%

2. **Error Rate**
   - Monitor spikes in error frequency
   - Track error trends over releases

3. **Performance Metrics**
   - App startup time < 3 seconds
   - Exercise load time < 2 seconds
   - Pack download speed monitoring

4. **Device-Specific Issues**
   - Filter by platform (iOS, Android, Windows, macOS)
   - Monitor memory issues by device type
   - Track OS version compatibility

## Troubleshooting

### Common Issues

1. **Sentry not receiving events**
   - Check DSN configuration
   - Verify network connectivity
   - Check Sentry project rate limits

2. **High memory usage from error monitoring**
   - Reduce breadcrumb limit
   - Decrease offline queue size
   - Lower sampling rates

3. **Sensitive data appearing in reports**
   - Review sanitization rules
   - Add custom filtering
   - Update privacy settings

## Best Practices

1. **Always include context** when logging errors
2. **Use appropriate error levels** (Debug, Info, Warning, Error, Fatal)
3. **Track performance** for critical user flows
4. **Add breadcrumbs** for complex operations
5. **Test error handling** in development
6. **Monitor error trends** after releases
7. **Set up alerts** for critical errors
8. **Review privacy compliance** regularly

## Release Checklist

- [ ] Update Sentry DSN for production
- [ ] Set appropriate sampling rates
- [ ] Disable debug mode
- [ ] Test error reporting in release build
- [ ] Verify privacy compliance
- [ ] Set up monitoring alerts
- [ ] Document known issues
- [ ] Train support team on dashboard

## Support

For issues or questions about error monitoring:
1. Check Sentry documentation: https://docs.sentry.io/
2. Review this guide
3. Contact the development team

## Version History

- v1.0.0 - Initial error monitoring implementation
- Integrated with Sentry
- Added privacy protection
- Implemented offline queuing
- Added performance monitoring