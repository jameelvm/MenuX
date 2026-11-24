using Microsoft.Extensions.DependencyInjection;

namespace MenuX.Catalog;

/// <summary>Registers the Catalog module with the host. Empty in week 1.</summary>
public static class CatalogModule
{
    public static IServiceCollection AddCatalogModule(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        return services;
    }
}
