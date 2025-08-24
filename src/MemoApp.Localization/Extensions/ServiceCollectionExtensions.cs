using Microsoft.Extensions.DependencyInjection;
using MemoApp.Localization.Services;

namespace MemoApp.Localization.Extensions;

/// <summary>
/// Extension methods for configuring localization services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds localization services to the dependency injection container.
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddLocalization(this IServiceCollection services)
    {
        services.AddSingleton<ILocalizationService, LocalizationService>();
        return services;
    }

    /// <summary>
    /// Adds localization services with a custom implementation to the dependency injection container.
    /// </summary>
    /// <typeparam name="TImplementation">The localization service implementation type</typeparam>
    /// <param name="services">The service collection</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddLocalization<TImplementation>(this IServiceCollection services)
        where TImplementation : class, ILocalizationService
    {
        services.AddSingleton<ILocalizationService, TImplementation>();
        return services;
    }
    
    /// <summary>
    /// Initializes the LocalizationResourceManager with the registered service.
    /// Call this after building the service provider.
    /// </summary>
    /// <param name="serviceProvider">The service provider</param>
    public static void InitializeLocalization(this IServiceProvider serviceProvider)
    {
        var localizationService = serviceProvider.GetRequiredService<ILocalizationService>();
        LocalizationResourceManager.Initialize(localizationService);
    }
}