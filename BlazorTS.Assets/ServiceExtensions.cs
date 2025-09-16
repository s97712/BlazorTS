using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace BlazorTS.Assets;

/// <summary>
/// Extension methods for setting up asset services.
/// </summary>
public static class ServiceExtensions
{
    /// <summary>
    /// Adds the asset integration services to the specified <see cref="IServiceCollection"/>.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the services to.</param>
    /// <param name="configure">An <see cref="Action{AssetOptions}"/> to configure the provided <see cref="AssetOptions"/>.</param>
    /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
    public static IServiceCollection AddAssetIntegration(
        this IServiceCollection services,
        Action<AssetOptions> configure)
    {
        services.AddHttpContextAccessor();
        services.Configure(configure);

        services.AddScoped<IAssetService, AssetService>();
        return services;
    }
}