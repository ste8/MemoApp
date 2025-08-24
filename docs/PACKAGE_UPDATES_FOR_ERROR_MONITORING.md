# Package Updates for Error Monitoring

## Required NuGet Package Updates

Add these package references to your MAUI project file (`src/MemoApp.UI.MauiApp/MemoApp.UI.MauiApp.csproj`):

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFrameworks>net9.0-android;net9.0-ios;net9.0-maccatalyst</TargetFrameworks>
    <TargetFrameworks Condition="$([MSBuild]::IsOSPlatform('windows'))">$(TargetFrameworks);net9.0-windows10.0.19041.0</TargetFrameworks>
    <!-- Existing properties -->
  </PropertyGroup>

  <ItemGroup>
    <!-- Existing packages -->
    
    <!-- ERROR MONITORING PACKAGES -->
    <PackageReference Include="Sentry.Maui" Version="4.0.0" />
    <PackageReference Include="Sentry" Version="4.0.0" />
    
    <!-- TESTING PACKAGES (if not already included) -->
    <PackageReference Include="FluentAssertions" Version="6.12.0" />
    <PackageReference Include="Moq" Version="4.20.69" />
  </ItemGroup>

</Project>
```

## Core Project Updates

Add these package references to your Core project file (`src/MemoApp.Core/MemoApp.Core.csproj`):

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFrameworks>net9.0;net9.0-android;net9.0-ios;net9.0-maccatalyst</TargetFrameworks>
    <TargetFrameworks Condition="$([MSBuild]::IsOSPlatform('windows'))">$(TargetFrameworks);net9.0-windows10.0.19041.0</TargetFrameworks>
    <!-- Existing properties -->
  </PropertyGroup>

  <ItemGroup>
    <!-- Existing packages -->
    
    <!-- ERROR MONITORING INTERFACES -->
    <!-- No additional packages needed - only interfaces are defined in Core -->
  </ItemGroup>

</Project>
```

## Test Project Updates

Add these package references to your test projects:

### Unit Tests (`tests/MemoApp.Core.Tests/MemoApp.Core.Tests.csproj`):

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <IsPackable>false</IsPackable>
    <IsTestProject>true</IsTestProject>
  </PropertyGroup>

  <ItemGroup>
    <!-- Existing packages -->
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.8.0" />
    <PackageReference Include="xunit" Version="2.4.2" />
    <PackageReference Include="xunit.runner.visualstudio" Version="2.4.5" />
    <PackageReference Include="coverlet.collector" Version="6.0.0" />
    
    <!-- ERROR MONITORING TESTING -->
    <PackageReference Include="FluentAssertions" Version="6.12.0" />
    <PackageReference Include="Moq" Version="4.20.69" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\src\MemoApp.Core\MemoApp.Core.csproj" />
    <ProjectReference Include="..\..\src\MemoApp.UI.MauiApp\MemoApp.UI.MauiApp.csproj" />
  </ItemGroup>

</Project>
```

## Platform-Specific Configurations

### Android Manifest Updates

Add to `Platforms/Android/AndroidManifest.xml`:

```xml
<?xml version="1.0" encoding="utf-8"?>
<manifest xmlns:android="http://schemas.android.com/apk/res/android">
    
    <!-- Existing permissions -->
    
    <!-- ERROR MONITORING PERMISSIONS -->
    <uses-permission android:name="android.permission.INTERNET" />
    <uses-permission android:name="android.permission.ACCESS_NETWORK_STATE" />
    
    <application>
        <!-- Existing configuration -->
        
        <!-- Sentry configuration (optional - can also be done in code) -->
        <meta-data 
            android:name="io.sentry.dsn" 
            android:value="YOUR_SENTRY_DSN" />
    </application>
    
</manifest>
```

### iOS Info.plist Updates

Add to `Platforms/iOS/Info.plist`:

```xml
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
    <!-- Existing configuration -->
    
    <!-- NETWORK SECURITY FOR SENTRY -->
    <key>NSAppTransportSecurity</key>
    <dict>
        <key>NSExceptionDomains</key>
        <dict>
            <key>sentry.io</key>
            <dict>
                <key>NSExceptionAllowsInsecureHTTPLoads</key>
                <false/>
                <key>NSExceptionMinimumTLSVersion</key>
                <string>TLSv1.2</string>
            </dict>
        </dict>
    </dict>
    
    <!-- SENTRY CONFIGURATION (optional - can also be done in code) -->
    <key>SentryDsn</key>
    <string>YOUR_SENTRY_DSN</string>
