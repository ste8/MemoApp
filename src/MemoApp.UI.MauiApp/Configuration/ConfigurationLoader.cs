using System.Reflection;
using Microsoft.Extensions.Configuration;

namespace MemoApp.UI.MauiApp.Configuration;

/// <summary>
/// Handles loading of embedded configuration files into the MAUI application builder
/// </summary>
public static class ConfigurationLoader
{
    private const string AppSettingsResourceName = "MemoApp.UI.MauiApp.appsettings.json";
    private const string DevAppSettingsResourceName = "MemoApp.UI.MauiApp.appsettings.Development.json";
    private const string ProductionAppSettingsResourceName = "MemoApp.UI.MauiApp.appsettings.Production.json";

    /// <summary>
    /// Loads embedded configuration files into the builder configuration
    /// </summary>
    /// <param name="builder">The MAUI application builder</param>
    public static void LoadEmbeddedConfiguration(this MauiAppBuilder builder)
    {
        var assembly = Assembly.GetExecutingAssembly();
        
        // Always load main appsettings.json
        LoadConfigurationFromResource(builder, assembly, AppSettingsResourceName);
        
#if DEBUG
        // Load development settings in debug builds
        LoadConfigurationFromResource(builder, assembly, DevAppSettingsResourceName);
#else
        // Load production settings in release builds
        LoadConfigurationFromResource(builder, assembly, ProductionAppSettingsResourceName);
#endif
    }

    /// <summary>
    /// Loads a specific configuration resource into the builder
    /// </summary>
    /// <param name="builder">The MAUI application builder</param>
    /// <param name="assembly">The assembly containing the resource</param>
    /// <param name="resourceName">The name of the embedded resource</param>
    private static void LoadConfigurationFromResource(MauiAppBuilder builder, Assembly assembly, string resourceName)
    {
        using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream == null)
        {
            return; // Resource not found, skip silently
        }

        try
        {
            var configBuilder = new ConfigurationBuilder();
            configBuilder.AddJsonStream(stream);
            var config = configBuilder.Build();

            // Add all configuration values to the builder
            foreach (var kvp in config.AsEnumerable().Where(k => k.Value != null))
            {
                builder.Configuration[kvp.Key] = kvp.Value!;
            }
        }
        catch (Exception ex)
        {
            // Log configuration loading error but don't fail startup
            System.Diagnostics.Debug.WriteLine($"Failed to load configuration from {resourceName}: {ex.Message}");
        }
    }
}