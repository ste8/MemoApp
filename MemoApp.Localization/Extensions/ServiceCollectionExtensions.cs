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
}