</dict>
</plist>
```

### Windows Package.appxmanifest Updates

Add to `Platforms/Windows/Package.appxmanifest`:

```xml
<?xml version="1.0" encoding="utf-8"?>
<Package>
  <!-- Existing configuration -->
  
  <Applications>
    <Application>
      <!-- Existing configuration -->
      
      <!-- NETWORK CAPABILITIES FOR ERROR REPORTING -->
      <Capabilities>
        <Capability Name="internetClient" />
        <Capability Name="internetClientServer" />
      </Capabilities>
    </Application>
  </Applications>
</Package>
```

## Build Configuration Updates

### Directory.Build.props (Optional - for solution-wide settings)

Create `Directory.Build.props` in the repository root:

```xml
<Project>
  
  <PropertyGroup>
    <LangVersion>latest</LangVersion>
    <Nullable>enable</Nullable>
    <TreatWarningsAsErrors>false</TreatWarningsAsErrors>
    <WarningsAsErrors />
    <WarningsNotAsErrors>CS8618;CS8767</WarningsNotAsErrors>
  </PropertyGroup>

  <!-- ERROR MONITORING COMPILATION SYMBOLS -->
  <PropertyGroup Condition="'$(Configuration)' == 'Debug'">
    <DefineConstants>$(DefineConstants);DEBUG;ERROR_MONITORING_DEBUG</DefineConstants>
  </PropertyGroup>
  
  <PropertyGroup Condition="'$(Configuration)' == 'Release'">
    <DefineConstants>$(DefineConstants);ERROR_MONITORING_RELEASE</DefineConstants>
  </PropertyGroup>

</Project>
```

## Environment-Specific Configuration

### appsettings.Development.json

Create `src/MemoApp.UI.MauiApp/appsettings.Development.json`:

```json
{
  "Sentry": {
    "Dsn": "YOUR_DEV_SENTRY_DSN",
    "Environment": "development",
    "TracesSampleRate": 1.0,
    "ProfilesSampleRate": 1.0,
    "Debug": true,
    "DiagnosticLevel": "Debug"
  },
  "ErrorMonitoring": {
    "Enabled": true,
    "OfflineQueueSize": 50,
    "FlushInterval": 30,
    "MaxBreadcrumbs": 50,
    "EnablePerformanceMonitoring": true,
    "EnableUserFeedback": true
  }
}
```

### appsettings.Production.json

Create `src/MemoApp.UI.MauiApp/appsettings.Production.json`:

```json
{
  "Sentry": {
    "Dsn": "YOUR_PROD_SENTRY_DSN",
    "Environment": "production",
    "TracesSampleRate": 0.1,
    "ProfilesSampleRate": 0.1,
    "Debug": false,
    "DiagnosticLevel": "Warning"
  },
  "ErrorMonitoring": {
    "Enabled": true,
    "OfflineQueueSize": 100,
    "FlushInterval": 60,
    "MaxBreadcrumbs": 100,
    "EnablePerformanceMonitoring": true,
    "EnableUserFeedback": false
  }
}
```

## Version Compatibility

### Minimum Requirements

- **.NET**: 9.0 or later
- **MAUI**: 9.0 or later
- **Sentry.Maui**: 4.0.0 or later
- **Platform Versions**:
  - iOS: 12.0+
  - Android: API 21 (Android 5.0)+
  - Windows: 10.0.19041.0+
  - macOS: 11.0+

### Known Compatibility Issues

1. **Sentry.Maui 4.x** requires .NET 9.0
2. **Performance monitoring** requires Sentry 4.0+
3. **Profile sampling** requires Sentry 4.0+

## Installation Steps

1. **Update package references** in project files
2. **Install packages**:
   ```bash
   dotnet restore
   ```
3. **Configure Sentry DSN** in appsettings.json
4. **Update platform manifests** with required permissions
5. **Update MauiProgram.cs** with error monitoring integration
6. **Build and test**:
   ```bash
   dotnet build
   dotnet test
   ```

## Verification

After installation, verify error monitoring is working:

1. **Check logs** during app startup
2. **Trigger test exception** in debug build
3. **Monitor Sentry dashboard** for events
4. **Test offline queuing** by disabling network
5. **Verify privacy settings** are working
6. **Test performance monitoring** with slow operations

## Troubleshooting

### Common Installation Issues

1. **Package conflicts**: Clear NuGet cache
   ```bash
   dotnet nuget locals all --clear
   ```

2. **Build errors**: Ensure all projects target compatible .NET versions

3. **Platform-specific issues**: Check manifest files are correctly configured

4. **Sentry not receiving events**: Verify DSN configuration and network connectivity

### Support Resources

- [Sentry MAUI Documentation](https://docs.sentry.io/platforms/dotnet/guides/maui/)
- [.NET MAUI Documentation](https://docs.microsoft.com/en-us/dotnet/maui/)
- [Project GitHub Issues](https://github.com/your-repo/issues)